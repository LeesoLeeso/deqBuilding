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
                string stars = new string('★', currentCard.level);
                levelText.text = $"{stars} Lv.{currentCard.level}";
                levelText.color = elemColor;
            }
            if (costText != null) costText.text = $"{currentCard.EnergyCost}";
            if (damageText != null) damageText.text = $"⚔ {currentCard.Damage}";

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
