using System;
using UnityEngine;

namespace DeckGame
{
    public enum CardElement
    {
        Water,  // 물
        Grass,  // 풀
        Fire    // 불
    }

    [System.Serializable]
    public class CardData
    {
        public CardElement element;
        public int level = 1;

        public CardData(CardElement element, int level = 1)
        {
            this.element = element;
            this.level = Mathf.Max(1, level);
        }

        public int EnergyCost
        {
            get
            {
                // Lv1 = 1, Lv2 = 2, Lv3 = 3
                return Mathf.Clamp(level, 1, 5);
            }
        }

        public int Damage
        {
            get
            {
                // Lv1 = 10, Lv2 = 25, Lv3 = 60
                switch (level)
                {
                    case 1: return 10;
                    case 2: return 25;
                    case 3: return 60;
                    default: return 60 + (level - 3) * 40;
                }
            }
        }

        public string GetElementName()
        {
            switch (element)
            {
                case CardElement.Water: return "물";
                case CardElement.Grass: return "풀";
                case CardElement.Fire: return "불";
                default: return "";
            }
        }

        public string GetCardName()
        {
            switch (element)
            {
                case CardElement.Water:
                    if (level == 1) return "물 화살";
                    if (level == 2) return "거친 해일";
                    return "절대영도";
                case CardElement.Grass:
                    if (level == 1) return "덩굴 채찍";
                    if (level == 2) return "가시 폭풍";
                    return "세계수의 분노";
                case CardElement.Fire:
                    if (level == 1) return "작은 불꽃";
                    if (level == 2) return "화염 폭발";
                    return "지옥의 겁화";
                default:
                    return $"{GetElementName()} Lv.{level}";
            }
        }

        public Color GetElementColor()
        {
            switch (element)
            {
                case CardElement.Water:
                    return new Color(0.18f, 0.55f, 0.95f, 1f); // 산뜻한 파랑
                case CardElement.Grass:
                    return new Color(0.20f, 0.75f, 0.35f, 1f); // 생생한 초록
                case CardElement.Fire:
                    return new Color(0.92f, 0.28f, 0.22f, 1f); // 강렬한 빨강
                default:
                    return Color.white;
            }
        }

        public Color GetElementDarkColor()
        {
            switch (element)
            {
                case CardElement.Water:
                    return new Color(0.08f, 0.25f, 0.45f, 1f);
                case CardElement.Grass:
                    return new Color(0.10f, 0.35f, 0.18f, 1f);
                case CardElement.Fire:
                    return new Color(0.45f, 0.12f, 0.10f, 1f);
                default:
                    return Color.gray;
            }
        }

        public bool CanMergeWith(CardData other)
        {
            if (other == null) return false;
            return other.element == this.element && other.level == this.level;
        }

        /// <summary>
        /// 이 카드의 속성으로 공격할 때 대상 몬스터의 속성에 따른 대미지 배율을 반환합니다.
        /// 불→풀 1.5x / 물→불 1.5x / 풀→물 1.5x
        /// 동속성 1.0x
        /// 불→물 0.5x / 물→풀 0.5x / 풀→불 0.5x
        /// </summary>
        public float GetElementMultiplier(CardElement defenderElement)
        {
            // 동속성: 1배
            if (element == defenderElement) return 1f;

            // 유리한 상성 (1.5배)
            if (element == CardElement.Fire  && defenderElement == CardElement.Grass) return 1.5f;
            if (element == CardElement.Water && defenderElement == CardElement.Fire)  return 1.5f;
            if (element == CardElement.Grass && defenderElement == CardElement.Water) return 1.5f;

            // 불리한 상성 (0.5배)
            return 0.5f;
        }

        public CardData GetEvolved()
        {
            return new CardData(element, level + 1);
        }
    }
}
