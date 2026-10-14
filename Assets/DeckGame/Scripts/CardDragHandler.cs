using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DeckGame
{
    /// <summary>
    /// 카드 드래그/드롭 처리.
    ///
    /// 핵심 설계:
    ///  - CardVisualRoot 에 Canvas(overrideSorting) + GraphicRaycaster 를 붙여 이벤트를 자체적으로 수신.
    ///  - 드래그 중에는 re-parent 없이 anchoredPosition 에 delta 를 더해서 이동.
    ///    (eventData.delta / rootCanvas.scaleFactor 로 픽셀 → 캔버스 단위 변환)
    ///  - SnapBack 은 anchoredPosition 을 zero 로 리셋.
    ///  - OnEnable 에서 SourceSlot 과 루트 Canvas 를 매번 재탐색 (턴 종료 후 SetActive 사이클 대응).
    /// </summary>
    public class CardDragHandler : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static CardDragHandler CurrentlyDraggedCard { get; private set; }

        public CardSlot SourceSlot { get; private set; }

        // CardVisualRoot 에 붙이는 중첩 Canvas (이벤트 수신 + sortingOrder 제어용)
        private Canvas _cardCanvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        // 루트 Canvas (scaleFactor 를 얻기 위해 캐싱)
        private Canvas _rootCanvas;

        private bool _isDragging = false;

        // ─────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            // CanvasGroup — BattleSceneBuilder 에서 이미 추가했지만 없으면 추가
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 중첩 Canvas — overrideSorting 으로 드래그 시 최상단 렌더링
            _cardCanvas = GetComponent<Canvas>();
            if (_cardCanvas == null)
                _cardCanvas = gameObject.AddComponent<Canvas>();
            _cardCanvas.overrideSorting = false;

            // 중첩 Canvas 의 이벤트 수신에는 반드시 GraphicRaycaster 가 필요
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        private void Start()
        {
            Refresh();
        }

        // cardVisualRoot 가 SetActive(false → true) 될 때마다 재탐색
        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            // 부모 계층에서 CardSlot 탐색
            SourceSlot = GetComponentInParent<CardSlot>(true);

            // 루트 Canvas 탐색 (GetComponentsInParent 의 마지막 = 최상위)
            Canvas[] canvases = GetComponentsInParent<Canvas>(true);
            if (canvases != null && canvases.Length > 0)
                _rootCanvas = canvases[canvases.Length - 1];
        }

        // ─────────────────────────────────────────
        //  Drag Events
        // ─────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            // InputSystemUIInputModule 이 드래그를 인식하려면 반드시 구현해야 함
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (SourceSlot == null || SourceSlot.IsEmpty)
            {
                eventData.pointerDrag = null;
                return;
            }

            CurrentlyDraggedCard = this;
            _isDragging = true;

            // 레이캐스트를 막아서 드래그 중에는 자신이 드롭 감지에 간섭하지 않도록
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.85f;

            // 중첩 Canvas 의 sortingOrder 를 올려 카드를 최상단에 렌더링
            _cardCanvas.overrideSorting = true;
            _cardCanvas.sortingOrder = 100;

            // 하이라이트는 몬스터에 갖다 댈 때만 활성화 (MonsterController.OnPointerEnter에서 처리)
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            // eventData.delta = 이번 프레임의 마우스 이동량(스크린 픽셀)
            // rootCanvas.scaleFactor 로 나누면 캔버스 로컬 단위로 변환됨
            // anchoredPosition 은 부모(CardSlot) 기준 상대 위치이므로 delta 를 누적
            float scale = (_rootCanvas != null) ? _rootCanvas.scaleFactor : 1f;
            _rectTransform.anchoredPosition += eventData.delta / scale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;
            CurrentlyDraggedCard = null;

            _cardCanvas.overrideSorting = false;
            BattleManager.Instance?.Monster?.SetHighlight(false);

            // 레이캐스트 중에는 카드가 drop target을 가리지 않도록 blocksRaycasts는 SnapBack에서 복구
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool handled = false;
            foreach (var hit in results)
            {
                // 1. 몬스터에 드롭
                MonsterController monster = hit.gameObject.GetComponentInParent<MonsterController>();
                if (monster != null)
                {
                    handled = AttemptUseOnTarget(monster, null);
                    if (handled) break;
                }

                // 2. PlayerDropZone에 드롭
                PlayerDropZone dropZone = hit.gameObject.GetComponentInParent<PlayerDropZone>();
                if (dropZone != null)
                {
                    handled = AttemptUseOnTarget(null, BattleManager.Instance?.Player);
                    if (handled) break;
                }

                // 3. 카드 슬롯에 드롭 (합치기)
                CardSlot targetSlot = hit.gameObject.GetComponentInParent<CardSlot>();
                if (targetSlot != null && targetSlot != SourceSlot)
                {
                    handled = AttemptMergeToSlot(targetSlot);
                    if (handled) break;
                }
            }

            // 폴백: 레이캐스트가 몬스터를 잡지 못한 경우, 몬스터 RectTransform 정밀 체크
            if (!handled && BattleManager.Instance?.Monster != null)
            {
                RectTransform mRt = BattleManager.Instance.Monster.GetComponent<RectTransform>();
                bool inMonster = RectTransformUtility.RectangleContainsScreenPoint(
                    mRt, eventData.position, eventData.pressEventCamera);
                if (inMonster)
                {
                    handled = AttemptUseOnTarget(BattleManager.Instance.Monster, null);
                }
            }

            // 어디에도 드롭되지 않았으면 원위치 복귀
            if (!handled)
                SnapBack();
        }

        // ─────────────────────────────────────────
        //  Actions
        // ─────────────────────────────────────────

        /// <summary>
        /// 카드를 대상(몬스터 또는 플레이어)에게 사용.
        /// monster != null → 몬스터 대상
        /// player  != null → 플레이어 대상
        /// </summary>
        public bool AttemptUseOnTarget(MonsterController monster, PlayerController player)
        {
            if (SourceSlot == null || SourceSlot.IsEmpty) return false;

            CardData card = SourceSlot.currentCard;
            int cost = card.EnergyCost;
            if (BattleManager.Instance.Player.CurrentEnergy < cost)
            {
                BattleManager.Instance.SpawnFloatingText("⚡ 에너지가 부족합니다!", transform.position, Color.yellow, 30f);
                SnapBack();
                return false;
            }

            BattleManager.Instance.Player.ConsumeEnergy(cost);

            bool onMonster = monster != null;

            if (card.IsNeutral)
            {
                switch (card.neutralType)
                {
                    case NeutralCardType.HealthPotion:
                        if (onMonster) monster.Heal(card.HealAmount);
                        else           player.Heal(card.HealAmount);
                        break;

                    case NeutralCardType.Shield:
                        if (onMonster) monster.GainShield(card.ShieldAmount);
                        else           player.GainShield(card.ShieldAmount);
                        break;

                    case NeutralCardType.AimWeakPoint:
                        // 급소 노리기는 항상 플레이어에게 적용 (자신의 치명타 버프)
                        BattleManager.Instance.Player.ApplyCritBuff(3);
                        break;

                    case NeutralCardType.Taunt:
                        // 도발은 항상 몬스터에게 적용
                        MonsterController target = monster ?? BattleManager.Instance?.Monster;
                        target?.ApplyTaunt(3);
                        break;
                }
            }
            else
            {
                // 속성 공격 카드
                bool isCrit = BattleManager.Instance.Player != null
                           && (Random.value < BattleManager.Instance.Player.CurrentCritChance);

                if (onMonster)
                {
                    // 몬스터에게 속성 피해
                    monster.TakeDamage(card.Damage, card.element, isCrit);
                }
                else
                {
                    // 플레이어에게 자해 피해 (속성 배율 없음, 치명타 없음)
                    int selfDmg = card.Damage;
                    player.TakeDamage(selfDmg);
                    string elemIcon = card.element switch
                    {
                        CardElement.Fire  => "🔥",
                        CardElement.Water => "💧",
                        CardElement.Grass => "🌿",
                        _ => ""
                    };
                    BattleManager.Instance?.SpawnFloatingText(
                        $"{elemIcon} 자해! -{selfDmg}",
                        player.transform.position + new Vector3(160f, 50f, 0),
                        new Color(1f, 0.5f, 0.5f), 30f);
                }
            }

            BattleManager.Instance?.DiscardCard(card);
            SourceSlot.RemoveOneCard();
            SnapBack();
            return true;
        }

        /// <summary>
        /// 하위 호환성: 몬스터에게만 카드 사용 (MonsterController.OnDrop에서 호출)
        /// </summary>
        public bool AttemptAttackMonster(MonsterController monster)
            => AttemptUseOnTarget(monster, null);

        public bool AttemptMergeToSlot(CardSlot targetSlot)
        {
            if (SourceSlot == null || SourceSlot.IsEmpty || targetSlot == null || targetSlot == SourceSlot) return false;

            if (targetSlot.IsEmpty)
            {
                targetSlot.SetCard(SourceSlot.currentCard, SourceSlot.stackCount);
                SourceSlot.ClearSlot();
                SnapBack();
                return true;
            }

            if (targetSlot.currentCard.CanMergeWith(SourceSlot.currentCard))
            {
                targetSlot.TryAddCard(SourceSlot.currentCard);
                SourceSlot.RemoveOneCard();
                SnapBack();
                return true;
            }

            string failMsg = (!SourceSlot.currentCard.CanMerge || !targetSlot.currentCard.CanMerge)
                ? "합칠 수 없는 고유 카드입니다!"
                : "속성과 레벨이 같은 카드만 합칠 수 있습니다!";

            BattleManager.Instance?.SpawnFloatingText(failMsg, transform.position, new Color(1f, 0.4f, 0.4f), 22f);
            SnapBack();
            return false;
        }

        // ─────────────────────────────────────────
        //  Snap Back
        // ─────────────────────────────────────────

        public void SnapBack()
        {
            _isDragging = false;

            // 원위치 (CardSlot 기준 (0,0,0))
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.localScale = Vector3.one;

            // sortingOrder 리셋
            if (_cardCanvas != null)
                _cardCanvas.overrideSorting = false;

            // blocksRaycasts 복구
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1f;
            }

            // 슬롯 UI 갱신
            if (SourceSlot != null)
                SourceSlot.UpdateUI();
        }
    }
}
