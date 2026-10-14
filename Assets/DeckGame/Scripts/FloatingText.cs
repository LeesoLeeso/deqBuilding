using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DeckGame
{
    public class FloatingText : MonoBehaviour
    {
        public Text textComponent;
        private float _duration = 1.0f;
        private float _elapsed = 0f;
        private Vector3 _velocity = new Vector3(0, 100f, 0);

        public void Initialize(string message, Color color, float fontSize = 28f, float duration = 1.2f)
        {
            if (textComponent == null) textComponent = GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = message;
                textComponent.color = color;
                textComponent.fontSize = (int)fontSize;
            }
            _duration = duration;
            StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            RectTransform rect = GetComponent<RectTransform>();
            Vector3 startPos = rect.localPosition;
            Color startColor = textComponent != null ? textComponent.color : Color.white;

            while (_elapsed < _duration)
            {
                _elapsed += Time.deltaTime;
                float t = _elapsed / _duration;

                rect.localPosition = startPos + _velocity * _elapsed;

                if (textComponent != null)
                {
                    Color c = startColor;
                    c.a = Mathf.Lerp(1f, 0f, t * t);
                    textComponent.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
