using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DeckGame
{
    public class MonsterController : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Monster Stats")]
        public string monsterName = "불꽃 골렘 (Flame Golem)";
        public CardElement monsterElement = CardElement.Fire; // 속성 상성 계산용
        public int maxHp = 150;
        public int currentHp = 150;
        public int nextAttackDamage = 12;

        [Header("UI References")]
        public Text nameText;
        public Slider hpSlider;
        public Text hpText;
        public Text intentText;
        public Image avatarImage;
        public Image highlightRing;

        private Vector3 _originalAvatarPos;
        private Coroutine _shakeCoroutine;
        private bool _isHovered = false;

        private void Awake()
        {
            currentHp = maxHp;
            if (avatarImage != null)
            {
                _originalAvatarPos = avatarImage.transform.localPosition;
            }
            UpdateUI();
        }

        private void Start()
        {
            StartCoroutine(IdleFloatRoutine());
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
                hpText.text = $"{currentHp} / {maxHp}";
            }
            if (intentText != null)
            {
                intentText.text = $"⚔ 다음 행동: {nextAttackDamage} 피해";
            }
        }

        public void TakeDamage(int baseDamage, CardElement attackerElement)
        {
            // 속성 상성 배율 계산
            float multiplier = new CardData(attackerElement).GetElementMultiplier(monsterElement);
            int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

            currentHp = Mathf.Max(0, currentHp - finalDamage);
            UpdateUI();

            // Hit shake
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeRoutine());

            // 속성별 피해 텍스트 색상 및 상성 텍스트
            Color dmgColor;
            string effectText = "";
            float fontSize = 36f;

            if (multiplier >= 1.4f)
            {
                // 유리한 상성 (1.5배)
                effectText = " (1.5배 효과 발군!)";
                dmgColor = new Color(1f, 0.9f, 0.2f); // 황금빛 강조
                fontSize = 42f;
            }
            else if (multiplier <= 0.6f)
            {
                // 불리한 상성 (0.5배)
                effectText = " (0.5배 반감...)";
                dmgColor = new Color(0.7f, 0.7f, 0.7f); // 반감 회색빛
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

            BattleManager.Instance?.SpawnFloatingText(
                $"-{finalDamage}{effectText}",
                transform.position + new Vector3(0, 40f, 0),
                dmgColor, fontSize);

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

            UpdateUI();
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
