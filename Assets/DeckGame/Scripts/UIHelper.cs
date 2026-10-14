using UnityEngine;
using UnityEngine.UI;

namespace DeckGame
{
    public static class UIHelper
    {
        private static Sprite _roundedRectSprite;
        private static Sprite _circleSprite;
        private static Sprite _monsterSprite;

        public static Sprite GetRoundedRectSprite(int width = 128, int height = 128, int cornerRadius = 16)
        {
            if (_roundedRectSprite != null) return _roundedRectSprite;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[width * height];

            float r = cornerRadius;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(r - x, x - (width - 1 - r)));
                    float dy = Mathf.Max(0, Mathf.Max(r - y, y - (height - 1 - r)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > r)
                    {
                        pixels[y * width + x] = new Color32(255, 255, 255, 0);
                    }
                    else if (dist > r - 1f)
                    {
                        byte alpha = (byte)(255 * (r - dist));
                        pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                    }
                    else
                    {
                        pixels[y * width + x] = new Color32(255, 255, 255, 255);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            _roundedRectSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
            return _roundedRectSprite;
        }

        public static Sprite GetCircleSprite(int size = 128)
        {
            if (_circleSprite != null) return _circleSprite;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[size * size];

            float center = (size - 1) * 0.5f;
            float radius = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > radius)
                    {
                        pixels[y * size + x] = new Color32(255, 255, 255, 0);
                    }
                    else if (dist > radius - 1f)
                    {
                        byte alpha = (byte)(255 * (radius - dist));
                        pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = new Color32(255, 255, 255, 255);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _circleSprite;
        }

        public static Sprite GetMonsterSprite(int size = 256)
        {
            if (_monsterSprite != null) return _monsterSprite;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[size * size];

            // Procedural menacing monster avatar
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / center;
                    float ny = (y - center) / center;

                    // Body ellipse
                    float bodyDist = (nx * nx) / (0.7f * 0.7f) + (ny * ny) / (0.8f * 0.8f);
                    
                    // Horns
                    bool hornLeft = (nx < -0.3f && nx > -0.8f && ny > 0.3f && ny < 0.9f && Mathf.Abs(ny - (-nx * 1.2f)) < 0.25f);
                    bool hornRight = (nx > 0.3f && nx < 0.8f && ny > 0.3f && ny < 0.9f && Mathf.Abs(ny - (nx * 1.2f)) < 0.25f);

                    // Eyes
                    bool eyeLeft = (Mathf.Abs(nx - (-0.25f)) < 0.08f && Mathf.Abs(ny - 0.15f) < 0.05f);
                    bool eyeRight = (Mathf.Abs(nx - 0.25f) < 0.08f && Mathf.Abs(ny - 0.15f) < 0.05f);

                    // Mouth
                    bool mouth = (Mathf.Abs(nx) < 0.35f && ny < -0.2f && ny > -0.35f && (Mathf.Abs(ny + 0.25f) < 0.06f));

                    if (eyeLeft || eyeRight)
                    {
                        // Glowing red eyes
                        pixels[y * size + x] = new Color32(255, 60, 40, 255);
                    }
                    else if (mouth)
                    {
                        pixels[y * size + x] = new Color32(20, 20, 25, 255);
                    }
                    else if (bodyDist <= 1.0f || hornLeft || hornRight)
                    {
                        // Deep dark purple-crimson body shading
                        byte shade = (byte)(80 + (int)(ny * 40));
                        pixels[y * size + x] = new Color32((byte)(shade + 30), 30, (byte)(shade + 50), 255);
                    }
                    else
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            _monsterSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _monsterSprite;
        }
    }
}
