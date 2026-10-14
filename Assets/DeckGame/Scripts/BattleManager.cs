using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeckGame
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("References")]
        public PlayerController Player;
        public MonsterController Monster;
        public CardSlot[] cardSlots = new CardSlot[5];
        public Transform floatingTextContainer;
        public GameObject floatingTextPrefab;

        [Header("Buttons")]
        public Button turnEndButton;
        public Text turnText;

        public int currentTurn = 1;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (turnEndButton != null) turnEndButton.onClick.AddListener(OnTurnEndClicked);

            StartBattle();
        }

        public void StartBattle()
        {
            currentTurn = 1;
            UpdateTurnUI();

            // Clear all slots
            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (cardSlots[i] != null) cardSlots[i].ClearSlot();
            }

            // Draw 4 initial cards
            StartCoroutine(InitialDrawRoutine());
        }

        private IEnumerator InitialDrawRoutine()
        {
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < 4; i++)
            {
                DrawRandomCard();
                yield return new WaitForSeconds(0.1f);
            }
        }

        public bool DrawRandomCard()
        {
            // Find first empty slot
            CardSlot emptySlot = null;
            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (cardSlots[i] != null && cardSlots[i].IsEmpty)
                {
                    emptySlot = cardSlots[i];
                    break;
                }
            }

            if (emptySlot == null)
            {
                SpawnFloatingText("손패가 가득 찼습니다! (최대 5칸)", new Vector3(0, -180f, 0), Color.yellow, 24f);
                return false;
            }

            // Random element (Water, Grass, Fire) Lv.1
            CardElement randomElem = (CardElement)Random.Range(0, 3);
            CardData newCard = new CardData(randomElem, 1);

            emptySlot.SetCard(newCard, 1);
            return true;
        }

        public void OnTurnEndClicked()
        {
            StartCoroutine(TurnEndRoutine());
        }

        private IEnumerator TurnEndRoutine()
        {
            if (turnEndButton != null) turnEndButton.interactable = false;

            // Monster Turn
            SpawnFloatingText("적의 턴!", Monster.transform.position + new Vector3(0, 50f, 0), Color.red, 32f);
            yield return new WaitForSeconds(0.8f);

            if (Monster != null && Monster.currentHp > 0 && Player != null)
            {
                Player.TakeDamage(Monster.nextAttackDamage);
            }

            yield return new WaitForSeconds(0.8f);

            // Player Turn
            currentTurn++;
            UpdateTurnUI();

            if (Player != null) Player.ResetEnergy();

            // Draw up to 2 cards
            int drawn = 0;
            for (int i = 0; i < 2; i++)
            {
                if (DrawRandomCard()) drawn++;
                yield return new WaitForSeconds(0.12f);
            }

            SpawnFloatingText($"턴 {currentTurn} 시작! (에너지 충전 & 카드 +{drawn})", new Vector3(0, 0, 0), Color.cyan, 30f);

            if (turnEndButton != null) turnEndButton.interactable = true;
        }

        private void UpdateTurnUI()
        {
            if (turnText != null) turnText.text = $"TURN {currentTurn}";
        }

        public void SpawnFloatingText(string message, Vector3 worldPos, Color color, float fontSize = 28f, float duration = 1.2f)
        {
            if (floatingTextContainer == null) return;

            GameObject go = new GameObject("FloatingText", typeof(RectTransform), typeof(Text), typeof(FloatingText));
            go.transform.SetParent(floatingTextContainer, false);
            go.transform.position = worldPos;

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 60);

            FloatingText ft = go.GetComponent<FloatingText>();
            ft.Initialize(message, color, fontSize, duration);
        }
    }
}
