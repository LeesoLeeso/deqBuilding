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
        public CardSlot[] cardSlots = new CardSlot[7];
        public Transform floatingTextContainer;
        public GameObject floatingTextPrefab;

        [Header("Buttons")]
        public Button turnEndButton;
        public Text turnText;

        [Header("Deck System")]
        public Text drawPileText;
        public Text discardPileText;
        public List<CardData> drawPile = new List<CardData>();
        public List<CardData> discardPile = new List<CardData>();

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

            // Initialize deck with 20 cards, then draw 5 initial cards
            InitializeDeck();
            StartCoroutine(InitialDrawRoutine());
        }

        private void InitializeDeck()
        {
            drawPile.Clear();
            discardPile.Clear();

            // 공격 속성 카드 각 4개씩 (불, 물, 풀)
            for (int i = 0; i < 4; i++) drawPile.Add(new CardData(CardElement.Fire,  1));
            for (int i = 0; i < 4; i++) drawPile.Add(new CardData(CardElement.Water, 1));
            for (int i = 0; i < 4; i++) drawPile.Add(new CardData(CardElement.Grass, 1));

            // 무속성 카드
            for (int i = 0; i < 3; i++) drawPile.Add(new CardData(CardElement.Neutral, 1, NeutralCardType.HealthPotion));
            for (int i = 0; i < 3; i++) drawPile.Add(new CardData(CardElement.Neutral, 1, NeutralCardType.Shield));
            drawPile.Add(new CardData(CardElement.Neutral, 1, NeutralCardType.AimWeakPoint));
            drawPile.Add(new CardData(CardElement.Neutral, 1, NeutralCardType.Taunt));
            // Total: 4+4+4+3+3+1+1 = 20 cards

            ShuffleDeck(drawPile);
            UpdateDeckUI();
        }

        private void ShuffleDeck(List<CardData> deck)
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                CardData tmp = deck[i];
                deck[i] = deck[j];
                deck[j] = tmp;
            }
        }

        private IEnumerator InitialDrawRoutine()
        {
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < 5; i++)
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
                SpawnFloatingText($"손패가 가득 찼습니다! (최대 {cardSlots.Length}칸)", new Vector3(0, -180f, 0), Color.yellow, 24f);
                return false;
            }

            // Reshuffle discard into draw pile if draw pile is empty
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    SpawnFloatingText("더 이상 뽑을 카드가 없습니다!", new Vector3(0, -180f, 0), Color.red, 26f);
                    return false;
                }
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                ShuffleDeck(drawPile);
                SpawnFloatingText("버린 카드를 섞어 덱을 다시 채웠습니다! 🔄", new Vector3(0, 60f, 0), new Color(0.4f, 1f, 0.6f), 26f);
            }

            // Pop top card from draw pile
            CardData newCard = drawPile[drawPile.Count - 1];
            drawPile.RemoveAt(drawPile.Count - 1);

            emptySlot.SetCard(newCard, 1);
            UpdateDeckUI();
            return true;
        }

        public void DiscardCard(CardData card)
        {
            discardPile.Add(card);
            UpdateDeckUI();
        }

        private void UpdateDeckUI()
        {
            if (drawPileText != null)    drawPileText.text    = $"{drawPile.Count}";
            if (discardPileText != null) discardPileText.text = $"{discardPile.Count}";
        }

        public void OnTurnEndClicked()
        {
            StartCoroutine(TurnEndRoutine());
        }

        private IEnumerator TurnEndRoutine()
        {
            if (turnEndButton != null) turnEndButton.interactable = false;

            // 1. Monster Turn 실행
            SpawnFloatingText("적의 턴!", Monster.transform.position + new Vector3(0, 50f, 0), Color.red, 32f);
            yield return new WaitForSeconds(0.8f);

            if (Monster != null && Monster.currentHp > 0 && Player != null)
            {
                switch (Monster.nextIntent)
                {
                    case MonsterIntentType.Attack:
                        Player.TakeDamage(Monster.nextAttackDamage);
                        break;
                    case MonsterIntentType.Defend:
                        Monster.GainShield(15);
                        break;
                    case MonsterIntentType.Buff:
                        Monster.nextAttackDamage += 4;
                        SpawnFloatingText("🔥 적 공격력 +4 강화!", Monster.transform.position + new Vector3(0, 50f, 0), Color.red, 32f);
                        Monster.UpdateUI();
                        break;
                }
            }

            yield return new WaitForSeconds(0.8f);

            // 2. 적 공격 턴 종료 후: 플레이어 방어도 리셋 (규칙: 다음 적 공격 턴까지 적용 후 사라짐)
            if (Player != null)
            {
                Player.ResetShield();
                if (Player.critBuffTurns > 0)
                {
                    Player.critBuffTurns--;
                    Player.UpdateUI();
                }
            }

            // 3. 몬스터 도발 카운트다운 및 다음 행동 주사위
            if (Monster != null && Monster.currentHp > 0)
            {
                if (Monster.tauntTurns > 0)
                {
                    Monster.tauntTurns--;
                }
                Monster.RollNextIntent();
                Monster.UpdateUI();
            }

            // 4. Player Turn 시작
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
