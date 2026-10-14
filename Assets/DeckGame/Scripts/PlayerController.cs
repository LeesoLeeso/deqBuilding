using UnityEngine;
using UnityEngine.UI;

namespace DeckGame
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Stats")]
        public int maxHp = 100;
        public int currentHp = 100;
        public int maxEnergy = 3;
        public int currentEnergy = 3;

        [Header("UI References")]
        public Slider hpSlider;
        public Text hpText;
        public Text energyText;
        public Image[] energyOrbs;

        public int CurrentEnergy => currentEnergy;

        private void Awake()
        {
            currentHp = maxHp;
            currentEnergy = maxEnergy;
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

        public void TakeDamage(int damage)
        {
            currentHp = Mathf.Max(0, currentHp - damage);
            UpdateUI();

            BattleManager.Instance?.SpawnFloatingText($"-{damage}", transform.position + new Vector3(0, 50f, 0), Color.red, 34f);
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
                hpText.text = $"{currentHp} / {maxHp}";
            }
            if (energyText != null)
            {
                energyText.text = $"⚡ {currentEnergy} / {maxEnergy}";
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
