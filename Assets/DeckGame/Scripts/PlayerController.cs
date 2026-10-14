using UnityEngine;
using UnityEngine.UI;

namespace DeckGame
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Stats")]
        public int maxHp = 100;
        public int currentHp = 100;
        public int currentShield = 0; // 방어도로 획득한 실드
        public int maxEnergy = 3;
        public int currentEnergy = 3;

        [Header("Buffs")]
        public int critBuffTurns = 0; // 급소 노리기 지속 턴 수
        public float baseCritChance = 0.20f; // 기본 치명타율 20%

        [Header("UI References")]
        public Slider hpSlider;
        public Text hpText;
        public Text energyText;
        public Text statusText;
        public Image[] energyOrbs;

        public int CurrentEnergy => currentEnergy;

        public float CurrentCritChance => critBuffTurns > 0 ? baseCritChance * 1.5f : baseCritChance;

        private void Awake()
        {
            currentHp = maxHp;
            currentShield = 0;
            currentEnergy = maxEnergy;
            critBuffTurns = 0;
            UpdateUI();
        }

        public void ConsumeEnergy(int amount)
        {
            currentEnergy = Mathf.Max(0, currentEnergy - amount);
            UpdateUI();
        }

        public void ResetEnergy()
        {
            currentEnergy = maxEnergy;
            UpdateUI();
        }

        public void Heal(int amount)
        {
            int oldHp = currentHp;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            int recovered = currentHp - oldHp;
            UpdateUI();

            BattleManager.Instance?.SpawnFloatingText($"+{recovered} 체력 회복!", transform.position + new Vector3(0, 45f, 0), new Color(0.2f, 1f, 0.4f), 32f);
        }

        public void GainShield(int amount)
        {
            currentShield += amount;
            UpdateUI();

            BattleManager.Instance?.SpawnFloatingText($"+{amount} 방어도!", transform.position + new Vector3(0, 45f, 0), new Color(0.3f, 0.8f, 1f), 32f);
        }

        /// <summary>
        /// 적의 공격 턴이 끝나면 방어도는 사라집니다.
        /// </summary>
        public void ResetShield()
        {
            if (currentShield > 0)
            {
                currentShield = 0;
                UpdateUI();
            }
        }

        public void ApplyCritBuff(int turns = 3)
        {
            critBuffTurns = turns;
            UpdateUI();

            BattleManager.Instance?.SpawnFloatingText($"🎯 급소 노리기! 치명타 x1.5 ({turns}턴)", transform.position + new Vector3(0, 50f, 0), new Color(1f, 0.85f, 0.2f), 32f);
        }

        public void TakeDamage(int damage)
        {
            int damageAfterShield = damage;
            if (currentShield > 0)
            {
                if (currentShield >= damage)
                {
                    currentShield -= damage;
                    damageAfterShield = 0;
                    BattleManager.Instance?.SpawnFloatingText($"방어 완벽 흡수! (남은 방패: {currentShield})", transform.position + new Vector3(0, 50f, 0), Color.cyan, 32f);
                }
                else
                {
                    damageAfterShield = damage - currentShield;
                    int absorbed = currentShield;
                    currentShield = 0;
                    BattleManager.Instance?.SpawnFloatingText($"방어 {absorbed} 흡수! (-{damageAfterShield} 피해)", transform.position + new Vector3(0, 50f, 0), new Color(1f, 0.6f, 0.3f), 32f);
                }
            }

            if (damageAfterShield > 0)
            {
                currentHp = Mathf.Max(0, currentHp - damageAfterShield);
                BattleManager.Instance?.SpawnFloatingText($"-{damageAfterShield}", transform.position + new Vector3(0, 50f, 0), Color.red, 34f);
            }

            UpdateUI();
        }

        public void UpdateUI()
        {
            if (hpSlider != null)
            {
                hpSlider.maxValue = maxHp;
                hpSlider.value = currentHp;
            }

            if (hpText != null)
            {
                if (currentShield > 0)
                {
                    hpText.text = $"{currentHp} / {maxHp}  <color=#44CCFF>[🛡️{currentShield}]</color>";
                }
                else
                {
                    hpText.text = $"{currentHp} / {maxHp}";
                }
            }

            if (energyText != null)
            {
                energyText.text = $"⚡ {currentEnergy} / {maxEnergy}";
            }

            if (statusText != null)
            {
                string status = "";
                if (critBuffTurns > 0)
                {
                    status += $"<color=#FFD700>🎯 급소노리기 ({critBuffTurns}턴: 치명타 {(int)(CurrentCritChance * 100)}%)</color> ";
                }
                if (currentShield > 0)
                {
                    status += $"<color=#44CCFF>🛡️ 방어도 {currentShield}</color>";
                }
                statusText.text = status;
            }

            if (energyOrbs != null)
            {
                for (int i = 0; i < energyOrbs.Length; i++)
                {
                    if (energyOrbs[i] != null)
                    {
                        energyOrbs[i].color = i < currentEnergy ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    }
                }
            }
        }
    }
}
