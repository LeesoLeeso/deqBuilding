using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DeckGame.Editor
{
    public static class BattleSceneBuilder
    {
        [MenuItem("DeckGame/Build Battle Scene")]
        public static void BuildBattleScene()
        {
            // Clear existing objects in the active scene
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                Object.DestroyImmediate(go);
            }

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Sprite roundedSprite = UIHelper.GetRoundedRectSprite(128, 128, 16);
            Sprite circleSprite = UIHelper.GetCircleSprite(128);
            Sprite monsterSprite = UIHelper.GetMonsterSprite(256);

            // 1. Camera
            GameObject camGo = new GameObject("Main Camera");
            Camera cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.14f, 1f); // Deep dark battle slate
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camGo.transform.position = new Vector3(0, 0, -10f);

            // 2. EventSystem
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // 3. Canvas
            GameObject canvasGo = new GameObject("BattleCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1.0f; // Lock to height to preserve vertical layout across any window size
            canvasGo.AddComponent<GraphicRaycaster>();

            // 4. Background (Raycast disabled so it never blocks UI)
            GameObject bgGo = CreateUIObject("Background", canvasGo.transform);
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.15f, 1f);
            bgImg.raycastTarget = false;

            // Subtle arena circle on ground (Raycast disabled)
            GameObject arenaGo = CreateUIObject("ArenaCircle", canvasGo.transform);
            RectTransform arenaRect = arenaGo.GetComponent<RectTransform>();
            arenaRect.anchorMin = new Vector2(0.5f, 0.65f);
            arenaRect.anchorMax = new Vector2(0.5f, 0.65f);
            arenaRect.anchoredPosition = Vector2.zero;
            arenaRect.sizeDelta = new Vector2(650, 280);
            Image arenaImg = arenaGo.AddComponent<Image>();
            arenaImg.sprite = circleSprite;
            arenaImg.color = new Color(0.12f, 0.16f, 0.24f, 0.45f);
            arenaImg.raycastTarget = false;

            // 5. BattleManager Root
            GameObject managerGo = new GameObject("BattleManager");
            BattleManager battleMgr = managerGo.AddComponent<BattleManager>();

            // 6. Player Controller Root (Top Left)
            GameObject playerGo = CreateUIObject("PlayerUI", canvasGo.transform);
            PlayerController playerCtrl = playerGo.AddComponent<PlayerController>();
            RectTransform playerRect = playerGo.GetComponent<RectTransform>();
            playerRect.anchorMin = new Vector2(0f, 1f);
            playerRect.anchorMax = new Vector2(0f, 1f);
            playerRect.pivot = new Vector2(0f, 1f);
            playerRect.anchoredPosition = new Vector2(40f, -30f);
            playerRect.sizeDelta = new Vector2(360f, 155f);

            // Player Background Box
            Image playerBoxImg = playerGo.AddComponent<Image>();
            playerBoxImg.sprite = roundedSprite;
            playerBoxImg.type = Image.Type.Sliced;
            playerBoxImg.color = new Color(0.12f, 0.15f, 0.22f, 0.9f);
            playerBoxImg.raycastTarget = false;

            // Player Label
            GameObject pLabel = CreateText("Label", playerGo.transform, "플레이어 (PLAYER)", defaultFont, 20, FontStyle.Bold, new Color(0.4f, 0.85f, 1f), TextAnchor.MiddleLeft);
            RectTransform pLabelRect = pLabel.GetComponent<RectTransform>();
            pLabelRect.anchoredPosition = new Vector2(20f, -24f);
            pLabelRect.sizeDelta = new Vector2(200f, 30f);

            // Player Status / Buff Text
            GameObject pStatusGo = CreateText("StatusText", playerGo.transform, "", defaultFont, 13, FontStyle.Bold, Color.yellow, TextAnchor.MiddleLeft);
            RectTransform pStatusRect = pStatusGo.GetComponent<RectTransform>();
            pStatusRect.anchoredPosition = new Vector2(20f, -126f);
            pStatusRect.sizeDelta = new Vector2(320f, 22f);
            playerCtrl.statusText = pStatusGo.GetComponent<Text>();

            // Player HP Slider
            Slider pSlider = CreateHpBar("PlayerHpBar", playerGo.transform, new Vector2(20f, -58f), new Vector2(320f, 22f), new Color(0.18f, 0.75f, 0.4f), roundedSprite);
            playerCtrl.hpSlider = pSlider;

            // Player HP Text
            GameObject pHpTextGo = CreateText("HpText", playerGo.transform, "100 / 100", defaultFont, 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform pHpTextRect = pHpTextGo.GetComponent<RectTransform>();
            pHpTextRect.anchoredPosition = new Vector2(20f, -58f);
            pHpTextRect.sizeDelta = new Vector2(320f, 22f);
            playerCtrl.hpText = pHpTextGo.GetComponent<Text>();

            // Player Energy Box
            GameObject energyGo = CreateUIObject("EnergyBox", playerGo.transform);
            RectTransform energyRect = energyGo.GetComponent<RectTransform>();
            energyRect.anchoredPosition = new Vector2(20f, -92f);
            energyRect.sizeDelta = new Vector2(320f, 30f);

            GameObject energyTextGo = CreateText("EnergyText", energyGo.transform, "⚡ 3 / 3", defaultFont, 20, FontStyle.Bold, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleLeft);
            RectTransform energyTextRect = energyTextGo.GetComponent<RectTransform>();
            energyTextRect.anchoredPosition = new Vector2(0, 0);
            energyTextRect.sizeDelta = new Vector2(120f, 30f);
            playerCtrl.energyText = energyTextGo.GetComponent<Text>();

            // Energy Orbs
            Image[] orbs = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject orbGo = CreateUIObject($"Orb_{i}", energyGo.transform);
                RectTransform orbRect = orbGo.GetComponent<RectTransform>();
                orbRect.anchoredPosition = new Vector2(130f + i * 36f, 0);
                orbRect.sizeDelta = new Vector2(26f, 26f);
                Image orbImg = orbGo.AddComponent<Image>();
                orbImg.sprite = circleSprite;
                orbImg.color = new Color(1f, 0.85f, 0.2f, 1f);
                orbImg.raycastTarget = false;
                orbs[i] = orbImg;
            }
            playerCtrl.energyOrbs = orbs;

            // 7. Top Turn Display
            GameObject turnGo = CreateUIObject("TurnBox", canvasGo.transform);
            RectTransform turnRect = turnGo.GetComponent<RectTransform>();
            turnRect.anchorMin = new Vector2(0.5f, 1f);
            turnRect.anchorMax = new Vector2(0.5f, 1f);
            turnRect.pivot = new Vector2(0.5f, 1f);
            turnRect.anchoredPosition = new Vector2(0, -30f);
            turnRect.sizeDelta = new Vector2(220f, 50f);

            Image turnBg = turnGo.AddComponent<Image>();
            turnBg.sprite = roundedSprite;
            turnBg.type = Image.Type.Sliced;
            turnBg.color = new Color(0.14f, 0.17f, 0.26f, 0.9f);
            turnBg.raycastTarget = false;

            GameObject turnTextGo = CreateText("TurnText", turnGo.transform, "TURN 1", defaultFont, 24, FontStyle.Bold, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            RectTransform turnTextRect = turnTextGo.GetComponent<RectTransform>();
            turnTextRect.anchoredPosition = Vector2.zero;
            turnTextRect.sizeDelta = new Vector2(220f, 50f);
            battleMgr.turnText = turnTextGo.GetComponent<Text>();

            // 8. Monster Area (Upper Center of screen, comfortably above hand)
            GameObject monsterGo = CreateUIObject("Monster", canvasGo.transform);
            MonsterController monsterCtrl = monsterGo.AddComponent<MonsterController>();
            RectTransform monsterRect = monsterGo.GetComponent<RectTransform>();
            monsterRect.anchorMin = new Vector2(0.5f, 0.65f);
            monsterRect.anchorMax = new Vector2(0.5f, 0.65f);
            monsterRect.pivot = new Vector2(0.5f, 0.5f);
            monsterRect.anchoredPosition = Vector2.zero;
            monsterRect.sizeDelta = new Vector2(380f, 360f);

            // 몬스터 전역 드롭 영역 (380x360 전체에서 카드 드롭을 감지하도록 투명 Image 추가)
            Image monsterAreaImg = monsterGo.AddComponent<Image>();
            monsterAreaImg.color = new Color(0, 0, 0, 0.005f);
            monsterAreaImg.raycastTarget = true;

            // Highlight Ring for drop target
            GameObject highlightGo = CreateUIObject("HighlightRing", monsterGo.transform);
            RectTransform hlRect = highlightGo.GetComponent<RectTransform>();
            hlRect.anchoredPosition = new Vector2(0, 30f);
            hlRect.sizeDelta = new Vector2(280f, 280f);
            Image hlImg = highlightGo.AddComponent<Image>();
            hlImg.sprite = circleSprite;
            hlImg.color = new Color(1f, 0.3f, 0.2f, 0.35f);
            hlImg.raycastTarget = false;
            highlightGo.SetActive(false);
            monsterCtrl.highlightRing = hlImg;

            // Monster Avatar (RaycastTarget true so drop is received)
            GameObject avatarGo = CreateUIObject("Avatar", monsterGo.transform);
            RectTransform avatarRect = avatarGo.GetComponent<RectTransform>();
            avatarRect.anchoredPosition = new Vector2(0, 30f);
            avatarRect.sizeDelta = new Vector2(200f, 200f);
            Image avatarImg = avatarGo.AddComponent<Image>();
            avatarImg.sprite = monsterSprite;
            avatarImg.preserveAspect = true;
            avatarImg.raycastTarget = true;
            monsterCtrl.avatarImage = avatarImg;

            // Monster Name
            GameObject mNameGo = CreateText("MonsterName", monsterGo.transform, "불꽃 골렘 (Flame Golem)", defaultFont, 24, FontStyle.Bold, new Color(1f, 0.45f, 0.3f), TextAnchor.MiddleCenter);
            RectTransform mNameRect = mNameGo.GetComponent<RectTransform>();
            mNameRect.anchoredPosition = new Vector2(0, -95f);
            mNameRect.sizeDelta = new Vector2(340f, 30f);
            monsterCtrl.nameText = mNameGo.GetComponent<Text>();

            // Monster HP Slider
            Slider mSlider = CreateHpBar("MonsterHpBar", monsterGo.transform, new Vector2(0, -130f), new Vector2(340f, 24f), new Color(0.9f, 0.22f, 0.22f), roundedSprite);
            monsterCtrl.hpSlider = mSlider;

            // Monster HP Text
            GameObject mHpTextGo = CreateText("HpText", monsterGo.transform, "150 / 150", defaultFont, 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform mHpTextRect = mHpTextGo.GetComponent<RectTransform>();
            mHpTextRect.anchoredPosition = new Vector2(0, -130f);
            mHpTextRect.sizeDelta = new Vector2(340f, 24f);
            monsterCtrl.hpText = mHpTextGo.GetComponent<Text>();

            // Monster Intent Text
            GameObject intentGo = CreateText("IntentText", monsterGo.transform, "⚔ 다음 행동: 12 피해", defaultFont, 18, FontStyle.Bold, new Color(1f, 0.8f, 0.25f), TextAnchor.MiddleCenter);
            RectTransform intentRect = intentGo.GetComponent<RectTransform>();
            intentRect.anchoredPosition = new Vector2(0, -162f);
            intentRect.sizeDelta = new Vector2(340f, 24f);
            monsterCtrl.intentText = intentGo.GetComponent<Text>();

            // Monster Element Toggle Buttons (Right side of Monster for instant testing)
            GameObject elemGroup = CreateUIObject("ElementToggleGroup", canvasGo.transform);
            RectTransform egRect = elemGroup.GetComponent<RectTransform>();
            egRect.anchorMin = new Vector2(0.5f, 0.65f);
            egRect.anchorMax = new Vector2(0.5f, 0.65f);
            egRect.pivot = new Vector2(0f, 0.5f);
            egRect.anchoredPosition = new Vector2(210f, 20f);
            egRect.sizeDelta = new Vector2(100f, 150f);

            GameObject tagTextGo = CreateText("Tag", elemGroup.transform, "속성 전환", defaultFont, 14, FontStyle.Bold, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleCenter);
            RectTransform tagRect = tagTextGo.GetComponent<RectTransform>();
            tagRect.anchoredPosition = new Vector2(0, 55f);
            tagRect.sizeDelta = new Vector2(100f, 22f);

            GameObject fireBtn = CreateButton("Btn_Fire", elemGroup.transform, new Vector2(0, 20f), new Vector2(90f, 32f), "🔥 불", defaultFont, new Color(0.85f, 0.25f, 0.2f), roundedSprite);
            fireBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            fireBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            monsterCtrl.fireButton = fireBtn.GetComponent<Button>();

            GameObject waterBtn = CreateButton("Btn_Water", elemGroup.transform, new Vector2(0, -18f), new Vector2(90f, 32f), "💧 물", defaultFont, new Color(0.2f, 0.5f, 0.9f), roundedSprite);
            waterBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            waterBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            monsterCtrl.waterButton = waterBtn.GetComponent<Button>();

            GameObject grassBtn = CreateButton("Btn_Grass", elemGroup.transform, new Vector2(0, -56f), new Vector2(90f, 32f), "🌿 풀", defaultFont, new Color(0.2f, 0.75f, 0.35f), roundedSprite);
            grassBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            grassBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            monsterCtrl.grassButton = grassBtn.GetComponent<Button>();

            // 9. Hand Panel (Bottom of screen)
            GameObject handGo = CreateUIObject("HandPanel", canvasGo.transform);
            RectTransform handRect = handGo.GetComponent<RectTransform>();
            handRect.anchorMin = new Vector2(0.5f, 0f);
            handRect.anchorMax = new Vector2(0.5f, 0f);
            handRect.pivot = new Vector2(0.5f, 0f);
            handRect.anchoredPosition = new Vector2(0, 30f);
            handRect.sizeDelta = new Vector2(1280f, 280f);

            // Hand Label / Guide
            GameObject guideGo = CreateText("GuideText", handGo.transform, "💡 무속성 카드(포션, 방어, 급소노리기, 도발) 추가 | 상단 '🔄 조합' 카드는 3장 중첩 시 진화", defaultFont, 16, FontStyle.Normal, new Color(0.75f, 0.85f, 1f, 0.9f), TextAnchor.MiddleCenter);
            RectTransform guideRect = guideGo.GetComponent<RectTransform>();
            guideRect.anchoredPosition = new Vector2(0, 260f);
            guideRect.sizeDelta = new Vector2(1100f, 24f);

            // 7 Card Slots
            int slotCount = 7;
            CardSlot[] slots = new CardSlot[slotCount];
            float slotWidth = 160f;
            float slotSpacing = 16f;
            float totalWidth = slotCount * slotWidth + (slotCount - 1) * slotSpacing;
            float startX = -totalWidth * 0.5f + slotWidth * 0.5f;

            for (int i = 0; i < slotCount; i++)
            {
                float posX = startX + i * (slotWidth + slotSpacing);
                GameObject slotGo = CreateCardSlotUI($"CardSlot_{i}", handGo.transform, new Vector2(posX, 125f), new Vector2(slotWidth, 240f), i, defaultFont, roundedSprite, circleSprite);
                slots[i] = slotGo.GetComponent<CardSlot>();
            }

            // 10. Action Buttons (Turn End Button) — moved up to make room for DiscardPile UI
            GameObject turnBtnGo = CreateButton("TurnEndButton", canvasGo.transform, new Vector2(810f, 200f), new Vector2(170f, 60f), "턴 종료 ⏩", defaultFont, new Color(0.85f, 0.25f, 0.2f), roundedSprite);
            battleMgr.turnEndButton = turnBtnGo.GetComponent<Button>();

            // 10a. Draw Pile UI (Bottom Left)
            GameObject drawPileGo = CreateUIObject("DrawPileUI", canvasGo.transform);
            RectTransform drawPileRect = drawPileGo.GetComponent<RectTransform>();
            drawPileRect.anchorMin = new Vector2(0f, 0f);
            drawPileRect.anchorMax = new Vector2(0f, 0f);
            drawPileRect.pivot = new Vector2(0.5f, 0f);
            drawPileRect.anchoredPosition = new Vector2(100f, 20f);
            drawPileRect.sizeDelta = new Vector2(140f, 100f);

            Image drawPileBg = drawPileGo.AddComponent<Image>();
            drawPileBg.sprite = roundedSprite;
            drawPileBg.type = Image.Type.Sliced;
            drawPileBg.color = new Color(0.10f, 0.14f, 0.22f, 0.9f);
            drawPileBg.raycastTarget = false;

            GameObject drawLabelGo = CreateText("Label", drawPileGo.transform, "🂠 남은 덱", defaultFont, 14, FontStyle.Bold, new Color(0.6f, 0.85f, 1f), TextAnchor.MiddleCenter);
            drawLabelGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 28f);
            drawLabelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 24f);

            GameObject drawCountGo = CreateText("CountText", drawPileGo.transform, "20", defaultFont, 36, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            drawCountGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -10f);
            drawCountGo.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 50f);
            battleMgr.drawPileText = drawCountGo.GetComponent<Text>();

            // 10b. Discard Pile UI (Bottom Right, below TurnEndButton)
            GameObject discardPileGo = CreateUIObject("DiscardPileUI", canvasGo.transform);
            RectTransform discardPileRect = discardPileGo.GetComponent<RectTransform>();
            discardPileRect.anchorMin = new Vector2(1f, 0f);
            discardPileRect.anchorMax = new Vector2(1f, 0f);
            discardPileRect.pivot = new Vector2(0.5f, 0f);
            discardPileRect.anchoredPosition = new Vector2(-100f, 20f);
            discardPileRect.sizeDelta = new Vector2(140f, 100f);

            Image discardPileBg = discardPileGo.AddComponent<Image>();
            discardPileBg.sprite = roundedSprite;
            discardPileBg.type = Image.Type.Sliced;
            discardPileBg.color = new Color(0.18f, 0.10f, 0.10f, 0.9f);
            discardPileBg.raycastTarget = false;

            GameObject discardLabelGo = CreateText("Label", discardPileGo.transform, "🗑 버린 카드", defaultFont, 14, FontStyle.Bold, new Color(1f, 0.65f, 0.55f), TextAnchor.MiddleCenter);
            discardLabelGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 28f);
            discardLabelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 24f);

            GameObject discardCountGo = CreateText("CountText", discardPileGo.transform, "0", defaultFont, 36, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            discardCountGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -10f);
            discardCountGo.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 50f);
            battleMgr.discardPileText = discardCountGo.GetComponent<Text>();

            // 11. Player Drop Zone (Bottom Center — "나에게 사용" 영역)
            GameObject playerDropZoneGo = CreateUIObject("PlayerDropZone", canvasGo.transform);
            RectTransform pdzRect = playerDropZoneGo.GetComponent<RectTransform>();
            pdzRect.anchorMin = new Vector2(0.5f, 0f);
            pdzRect.anchorMax = new Vector2(0.5f, 0f);
            pdzRect.pivot = new Vector2(0.5f, 0f);
            pdzRect.anchoredPosition = new Vector2(0f, 20f);
            pdzRect.sizeDelta = new Vector2(340f, 100f);

            Image pdzBg = playerDropZoneGo.AddComponent<Image>();
            pdzBg.sprite = roundedSprite;
            pdzBg.type = Image.Type.Sliced;
            pdzBg.color = new Color(0.15f, 0.22f, 0.35f, 0.55f);
            pdzBg.raycastTarget = true;

            playerDropZoneGo.AddComponent<PlayerDropZone>();

            // 드롭존 텍스트 안내
            GameObject pdzTextGo = CreateText("Label", playerDropZoneGo.transform, "🙋 나에게 사용", defaultFont, 17, FontStyle.Bold, new Color(0.55f, 0.85f, 1f, 0.9f), TextAnchor.MiddleCenter);
            RectTransform pdzTextRect = pdzTextGo.GetComponent<RectTransform>();
            pdzTextRect.anchoredPosition = new Vector2(0f, 18f);
            pdzTextRect.sizeDelta = new Vector2(330f, 28f);

            GameObject pdzSubTextGo = CreateText("SubLabel", playerDropZoneGo.transform, "공격 카드 = 자해 | 포션/방어 = 회복/방어도", defaultFont, 12, FontStyle.Normal, new Color(0.75f, 0.85f, 1f, 0.7f), TextAnchor.MiddleCenter);
            RectTransform pdzSubRect = pdzSubTextGo.GetComponent<RectTransform>();
            pdzSubRect.anchoredPosition = new Vector2(0f, -12f);
            pdzSubRect.sizeDelta = new Vector2(330f, 22f);

            // 12. Floating Text Container
            GameObject floatGo = CreateUIObject("FloatingTextContainer", canvasGo.transform);
            RectTransform floatRect = floatGo.GetComponent<RectTransform>();
            floatRect.anchorMin = Vector2.zero;
            floatRect.anchorMax = Vector2.one;
            floatRect.sizeDelta = Vector2.zero;
            battleMgr.floatingTextContainer = floatGo.transform;

            // Wire references
            battleMgr.Player = playerCtrl;
            battleMgr.Monster = monsterCtrl;
            battleMgr.cardSlots = slots;

            // Save scene
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("DeckGame Battle Scene updated and built successfully!");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateText(string name, Transform parent, string content, Font font, int size, FontStyle style, Color color, TextAnchor anchor)
        {
            GameObject go = CreateUIObject(name, parent);
            Text t = go.AddComponent<Text>();
            t.text = content;
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            return go;
        }

        private static Slider CreateHpBar(string name, Transform parent, Vector2 pos, Vector2 size, Color fillColor, Sprite roundedSprite)
        {
            GameObject barGo = CreateUIObject(name, parent);
            RectTransform rect = barGo.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Slider slider = barGo.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // Background
            Image bg = barGo.AddComponent<Image>();
            bg.sprite = roundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            bg.raycastTarget = false;

            // Fill Area
            GameObject fillArea = CreateUIObject("Fill Area", barGo.transform);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(2f, 2f);
            fillAreaRect.offsetMax = new Vector2(-2f, -2f);

            // Fill
            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.sprite = roundedSprite;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = fillColor;
            fillImg.raycastTarget = false;

            slider.fillRect = fillRect;
            return slider;
        }

        private static GameObject CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Font font, Color btnColor, Sprite roundedSprite)
        {
            GameObject btnGo = CreateUIObject(name, parent);
            RectTransform rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnGo.AddComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = btnColor;

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = btnColor * 1.15f;
            cb.pressedColor = btnColor * 0.85f;
            btn.colors = cb;

            GameObject tGo = CreateText("Text", btnGo.transform, text, font, 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform tRect = tGo.GetComponent<RectTransform>();
            tRect.sizeDelta = size;

            return btnGo;
        }

        private static GameObject CreateCardSlotUI(string name, Transform parent, Vector2 pos, Vector2 size, int index, Font font, Sprite roundedSprite, Sprite circleSprite)
        {
            GameObject slotGo = CreateUIObject(name, parent);
            RectTransform slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.anchoredPosition = pos;
            slotRect.sizeDelta = size;

            CardSlot slot = slotGo.AddComponent<CardSlot>();
            slot.slotIndex = index;

            // Empty Slot Border (Drop target for empty slots)
            GameObject emptyBorderGo = CreateUIObject("EmptySlotBorder", slotGo.transform);
            RectTransform ebRect = emptyBorderGo.GetComponent<RectTransform>();
            ebRect.sizeDelta = size;
            Image ebImg = emptyBorderGo.AddComponent<Image>();
            ebImg.sprite = roundedSprite;
            ebImg.type = Image.Type.Sliced;
            ebImg.color = new Color(0.18f, 0.22f, 0.32f, 0.45f);
            ebImg.raycastTarget = true;
            slot.emptySlotBorder = ebImg;

            // Drop Highlight
            GameObject hlGo = CreateUIObject("DropHighlight", slotGo.transform);
            RectTransform hlRect = hlGo.GetComponent<RectTransform>();
            hlRect.sizeDelta = size + new Vector2(10f, 10f);
            Image hlImg = hlGo.AddComponent<Image>();
            hlImg.sprite = roundedSprite;
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            hlImg.raycastTarget = false;
            hlGo.SetActive(false);
            slot.dropHighlight = hlImg;

            // Card Visual Root (Main drag & click recipient)
            GameObject visualGo = CreateUIObject("CardVisualRoot", slotGo.transform);
            RectTransform visualRect = visualGo.GetComponent<RectTransform>();
            visualRect.sizeDelta = size;
            visualGo.AddComponent<CardDragHandler>();
            visualGo.AddComponent<CanvasGroup>();
            slot.cardVisualRoot = visualGo;

            // Card Border (The active raycast target for the card!)
            Image borderImg = visualGo.AddComponent<Image>();
            borderImg.sprite = roundedSprite;
            borderImg.type = Image.Type.Sliced;
            borderImg.color = Color.white;
            borderImg.raycastTarget = true;
            slot.cardBorder = borderImg;

            // Card Inner Background (raycast disabled so borderImg/CardVisualRoot gets the event)
            GameObject innerBgGo = CreateUIObject("CardBackground", visualGo.transform);
            RectTransform innerRect = innerBgGo.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(4f, 4f);
            innerRect.offsetMax = new Vector2(-4f, -4f);
            Image innerImg = innerBgGo.AddComponent<Image>();
            innerImg.sprite = roundedSprite;
            innerImg.type = Image.Type.Sliced;
            innerImg.color = new Color(0.12f, 0.16f, 0.24f, 1f);
            innerImg.raycastTarget = false;
            slot.cardBackground = innerImg;

            // Energy Cost Badge (Top Left, raycast disabled)
            GameObject costBadgeGo = CreateUIObject("CostBadge", visualGo.transform);
            RectTransform costRect = costBadgeGo.GetComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0f, 1f);
            costRect.anchorMax = new Vector2(0f, 1f);
            costRect.pivot = new Vector2(0.5f, 0.5f);
            costRect.anchoredPosition = new Vector2(18f, -18f);
            costRect.sizeDelta = new Vector2(36f, 36f);
            Image costBadgeImg = costBadgeGo.AddComponent<Image>();
            costBadgeImg.sprite = circleSprite;
            costBadgeImg.color = new Color(0.1f, 0.5f, 0.9f, 1f);
            costBadgeImg.raycastTarget = false;

            GameObject costTextGo = CreateText("CostText", costBadgeGo.transform, "1", font, 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            costTextGo.GetComponent<RectTransform>().sizeDelta = new Vector2(36f, 36f);
            slot.costText = costTextGo.GetComponent<Text>();

            // Level Text (Top Right)
            GameObject lvGo = CreateText("LevelText", visualGo.transform, "★ Lv.1", font, 16, FontStyle.Bold, Color.yellow, TextAnchor.MiddleRight);
            RectTransform lvRect = lvGo.GetComponent<RectTransform>();
            lvRect.anchorMin = new Vector2(1f, 1f);
            lvRect.anchorMax = new Vector2(1f, 1f);
            lvRect.pivot = new Vector2(1f, 1f);
            lvRect.anchoredPosition = new Vector2(-12f, -10f);
            lvRect.sizeDelta = new Vector2(90f, 26f);
            slot.levelText = lvGo.GetComponent<Text>();

            // Mergeable Icon Badge (Top Center: "합칠 수 있는 카드는 카드 윗 부분에 아이콘으로 작게 표시")
            GameObject mergeBadgeGo = CreateUIObject("MergeBadge", visualGo.transform);
            RectTransform mbRect = mergeBadgeGo.GetComponent<RectTransform>();
            mbRect.anchorMin = new Vector2(0.5f, 1f);
            mbRect.anchorMax = new Vector2(0.5f, 1f);
            mbRect.pivot = new Vector2(0.5f, 1f);
            mbRect.anchoredPosition = new Vector2(0f, -8f);
            mbRect.sizeDelta = new Vector2(46f, 18f);

            Image mbImg = mergeBadgeGo.AddComponent<Image>();
            mbImg.sprite = roundedSprite;
            mbImg.type = Image.Type.Sliced;
            mbImg.color = new Color(0.1f, 0.14f, 0.22f, 0.9f);
            mbImg.raycastTarget = false;

            GameObject mbTextGo = CreateText("Icon", mergeBadgeGo.transform, "🔄 조합", font, 10, FontStyle.Bold, new Color(1f, 0.88f, 0.3f), TextAnchor.MiddleCenter);
            RectTransform mbTextRect = mbTextGo.GetComponent<RectTransform>();
            mbTextRect.sizeDelta = new Vector2(46f, 18f);
            slot.mergeBadge = mergeBadgeGo;

            // Element Icon / Artwork placeholder (Center, raycast disabled)
            GameObject elemGo = CreateUIObject("ElementIcon", visualGo.transform);
            RectTransform elemRect = elemGo.GetComponent<RectTransform>();
            elemRect.anchoredPosition = new Vector2(0, 20f);
            elemRect.sizeDelta = new Vector2(70f, 70f);
            Image elemImg = elemGo.AddComponent<Image>();
            elemImg.sprite = circleSprite;
            elemImg.color = Color.cyan;
            elemImg.raycastTarget = false;
            slot.elementIcon = elemImg;

            // Card Name (Center-Bottom)
            GameObject nameGo = CreateText("CardName", visualGo.transform, "물 화살", font, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchoredPosition = new Vector2(0, -35f);
            nameRect.sizeDelta = new Vector2(150f, 28f);
            slot.nameText = nameGo.GetComponent<Text>();

            // Damage Stat (Bottom Center)
            GameObject dmgGo = CreateText("DamageText", visualGo.transform, "⚔ 10", font, 19, FontStyle.Bold, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleCenter);
            RectTransform dmgRect = dmgGo.GetComponent<RectTransform>();
            dmgRect.anchoredPosition = new Vector2(0, -75f);
            dmgRect.sizeDelta = new Vector2(150f, 28f);
            slot.damageText = dmgGo.GetComponent<Text>();

            // Stack Badge (Bottom Right, raycast disabled)
            GameObject stackBadgeGo = CreateUIObject("StackBadge", visualGo.transform);
            RectTransform stackBadgeRect = stackBadgeGo.GetComponent<RectTransform>();
            stackBadgeRect.anchorMin = new Vector2(1f, 0f);
            stackBadgeRect.anchorMax = new Vector2(1f, 0f);
            stackBadgeRect.pivot = new Vector2(0.5f, 0.5f);
            stackBadgeRect.anchoredPosition = new Vector2(-18f, 18f);
            stackBadgeRect.sizeDelta = new Vector2(36f, 36f);
            Image stackBadgeImg = stackBadgeGo.AddComponent<Image>();
            stackBadgeImg.sprite = circleSprite;
            stackBadgeImg.color = new Color(0.9f, 0.7f, 0.1f, 1f);
            stackBadgeImg.raycastTarget = false;
            slot.stackBadge = stackBadgeGo;

            GameObject stackTextGo = CreateText("StackText", stackBadgeGo.transform, "x1", font, 16, FontStyle.Bold, Color.black, TextAnchor.MiddleCenter);
            stackTextGo.GetComponent<RectTransform>().sizeDelta = new Vector2(36f, 36f);
            slot.stackText = stackTextGo.GetComponent<Text>();

            return slotGo;
        }
    }
}
