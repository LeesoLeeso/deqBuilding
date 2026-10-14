using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DeckGame
{
    /// <summary>
    /// 플레이어 자신 대상 카드 드롭존.
    /// 카드가 이 영역에 드롭되면 카드 효과가 플레이어 본인에게 적용됩니다.
    /// </summary>
    public class PlayerDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI")]
        public Image backgroundImage;

        private Color _normalColor;
        private Color _hoverColor;

        private void Awake()
        {
            backgroundImage = GetComponent<Image>();
            if (backgroundImage != null)
            {
                _normalColor = backgroundImage.color;
                _hoverColor  = new Color(0.3f, 0.7f, 1f, 0.45f);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (CardDragHandler.CurrentlyDraggedCard != null)
            {
                CardDragHandler.CurrentlyDraggedCard.AttemptUseOnTarget(null, BattleManager.Instance?.Player);
            }
            SetHighlight(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CardDragHandler.CurrentlyDraggedCard != null)
                SetHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        private void SetHighlight(bool active)
        {
            if (backgroundImage != null)
                backgroundImage.color = active ? _hoverColor : _normalColor;
        }
    }
}
