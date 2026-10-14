using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DeckGame
{
    public class CardSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public int slotIndex = 0;
        public CardData currentCard;
        public int stackCount = 0;

        [Header("UI Hierarchy")]
        public GameObject cardVisualRoot;
        public Image cardBackground;
        public Image cardBorder;
        public Image elementIcon;
        public Text nameText;
        public Text levelText;
        public Text costText;
        public Text damageText;
        public GameObject stackBadge;
        public Text stackText;
        public GameObject mergeBadge; // 합치기 가능 카드 표시 아이콘
        public Image dropHighlight;

        [Header("Slot Frame")]
        public Image emptySlotBorder;

        public bool IsEmpty => currentCard == null || stackCount <= 0;

        private void Awake()
        {
            UpdateUI();
        }

        public void SetCard(CardData card, int count = 1)
        {
            currentCard = card;
            stackCount = count;
            UpdateUI();
        }

        public bool TryAddCard(CardData card)
        {
            if (IsEmpty)
            {
                SetCard(card, 1);
                return true;
            }

            if (currentCard.CanMergeWith(card))
            {
                stackCount++;
                if (stackCount >= 3)
                {
                    // Trigger Evolution!
                    TriggerEvolution();
                }
                else
                {
                    UpdateUI();
                    PlayStackPunchAnimation();
                    BattleManager.Instance?.SpawnFloatingText($"중첩! (x{stackCount})", transform.position, Color.white, 24f);
                }
                return true;
            }

            return false;
        }

        public void RemoveOneCard()
        {
            if (IsEmpty) return;

            stackCount--;
            if (stackCount <= 0)
            {
                ClearSlot();
            }
            else
            {
                UpdateUI();
            }
        }

        public void ClearSlot()
        {
            currentCard = null;
            stackCount = 0;
            UpdateUI();
        }

        private void TriggerEvolution()
        {
            int oldLevel = currentCard.level;
            currentCard = currentCard.GetEvolved();
            stackCount = 1;
            UpdateUI();

            // Evolution celebration effect
            PlayEvolutionAnimation();

            Color glowColor = currentCard.GetElementColor();
            BattleManager.Instance?.SpawnFloatingText($"진화! Lv.{currentCard.level} {currentCard.GetCardName()}", transform.position + new Vector3(0, 50f, 0), glowColor, 34f, 1.8f);
        }

        public void UpdateUI()
        {
            if (IsEmpty)
            {
                if (cardVisualRoot != null) cardVisualRoot.SetActive(false);
                if (emptySlotBorder != null) emptySlotBorder.gameObject.SetActive(true);
                if (dropHighlight != null) dropHighlight.gameObject.SetActive(false);
                return;
            }

            if (cardVisualRoot != null) cardVisualRoot.SetActive(true);
            if (emptySlotBorder != null) emptySlotBorder.gameObject.SetActive(false);

            Color elemColor = currentCard.GetElementColor();
            Color darkColor = currentCard.GetElementDarkColor();

            if (cardBackground != null) cardBackground.color = darkColor;
            if (cardBorder != null) cardBorder.color = elemColor;
            if (elementIcon != null) elementIcon.color = elemColor;

            if (nameText != null) nameText.text = currentCard.GetCardName();
            if (levelText != null)
            {
                if (currentCard.CanMerge)
                {
                    string stars = new string('★', currentCard.level);
                    levelText.text = $"{stars} Lv.{currentCard.level}";
                    levelText.color = elemColor;
                }
                else
                {
                    levelText.text = "고유";
                    levelText.color = new Color(0.95f, 0.85f, 0.5f);
                }
            }
            if (costText != null) costText.text = $"{currentCard.EnergyCost}";

            // 합치기 가능 카드 아이콘 표시 (카드 상단)
            if (mergeBadge != null)
            {
                mergeBadge.SetActive(currentCard.CanMerge);
            }

            // 대미지 또는 무속성 카드 효과 수치 표시
            if (damageText != null)
            {
                damageText.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (currentCard.IsNeutral)
                {
                    damageText.text = currentCard.GetStatDescription();
                    damageText.color = elemColor;
                }
                else
                {
                    int baseDamage = currentCard.Damage;
                    var bm = BattleManager.Instance != null ? BattleManager.Instance : FindFirstObjectByType<BattleManager>();
                    var monster = bm?.Monster != null ? bm.Monster : FindFirstObjectByType<MonsterController>();
                    if (monster != null)
                    {
                        float mult = currentCard.GetElementMultiplier(monster.monsterElement);
                        int finalDamage = Mathf.RoundToInt(baseDamage * mult);

                        if (mult >= 1.4f)
                        {
                            // 1.5배 유리 상성
                            damageText.text = $"⚔ {finalDamage} <size=14>(1.5x)</size>";
                            damageText.color = new Color(1f, 0.88f, 0.25f);
                        }
                        else if (mult <= 0.6f)
                        {
                            // 0.5배 불리 상성
                            damageText.text = $"⚔ {finalDamage} <size=14>(0.5x)</size>";
                            damageText.color = new Color(0.72f, 0.72f, 0.75f);
                        }
                        else
                        {
                            // 1.0배 동속성
                            damageText.text = $"⚔ {finalDamage}";
                            damageText.color = new Color(1f, 0.95f, 0.5f);
                        }
                    }
                    else
                    {
                        damageText.text = $"⚔ {baseDamage}";
                        damageText.color = new Color(1f, 0.9f, 0.4f);
                    }
                }
            }

            if (stackBadge != null && stackText != null)
            {
                if (stackCount > 1)
                {
                    stackBadge.SetActive(true);
                    stackText.text = $"x{stackCount}";
                }
                else
                {
                    stackBadge.SetActive(false);
                }
            }

            if (dropHighlight != null) dropHighlight.gameObject.SetActive(false);
        }

        private void PlayStackPunchAnimation()
        {
            StartCoroutine(PunchScaleRoutine(1.2f, 0.2f));
        }

        private void PlayEvolutionAnimation()
        {
            StartCoroutine(PunchScaleRoutine(1.4f, 0.35f));
        }

        private IEnumerator PunchScaleRoutine(float targetScale, float duration)
        {
            if (cardVisualRoot == null) yield break;

            Transform target = cardVisualRoot.transform;
            Vector3 originalScale = Vector3.one;
            float half = duration * 0.5f;

            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                target.localScale = Vector3.Lerp(originalScale, Vector3.one * targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                target.localScale = Vector3.Lerp(Vector3.one * targetScale, originalScale, t);
                yield return null;
            }

            target.localScale = originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CardDragHandler.CurrentlyDraggedCard != null && CardDragHandler.CurrentlyDraggedCard.SourceSlot != this)
            {
                CardSlot srcSlot = CardDragHandler.CurrentlyDraggedCard.SourceSlot;
                if (!srcSlot.IsEmpty && !this.IsEmpty)
                {
                    if (this.currentCard.CanMergeWith(srcSlot.currentCard))
                    {
                        if (dropHighlight != null)
                        {
                            dropHighlight.color = new Color(0.2f, 1f, 0.3f, 0.4f);
                            dropHighlight.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (dropHighlight != null)
            {
                dropHighlight.gameObject.SetActive(false);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (CardDragHandler.CurrentlyDraggedCard != null)
            {
                CardDragHandler.CurrentlyDraggedCard.AttemptMergeToSlot(this);
            }
        }
    }
}
