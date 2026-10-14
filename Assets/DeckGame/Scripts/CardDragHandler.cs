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

            BattleManager.Instance?.Monster?.SetHighlight(true);
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

            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1.0f;
            _cardCanvas.overrideSorting = false;

            BattleManager.Instance?.Monster?.SetHighlight(false);

            // 드롭 대상 감지
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool handled = false;
            foreach (var hit in results)
            {
                MonsterController monster = hit.gameObject.GetComponentInParent<MonsterController>();
                if (monster != null)
                {
                    handled = AttemptAttackMonster(monster);
                    if (handled) break;
                }

                CardSlot targetSlot = hit.gameObject.GetComponentInParent<CardSlot>();
                if (targetSlot != null && targetSlot != SourceSlot)
                {
                    handled = AttemptMergeToSlot(targetSlot);
                    if (handled) break;
                }
            }

            // 어디에도 드롭하지 않았으면 원위치
            if (!handled)
                SnapBack();
        }

        // ─────────────────────────────────────────
        //  Actions
        // ─────────────────────────────────────────

        public bool AttemptAttackMonster(MonsterController monster)
        {
            if (SourceSlot == null || SourceSlot.IsEmpty || monster == null) return false;

            int cost = SourceSlot.currentCard.EnergyCost;
            if (BattleManager.Instance.Player.CurrentEnergy < cost)
            {
                BattleManager.Instance.SpawnFloatingText("⚡ 에너지가 부족합니다!", transform.position, Color.yellow, 30f);
                SnapBack();
                return false;
            }

            BattleManager.Instance.Player.ConsumeEnergy(cost);
            monster.TakeDamage(SourceSlot.currentCard.Damage, SourceSlot.currentCard.element);
            SourceSlot.RemoveOneCard();

            SnapBack();
            return true;
        }

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

            BattleManager.Instance?.SpawnFloatingText(
                "속성과 레벨이 같은 카드만 합칠 수 있습니다!",
                transform.position, new Color(1f, 0.4f, 0.4f), 22f);
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
