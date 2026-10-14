using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DeckGame
{
    public enum MonsterIntentType
    {
        Attack,   // 일반 공격
        Defend,   // 방어 태세 (방어도 획득)
        Buff      // 분노 강화 (공격력 증가)
    }

    public class MonsterController : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Monster Stats")]
        public string monsterName = "불꽃 골렘 (Flame Golem)";
        public CardElement monsterElement = CardElement.Fire; // 속성 상성 계산용
        public int maxHp = 150;
        public int currentHp = 150;
        public int monsterShield = 0; // 몬스터 방어도
        public int nextAttackDamage = 12;

        [Header("Intents & Status")]
        public MonsterIntentType nextIntent = MonsterIntentType.Attack;
        public int tauntTurns = 0; // 도발 지속 턴 수 (0보다 크면 공격 외 다른 행동 불가)

        public bool IsTaunted => tauntTurns > 0;

        [Header("UI References")]
        public Text nameText;
        public Slider hpSlider;
        public Text hpText;
        public Text intentText;
        public Image avatarImage;
        public Image highlightRing;
        public Button fireButton;
        public Button waterButton;
        public Button grassButton;

        private Vector3 _originalAvatarPos;
        private Coroutine _shakeCoroutine;
        private bool _isHovered = false;

        private void Awake()
        {
            currentHp = maxHp;
            monsterShield = 0;
            tauntTurns = 0;
            if (avatarImage != null)
            {
                _originalAvatarPos = avatarImage.transform.localPosition;
            }
            RollNextIntent();
            UpdateUI();
        }

        private void Start()
        {
            StartCoroutine(IdleFloatRoutine());

            if (fireButton != null) fireButton.onClick.AddListener(() => SetMonsterElement(CardElement.Fire));
            if (waterButton != null) waterButton.onClick.AddListener(() => SetMonsterElement(CardElement.Water));
            if (grassButton != null) grassButton.onClick.AddListener(() => SetMonsterElement(CardElement.Grass));
        }

        public void ApplyTaunt(int turns = 3)
        {
            tauntTurns = turns;
            nextIntent = MonsterIntentType.Attack; // 도발 즉시 행동을 공격으로 강제 고정!
            UpdateUI();

            BattleManager.Instance?.SpawnFloatingText($"💢 도발됨! 공격 외 행동 불가 ({turns}턴)", transform.position + new Vector3(0, 50f, 0), new Color(0.9f, 0.35f, 0.95f), 32f);
        }

        public void RollNextIntent()
        {
            // 도발 상태이면 무조건 공격만 가능!
            if (IsTaunted)
            {
                nextIntent = MonsterIntentType.Attack;
                return;
            }

            // 도발이 아니면 공격, 방어 태세, 버프 중 무작위 선택
            int roll = Random.Range(0, 100);
            if (roll < 55)
            {
                nextIntent = MonsterIntentType.Attack;
            }
            else if (roll < 80)
            {
                nextIntent = MonsterIntentType.Defend;
            }
            else
            {
                nextIntent = MonsterIntentType.Buff;
            }
        }

        public void GainShield(int amount)
        {
            monsterShield += amount;
            UpdateUI();
            BattleManager.Instance?.SpawnFloatingText($"🛡️ 몬스터 방어도 +{amount}!", transform.position + new Vector3(0, 45f, 0), Color.cyan, 32f);
        }

        public void Heal(int amount)
        {
            int oldHp = currentHp;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            int recovered = currentHp - oldHp;
            UpdateUI();
            BattleManager.Instance?.SpawnFloatingText($"💚 몬스터 체력 +{recovered} 회복!", transform.position + new Vector3(0, 45f, 0), new Color(0.2f, 1f, 0.4f), 32f);
        }

        public void SetMonsterElement(CardElement newElement)
        {
            monsterElement = newElement;
            switch (monsterElement)
            {
                case CardElement.Fire:
                    monsterName = "불꽃 골렘 (Flame Golem)";
                    if (avatarImage != null) avatarImage.color = new Color(1f, 0.85f, 0.8f);
                    break;
                case CardElement.Water:
                    monsterName = "심연의 수룡 (Abyssal Drake)";
                    if (avatarImage != null) avatarImage.color = new Color(0.7f, 0.88f, 1f);
                    break;
                case CardElement.Grass:
                    monsterName = "고대 덩굴 수호자 (Vine Guardian)";
                    if (avatarImage != null) avatarImage.color = new Color(0.75f, 1f, 0.75f);
                    break;
            }
            UpdateUI();
            string kName = monsterElement == CardElement.Fire ? "불" : (monsterElement == CardElement.Water ? "물" : "풀");
            BattleManager.Instance?.SpawnFloatingText($"몬스터 속성 변경: [{kName}]", transform.position + new Vector3(0, 40f, 0), Color.yellow, 28f);

            // 손패의 모든 카드 UI 갱신 (상성 반영된 데미지 즉시 업데이트)
            RefreshAllHandCards();
        }

        public void RefreshAllHandCards()
        {
            var bm = BattleManager.Instance != null ? BattleManager.Instance : FindFirstObjectByType<BattleManager>();
            var slots = bm?.cardSlots != null && bm.cardSlots.Length > 0 ? bm.cardSlots : FindObjectsByType<CardSlot>(FindObjectsSortMode.None);
            if (slots != null)
            {
                foreach (var slot in slots)
                {
                    if (slot != null && !slot.IsEmpty)
                    {
                        slot.UpdateUI();
                    }
                }
            }
        }

        public void CycleElement()
        {
            CardElement next = monsterElement switch
            {
                CardElement.Fire => CardElement.Water,
                CardElement.Water => CardElement.Grass,
                _ => CardElement.Fire
            };
            SetMonsterElement(next);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (CardDragHandler.CurrentlyDraggedCard == null)
            {
                CycleElement();
            }
        }

        public void UpdateUI()
        {
            if (nameText != null)
            {
                string elemColor = monsterElement switch
                {
                    CardElement.Fire => "#FF5544",
                    CardElement.Water => "#44AAFF",
                    CardElement.Grass => "#44DD66",
                    _ => "#FFFFFF"
                };
                string elemName = monsterElement switch
                {
                    CardElement.Fire => "불",
                    CardElement.Water => "물",
                    CardElement.Grass => "풀",
                    _ => ""
                };
                nameText.text = $"<color={elemColor}>[{elemName}]</color> {monsterName}";
            }

            if (hpSlider != null)
            {
                hpSlider.maxValue = maxHp;
                hpSlider.value = currentHp;
            }

            if (hpText != null)
            {
                if (monsterShield > 0)
                {
                    hpText.text = $"{currentHp} / {maxHp} <color=#44CCFF>[🛡️{monsterShield}]</color>";
                }
                else
                {
                    hpText.text = $"{currentHp} / {maxHp}";
                }
            }

            if (intentText != null)
            {
                string tauntStatus = IsTaunted ? $" <color=#FF44DD>[💢도발 {tauntTurns}턴]</color>" : "";
                string intentDesc = nextIntent switch
                {
                    MonsterIntentType.Attack => $"⚔ 공격: {nextAttackDamage} 피해",
                    MonsterIntentType.Defend => $"🛡️ 방어 태세: 방어도 15 획득",
                    MonsterIntentType.Buff => $"🔥 분노 포효: 공격력 +4 영구 강화",
                    _ => $"⚔ 공격: {nextAttackDamage} 피해"
                };

                intentText.text = $"다음 행동: {intentDesc}{tauntStatus}";
            }
        }

        public void TakeDamage(int baseDamage, CardElement attackerElement, bool isCrit = false)
        {
            // 속성 상성 배율 계산
            float multiplier = new CardData(attackerElement).GetElementMultiplier(monsterElement);
            int calculatedDamage = Mathf.RoundToInt(baseDamage * multiplier);

            // 치명타 적용 (급소 노리기 등으로 발생 시 1.5배 피해)
            if (isCrit)
            {
                calculatedDamage = Mathf.RoundToInt(calculatedDamage * 1.5f);
            }

            Debug.Log($"[속성 공격] 카드: {attackerElement} -> 몬스터: {monsterElement} | 기본: {baseDamage} x 상성: {multiplier}배 {(isCrit ? "x 크리티컬: 1.5배" : "")} = 최종 피해: {calculatedDamage}");

            // 몬스터 실드 흡수
            int damageAfterShield = calculatedDamage;
            if (monsterShield > 0)
            {
                if (monsterShield >= calculatedDamage)
                {
                    monsterShield -= calculatedDamage;
                    damageAfterShield = 0;
                    BattleManager.Instance?.SpawnFloatingText($"적 방패 {calculatedDamage} 흡수!", transform.position + new Vector3(0, 50f, 0), Color.cyan, 32f);
                }
                else
                {
                    damageAfterShield = calculatedDamage - monsterShield;
                    int absorbed = monsterShield;
                    monsterShield = 0;
                    BattleManager.Instance?.SpawnFloatingText($"적 방패 {absorbed} 파괴! (-{damageAfterShield})", transform.position + new Vector3(0, 50f, 0), new Color(1f, 0.6f, 0.2f), 32f);
                }
            }

            currentHp = Mathf.Max(0, currentHp - damageAfterShield);
            UpdateUI();

            // Hit shake
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeRoutine());

            // 속성별 피해 텍스트 색상 및 상성 텍스트
            Color dmgColor;
            string effectText = "";
            float fontSize = 36f;

            if (isCrit)
            {
                effectText = " 💥 치명타!";
                dmgColor = new Color(1f, 0.85f, 0.1f);
                fontSize = 44f;
            }
            else if (multiplier >= 1.4f)
            {
                // 유리한 상성 (1.5배)
                effectText = " (1.5배 효과 발군!)";
                dmgColor = new Color(1f, 0.9f, 0.2f);
                fontSize = 42f;
            }
            else if (multiplier <= 0.6f)
            {
                // 불리한 상성 (0.5배)
                effectText = " (0.5배 반감...)";
                dmgColor = new Color(0.7f, 0.7f, 0.7f);
                fontSize = 30f;
            }
            else
            {
                // 동속성 (1배)
                switch (attackerElement)
                {
                    case CardElement.Water: dmgColor = new Color(0.3f, 0.7f, 1f); break;
                    case CardElement.Grass: dmgColor = new Color(0.4f, 1f, 0.4f); break;
                    case CardElement.Fire:  dmgColor = new Color(1f, 0.35f, 0.2f); break;
                    default: dmgColor = Color.white; break;
                }
            }

            if (damageAfterShield > 0)
            {
                BattleManager.Instance?.SpawnFloatingText(
                    $"-{damageAfterShield}{effectText}",
                    transform.position + new Vector3(0, 40f, 0),
                    dmgColor, fontSize);
            }

            if (currentHp <= 0)
            {
                OnDefeated();
            }
        }

        private void OnDefeated()
        {
            BattleManager.Instance?.SpawnFloatingText("적 처치 완료!", transform.position, Color.yellow, 42f, 2f);
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            maxHp += 40;
            currentHp = maxHp;
            monsterShield = 0;
            tauntTurns = 0;
            nextAttackDamage += 3;

            // 처치될 때마다 다음 속성의 몬스터로 순환 (불 -> 풀 -> 물 -> 불)
            if (monsterElement == CardElement.Fire)
            {
                monsterElement = CardElement.Grass;
                monsterName = "고대 덩굴 수호자 (Vine Guardian)";
            }
            else if (monsterElement == CardElement.Grass)
            {
                monsterElement = CardElement.Water;
                monsterName = "심연의 수룡 (Abyssal Drake)";
            }
            else
            {
                monsterElement = CardElement.Fire;
                monsterName = "분노한 화염 골렘 (Flame Lord)";
            }

            RollNextIntent();
            UpdateUI();
            RefreshAllHandCards();
            BattleManager.Instance?.SpawnFloatingText($"새로운 적 등장!", transform.position, Color.red, 36f);
        }

        private IEnumerator ShakeRoutine()
        {
            if (avatarImage == null) yield break;

            float elapsed = 0f;
            float duration = 0.25f;
            float magnitude = 15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                avatarImage.transform.localPosition = _originalAvatarPos + new Vector3(x, y, 0);
                yield return null;
            }

            avatarImage.transform.localPosition = _originalAvatarPos;
        }

        private IEnumerator IdleFloatRoutine()
        {
            if (avatarImage == null) yield break;

            while (true)
            {
                float offset = Mathf.Sin(Time.time * 2f) * 6f;
                if (_shakeCoroutine == null)
                {
                    avatarImage.transform.localPosition = _originalAvatarPos + new Vector3(0, offset, 0);
                }
                yield return null;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (highlightRing != null && CardDragHandler.CurrentlyDraggedCard != null)
            {
                highlightRing.gameObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (highlightRing != null)
            {
                highlightRing.gameObject.SetActive(false);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (CardDragHandler.CurrentlyDraggedCard != null)
            {
                CardDragHandler.CurrentlyDraggedCard.AttemptAttackMonster(this);
            }
        }

        public void SetHighlight(bool active)
        {
            if (highlightRing != null)
            {
                highlightRing.gameObject.SetActive(active);
            }
        }
    }
}
