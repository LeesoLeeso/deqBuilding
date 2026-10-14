using System;
using UnityEngine;

namespace DeckGame
{
    public enum CardElement
    {
        Water,   // 물
        Grass,   // 풀
        Fire,    // 불
        Neutral  // 무속성
    }

    public enum NeutralCardType
    {
        None,          // 일반 속성 카드 (물/풀/불)
        HealthPotion,  // 체력 포션 (합치기 가능)
        Shield,        // 방어 (합치기 가능)
        AimWeakPoint,  // 급소 노리기 (합치기 불가)
        Taunt          // 도발 (합치기 불가)
    }

    [System.Serializable]
    public class CardData
    {
        public CardElement element;
        public NeutralCardType neutralType = NeutralCardType.None;
        public int level = 1;

        public CardData(CardElement element, int level = 1, NeutralCardType neutralType = NeutralCardType.None)
        {
            this.element = element;
            this.level = Mathf.Max(1, level);
            this.neutralType = neutralType;
        }

        public bool IsNeutral => element == CardElement.Neutral;

        /// <summary>
        /// 카드 합치기(조합) 가능 여부.
        /// 체력 포션, 방어, 일반 속성 카드는 가능 / 급소 노리기, 도발은 불가.
        /// </summary>
        public bool CanMerge
        {
            get
            {
                if (IsNeutral)
                {
                    if (neutralType == NeutralCardType.AimWeakPoint || neutralType == NeutralCardType.Taunt)
                        return false;
                    return true;
                }
                return true;
            }
        }

        public int EnergyCost
        {
            get
            {
                // 레벨에 관계없이 모든 카드는 에너지 1 소모
                return 1;
            }
        }

        public int Damage
        {
            get
            {
                if (IsNeutral) return 0;

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

        /// <summary>
        /// 체력 포션 회복량 (Lv1: 1, Lv2: 5, Lv3: 20)
        /// </summary>
        public int HealAmount
        {
            get
            {
                switch (level)
                {
                    case 1: return 1;
                    case 2: return 5;
                    case 3: return 20;
                    default: return 20 + (level - 3) * 15;
                }
            }
        }

        /// <summary>
        /// 방어 카드 방어도 수치 (Lv1: 3, Lv2: 10, Lv3: 45)
        /// </summary>
        public int ShieldAmount
        {
            get
            {
                switch (level)
                {
                    case 1: return 3;
                    case 2: return 10;
                    case 3: return 45;
                    default: return 45 + (level - 3) * 35;
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
                case CardElement.Neutral: return "무속성";
                default: return "";
            }
        }

        public string GetCardName()
        {
            if (IsNeutral)
            {
                switch (neutralType)
                {
                    case NeutralCardType.HealthPotion:
                        return $"체력 포션";
                    case NeutralCardType.Shield:
                        return $"방어";
                    case NeutralCardType.AimWeakPoint:
                        return "급소 노리기";
                    case NeutralCardType.Taunt:
                        return "도발";
                    default:
                        return "무속성 카드";
                }
            }

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

        /// <summary>
        /// 카드 하단에 표시될 수치/효과 설명 문자열
        /// </summary>
        public string GetStatDescription()
        {
            if (IsNeutral)
            {
                switch (neutralType)
                {
                    case NeutralCardType.HealthPotion:
                        return $"💚 +{HealAmount} 회복";
                    case NeutralCardType.Shield:
                        return $"🛡️ +{ShieldAmount} 방어";
                    case NeutralCardType.AimWeakPoint:
                        return "🎯 치명타 1.5x (3턴)";
                    case NeutralCardType.Taunt:
                        return "💢 공격 강제 (3턴)";
                    default:
                        return "";
                }
            }
            return $"⚔ {Damage}";
        }

        public Color GetElementColor()
        {
            if (IsNeutral)
            {
                switch (neutralType)
                {
                    case NeutralCardType.HealthPotion:
                        return new Color(1f, 0.42f, 0.55f, 1f); // 핑크/레드
                    case NeutralCardType.Shield:
                        return new Color(0.28f, 0.75f, 0.95f, 1f); // 스틸 시안
                    case NeutralCardType.AimWeakPoint:
                        return new Color(1f, 0.78f, 0.22f, 1f); // 크리티컬 골드
                    case NeutralCardType.Taunt:
                        return new Color(0.85f, 0.40f, 0.88f, 1f); // 도발 마젠타
                    default:
                        return new Color(0.82f, 0.85f, 0.90f, 1f);
                }
            }

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
            if (IsNeutral)
            {
                return new Color(0.14f, 0.16f, 0.22f, 1f);
            }

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
            if (!this.CanMerge || !other.CanMerge) return false;
            if (this.element != other.element) return false;
            if (this.neutralType != other.neutralType) return false;
            return other.level == this.level;
        }

        /// <summary>
        /// 이 카드의 속성으로 공격할 때 대상 몬스터의 속성에 따른 대미지 배율을 반환합니다.
        /// 불→풀 1.5x / 물→불 1.5x / 풀→물 1.5x
        /// 동속성 1.0x
        /// 불→물 0.5x / 물→풀 0.5x / 풀→불 0.5x
        /// </summary>
        public float GetElementMultiplier(CardElement defenderElement)
        {
            if (IsNeutral) return 1f;

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
            if (IsNeutral)
            {
                return new CardData(element, level + 1, neutralType);
            }
            return new CardData(element, level + 1);
        }
    }
}
