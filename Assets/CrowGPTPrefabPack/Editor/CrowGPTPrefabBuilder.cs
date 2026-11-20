using System.IO;
using CrowGPT;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrowGPTEditor
{
    public static class CrowGPTPrefabBuilder
    {
        private const string Root = "Assets/CrowGPTPrefabPack";
        private const string Generated = Root + "/Generated";
        private static readonly Color Ink = Hex("#202521");
        private static readonly Color Cream = Hex("#FFFDF7");
        private static readonly Color Green = Hex("#E7EDDF");
        private static readonly Color Muted = Hex("#72776F");

        [MenuItem("CrowGPT/Build Complete UI Prefab")]
        public static void Build()
        {
            ConfigureSprites();
            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh();

            TMP_FontAsset hand = EnsureFontAsset("PatrickHand-Regular", "PatrickHand SDF");
            TMP_FontAsset sans = EnsureFontAsset("DMSans", "DMSans SDF");
            if (hand == null || sans == null)
            {
                EditorUtility.DisplayDialog(
                    "CrowGPT",
                    "The included Patrick Hand and DM Sans font files were not found. " +
                    "Re-copy the complete CrowGPTPrefabPack folder and try again.",
                    "OK"
                );
                return;
            }

            Sprite background = Sprite("background_390x844");
            Sprite crowSprite = Sprite("crow");
            Sprite speechPanel = Sprite("panel_speech");
            Sprite speechTail = Sprite("speech_tail");
            Sprite inputPanel = Sprite("panel_input");
            Sprite startersPanel = Sprite("panel_starters");
            Sprite outlineButton = Sprite("button_outline");
            Sprite sendButton = Sprite("button_send");
            Sprite sendDisabled = Sprite("button_send_disabled");
            Sprite badge = Sprite("badge_question");
            Sprite crowBadge = Sprite("badge_crow");
            Sprite menuIcon = Sprite("icon_menu");
            Sprite closeIcon = Sprite("icon_close");
            Sprite plusIcon = Sprite("icon_plus");
            Sprite arrowIcon = Sprite("icon_arrow");
            Sprite arrowDisabled = Sprite("icon_arrow_disabled");
            Sprite drawerButton = Sprite("panel_drawer_button");
            Sprite phoneFrame = Sprite("phone_frame_470x944");

            GameObject canvasObject = new GameObject(
                "CrowGPTCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image backgroundImage = Image(
                "Background",
                canvasObject.transform,
                background,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
            backgroundImage.raycastTarget = false;

            GameObject safeArea = Rect(
                "SafeArea",
                canvasObject.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
            safeArea.AddComponent<SafeAreaFitter>();

            CrowGPTController controller = safeArea.AddComponent<CrowGPTController>();

            BuildHeader(
                safeArea.transform,
                sans,
                outlineButton,
                menuIcon,
                plusIcon,
                controller
            );

            GameObject hero = Rect(
                "Hero",
                safeArea.transform,
                new Vector2(0, 1),
                Vector2.one,
                new Vector2(0.5f, 1),
                new Vector2(0, -64),
                new Vector2(0, 520)
            );

            Image crow = Image(
                "Crow",
                hero.transform,
                crowSprite,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(39, 52),
                new Vector2(304, 238)
            );
            crow.preserveAspect = true;
            crow.raycastTarget = false;

            TMP_Text speechText;
            GameObject smile;
            BuildSpeech(
                hero.transform,
                hand,
                speechPanel,
                speechTail,
                badge,
                out speechText,
                out smile
            );

            TMP_Text selectedLabel;
            Button replay;
            Image replayBackground;
            Image replayImage;
            Button[] questionButtons = BuildQuestionControls(
                safeArea.transform,
                hand,
                sans,
                inputPanel,
                startersPanel,
                sendDisabled,
                arrowDisabled,
                badge,
                controller,
                out selectedLabel,
                out replay,
                out replayBackground,
                out replayImage
            );

            GameObject scrim;
            GameObject drawer;
            BuildDrawer(
                safeArea.transform,
                hand,
                sans,
                crowSprite,
                crowBadge,
                closeIcon,
                plusIcon,
                drawerButton,
                controller,
                out scrim,
                out drawer
            );

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("speechText").objectReferenceValue = speechText;
            serialized.FindProperty("selectedQuestionLabel").objectReferenceValue = selectedLabel;
            serialized.FindProperty("smile").objectReferenceValue = smile;
            SerializedProperty buttons = serialized.FindProperty("questionButtons");
            buttons.arraySize = questionButtons.Length;
            for (int i = 0; i < questionButtons.Length; i++)
                buttons.GetArrayElementAtIndex(i).objectReferenceValue = questionButtons[i];
            serialized.FindProperty("replayButton").objectReferenceValue = replay;
            serialized.FindProperty("replayBackground").objectReferenceValue = replayBackground;
            serialized.FindProperty("replayIcon").objectReferenceValue = replayImage;
            serialized.FindProperty("replayEnabledSprite").objectReferenceValue = sendButton;
            serialized.FindProperty("replayDisabledSprite").objectReferenceValue = sendDisabled;
            serialized.FindProperty("arrowEnabledSprite").objectReferenceValue = arrowIcon;
            serialized.FindProperty("arrowDisabledSprite").objectReferenceValue = arrowDisabled;
            serialized.FindProperty("drawer").objectReferenceValue = drawer;
            serialized.FindProperty("scrim").objectReferenceValue = scrim;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            drawer.SetActive(false);
            scrim.SetActive(false);

            string prefabPath = Generated + "/CrowGPTCanvas.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasObject, prefabPath);
            Object.DestroyImmediate(canvasObject);
            BuildPhonePreview(prefabPath, phoneFrame);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            EnsureEventSystem();
            EditorUtility.DisplayDialog(
                "CrowGPT",
                "Created both prefabs in the Generated folder:\n\n" +
                "• CrowGPTCanvas.prefab — full-screen device UI\n" +
                "• CrowGPTPhonePreview.prefab — framed interactive mockup\n\n" +
                "Drag either prefab into a scene, then enter Play Mode.",
                "Done"
            );
        }

        private static void BuildPhonePreview(string appPrefabPath, Sprite phoneFrame)
        {
            GameObject preview = new GameObject(
                "CrowGPTPhonePreview",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            Canvas canvas = preview.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = preview.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(470, 944);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image backdrop = Image(
                "PreviewBackdrop",
                preview.transform,
                null,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
            backdrop.color = Hex("#F2F2F2");
            backdrop.raycastTarget = false;

            GameObject viewport = Rect(
                "AppViewport",
                preview.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(390, 844)
            );
            viewport.AddComponent<RectMask2D>();

            GameObject appPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(appPrefabPath);
            GameObject app = (GameObject)PrefabUtility.InstantiatePrefab(
                appPrefab,
                viewport.transform
            );
            PrefabUtility.UnpackPrefabInstance(
                app,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction
            );
            Object.DestroyImmediate(app.GetComponent<CanvasScaler>());
            Object.DestroyImmediate(app.GetComponent<GraphicRaycaster>());
            Object.DestroyImmediate(app.GetComponent<Canvas>());
            app.name = "CrowGPTApp";
            RectTransform appRect = app.GetComponent<RectTransform>();
            appRect.anchorMin = Vector2.zero;
            appRect.anchorMax = Vector2.one;
            appRect.pivot = new Vector2(0.5f, 0.5f);
            appRect.anchoredPosition = Vector2.zero;
            appRect.sizeDelta = Vector2.zero;

            SafeAreaFitter fitter = app.GetComponentInChildren<SafeAreaFitter>();
            if (fitter != null)
            {
                RectTransform safeRect = fitter.GetComponent<RectTransform>();
                Object.DestroyImmediate(fitter);
                safeRect.anchorMin = Vector2.zero;
                safeRect.anchorMax = Vector2.one;
                safeRect.offsetMin = Vector2.zero;
                safeRect.offsetMax = Vector2.zero;
            }

            Image frame = Image(
                "PhoneFrame",
                preview.transform,
                phoneFrame,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(470, 944)
            );
            frame.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(
                preview,
                Generated + "/CrowGPTPhonePreview.prefab"
            );
            Object.DestroyImmediate(preview);
        }

        private static void BuildHeader(
            Transform parent,
            TMP_FontAsset sans,
            Sprite outline,
            Sprite menu,
            Sprite plus,
            CrowGPTController controller
        )
        {
            GameObject header = Rect(
                "Header",
                parent,
                new Vector2(0, 1),
                Vector2.one,
                new Vector2(0.5f, 1),
                Vector2.zero,
                new Vector2(0, 64)
            );
            Button menuButton = IconButton(
                "MenuButton",
                header.transform,
                outline,
                menu,
                new Vector2(0, 1),
                new Vector2(0, 1),
                new Vector2(0.5f, 0.5f),
                new Vector2(36, -32),
                new Vector2(40, 40),
                22
            );
            UnityEventTools.AddPersistentListener(menuButton.onClick, controller.OpenMenu);

            Text(
                "BrandLabel",
                header.transform,
                "CROWGPT",
                sans,
                14,
                Ink,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1),
                new Vector2(0.5f, 1),
                new Vector2(0.5f, 0.5f),
                new Vector2(0, -32),
                new Vector2(140, 32),
                FontStyles.Bold
            ).characterSpacing = 8;

            Button reset = IconButton(
                "ResetButton",
                header.transform,
                null,
                plus,
                Vector2.one,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(-36, -32),
                new Vector2(40, 40),
                22
            );
            UnityEventTools.AddPersistentListener(reset.onClick, controller.BeginAgain);
        }

        private static void BuildSpeech(
            Transform parent,
            TMP_FontAsset hand,
            Sprite panelSprite,
            Sprite tailSprite,
            Sprite badgeSprite,
            out TMP_Text speechText,
            out GameObject smile
        )
        {
            GameObject group = Rect(
                "SpeechGroup",
                parent,
                Vector2.one,
                Vector2.one,
                Vector2.one,
                new Vector2(-27, -36),
                new Vector2(226, 150)
            );
            group.transform.localRotation = Quaternion.Euler(0, 0, -1.2f);

            Image tail = Image(
                "SpeechTail",
                group.transform,
                tailSprite,
                new Vector2(0.5f, 0),
                new Vector2(0.5f, 0),
                new Vector2(0.5f, 0),
                new Vector2(20, -23),
                new Vector2(42, 27)
            );
            tail.preserveAspect = true;
            Shadow tailShadow = tail.gameObject.AddComponent<Shadow>();
            tailShadow.effectColor = Ink;
            tailShadow.effectDistance = new Vector2(5, -6);

            Image panel = Image(
                "SpeechPanel",
                group.transform,
                panelSprite,
                new Vector2(0.5f, 1),
                new Vector2(0.5f, 1),
                new Vector2(0.5f, 1),
                Vector2.zero,
                new Vector2(226, 124)
            );
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            Shadow panelShadow = panel.gameObject.AddComponent<Shadow>();
            panelShadow.effectColor = Ink;
            panelShadow.effectDistance = new Vector2(5, -6);

            speechText = Text(
                "SpeechText",
                panel.transform,
                "Hey there! My name is <color=#C95333>CrowGPT.</color>\nAsk me anything!",
                hand,
                24,
                Ink,
                TextAlignmentOptions.TopLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
            RectTransform textRect = speechText.rectTransform;
            textRect.offsetMin = new Vector2(18, 15);
            textRect.offsetMax = new Vector2(-18, -14);
            speechText.textWrappingMode = TextWrappingModes.Normal;
            speechText.richText = true;
            speechText.raycastTarget = false;

            Image smileImage = Image(
                "Smile",
                panel.transform,
                badgeSprite,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(180, 15),
                new Vector2(28, 28)
            );
            smile = smileImage.gameObject;
            smile.transform.localRotation = Quaternion.Euler(0, 0, -7);
            Text(
                "Face",
                smile.transform,
                "•ᴗ•",
                hand,
                15,
                Ink,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
        }

        private static Button[] BuildQuestionControls(
            Transform parent,
            TMP_FontAsset hand,
            TMP_FontAsset sans,
            Sprite inputPanel,
            Sprite starterPanel,
            Sprite sendDisabled,
            Sprite arrowDisabled,
            Sprite badge,
            CrowGPTController controller,
            out TMP_Text selectedLabel,
            out Button replay,
            out Image replayBackground,
            out Image replayIcon
        )
        {
            GameObject controls = Rect(
                "QuestionControls",
                parent,
                new Vector2(0.5f, 0),
                new Vector2(0.5f, 0),
                new Vector2(0.5f, 0),
                new Vector2(0, 38),
                new Vector2(356, 209)
            );

            Image selected = Image(
                "SelectedQuestion",
                controls.transform,
                inputPanel,
                new Vector2(0.5f, 1),
                new Vector2(0.5f, 1),
                new Vector2(0.5f, 1),
                Vector2.zero,
                new Vector2(356, 56)
            );
            selected.type = UnityEngine.UI.Image.Type.Sliced;
            Shadow selectedShadow = selected.gameObject.AddComponent<Shadow>();
            selectedShadow.effectColor = Ink;
            selectedShadow.effectDistance = new Vector2(4, -5);

            selectedLabel = Text(
                "SelectedQuestionLabel",
                selected.transform,
                "Ask CrowGPT",
                hand,
                24,
                Hex("#676D67"),
                TextAlignmentOptions.MidlineLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            );
            selectedLabel.rectTransform.offsetMin = new Vector2(20, 6);
            selectedLabel.rectTransform.offsetMax = new Vector2(-58, -6);

            replay = IconButton(
                "ReplayButton",
                selected.transform,
                sendDisabled,
                arrowDisabled,
                new Vector2(1, 0.5f),
                new Vector2(1, 0.5f),
                new Vector2(1, 0.5f),
                new Vector2(-27, 0),
                new Vector2(42, 42),
                20
            );
            replayBackground = replay.GetComponent<Image>();
            replayIcon = replay.transform.GetChild(0).GetComponent<Image>();
            UnityEventTools.AddPersistentListener(replay.onClick, controller.ReplaySelected);

            Image card = Image(
                "QuestionCard",
                controls.transform,
                starterPanel,
                new Vector2(0.5f, 1),
                new Vector2(0.5f, 1),
                new Vector2(0.5f, 1),
                new Vector2(0, -72),
                new Vector2(356, 137)
            );
            card.type = UnityEngine.UI.Image.Type.Sliced;

            string[] prompts = { "Who are you?", "What is an LLM?", "Why are you a crow?" };
            Button[] result = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject row = Rect(
                    "Question" + (i + 1),
                    card.transform,
                    new Vector2(0.5f, 1),
                    new Vector2(0.5f, 1),
                    new Vector2(0.5f, 1),
                    new Vector2(0, -10 - i * 39),
                    new Vector2(336, 39)
                );
                Image target = row.AddComponent<Image>();
                target.color = new Color(1, 1, 1, 0);
                Button button = row.AddComponent<Button>();
                button.targetGraphic = target;
                ColorBlock colors = button.colors;
                colors.highlightedColor = new Color(1, 1, 1, 0.72f);
                colors.pressedColor = new Color(1, 1, 1, 0.9f);
                colors.disabledColor = new Color(1, 1, 1, 0.55f);
                button.colors = colors;
                result[i] = button;

                Text(
                    "Label",
                    row.transform,
                    prompts[i],
                    hand,
                    23,
                    Ink,
                    TextAlignmentOptions.MidlineLeft,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero
                ).rectTransform.offsetMin = new Vector2(9, 0);

                Image numberBadge = Image(
                    "NumberBadge",
                    row.transform,
                    badge,
                    new Vector2(1, 0.5f),
                    new Vector2(1, 0.5f),
                    new Vector2(1, 0.5f),
                    new Vector2(-22, 0),
                    new Vector2(26, 26)
                );
                Text(
                    "Number",
                    numberBadge.transform,
                    (i + 1).ToString(),
                    sans,
                    11,
                    Ink,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    Vector2.zero,
                    FontStyles.Normal
                );
            }
            UnityEventTools.AddPersistentListener(result[0].onClick, controller.SelectQuestion1);
            UnityEventTools.AddPersistentListener(result[1].onClick, controller.SelectQuestion2);
            UnityEventTools.AddPersistentListener(result[2].onClick, controller.SelectQuestion3);
            return result;
        }

        private static void BuildDrawer(
            Transform parent,
            TMP_FontAsset hand,
            TMP_FontAsset sans,
            Sprite crow,
            Sprite badge,
            Sprite close,
            Sprite plus,
            Sprite darkButton,
            CrowGPTController controller,
            out GameObject scrim,
            out GameObject drawer
        )
        {
            Button scrimButton = Rect(
                "Scrim",
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero
            ).AddComponent<Button>();
            scrim = scrimButton.gameObject;
            Image scrimImage = scrim.AddComponent<Image>();
            scrimImage.color = new Color(20 / 255f, 26 / 255f, 22 / 255f, 0.38f);
            scrimButton.targetGraphic = scrimImage;
            UnityEventTools.AddPersistentListener(scrimButton.onClick, controller.CloseMenu);

            drawer = Rect(
                "Drawer",
                parent,
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(0, 0.5f),
                Vector2.zero,
                new Vector2(328, 0)
            );
            Image drawerBackground = drawer.AddComponent<Image>();
            drawerBackground.color = Hex("#F7F3E9");
            Shadow drawerShadow = drawer.AddComponent<Shadow>();
            drawerShadow.effectColor = new Color(21 / 255f, 27 / 255f, 23 / 255f, 0.2f);
            drawerShadow.effectDistance = new Vector2(14, 0);

            Image brandBadge = Image(
                "CrowBadge",
                drawer.transform,
                badge,
                new Vector2(0, 1),
                new Vector2(0, 1),
                new Vector2(0, 1),
                new Vector2(16, -16),
                new Vector2(32, 32)
            );
            Image brandCrow = Image(
                "Crow",
                brandBadge.transform,
                crow,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(27, 21)
            );
            brandCrow.preserveAspect = true;
            Text(
                "BrandLabel",
                drawer.transform,
                "CrowGPT",
                sans,
                16,
                Ink,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1),
                new Vector2(0, 1),
                new Vector2(0, 1),
                new Vector2(57, -16),
                new Vector2(140, 32),
                FontStyles.Bold
            );
            Button closeButton = IconButton(
                "CloseButton",
                drawer.transform,
                null,
                close,
                Vector2.one,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(-36, -20),
                new Vector2(40, 40),
                22
            );
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.CloseMenu);

            Image newBackground = Image(
                "NewConversation",
                drawer.transform,
                darkButton,
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(0.5f, 1),
                new Vector2(0, -84),
                new Vector2(-32, 46)
            );
            newBackground.type = UnityEngine.UI.Image.Type.Sliced;
            Button newButton = newBackground.gameObject.AddComponent<Button>();
            newButton.targetGraphic = newBackground;
            Image newIcon = Image(
                "PlusIcon",
                newBackground.transform,
                plus,
                new Vector2(0, 0.5f),
                new Vector2(0, 0.5f),
                new Vector2(0, 0.5f),
                new Vector2(14, 0),
                new Vector2(18, 18)
            );
            newIcon.color = Cream;
            Text(
                "Label",
                newBackground.transform,
                "New conversation",
                sans,
                13,
                Cream,
                TextAlignmentOptions.MidlineLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                FontStyles.Bold
            ).rectTransform.offsetMin = new Vector2(43, 0);
            UnityEventTools.AddPersistentListener(newButton.onClick, controller.BeginAgain);

            Text(
                "Tagline",
                drawer.transform,
                "Built to be curious",
                hand,
                22,
                Ink,
                TextAlignmentOptions.BottomLeft,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(21, 69),
                new Vector2(280, 32)
            );
            Text(
                "Description",
                drawer.transform,
                "A friendly place for questions, ideas, and everyday discoveries.",
                sans,
                12,
                Muted,
                TextAlignmentOptions.TopLeft,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(21, 20),
                new Vector2(280, 46)
            );
        }

        private static Button IconButton(
            string name,
            Transform parent,
            Sprite background,
            Sprite icon,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            float iconSize
        )
        {
            Image image = Image(
                name,
                parent,
                background,
                anchorMin,
                anchorMax,
                pivot,
                position,
                size
            );
            if (background == null)
                image.color = new Color(1, 1, 1, 0);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Image iconImage = Image(
                "Icon",
                image.transform,
                icon,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(iconSize, iconSize)
            );
            iconImage.raycastTarget = false;
            return button;
        }

        private static Image Image(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size
        )
        {
            GameObject gameObject = Rect(
                name,
                parent,
                anchorMin,
                anchorMax,
                pivot,
                position,
                size
            );
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            return image;
        }

        private static TMP_Text Text(
            string name,
            Transform parent,
            string content,
            TMP_FontAsset font,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 dimensions,
            FontStyles style = FontStyles.Normal
        )
        {
            GameObject gameObject = Rect(
                name,
                parent,
                anchorMin,
                anchorMax,
                pivot,
                position,
                dimensions
            );
            TMP_Text text = gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static GameObject Rect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size
        )
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return gameObject;
        }

        private static void ConfigureSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Sprites" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                string file = Path.GetFileNameWithoutExtension(path);
                if (file == "panel_speech")
                    importer.spriteBorder = new Vector4(64, 64, 64, 64);
                else if (file == "panel_input")
                    importer.spriteBorder = new Vector4(56, 56, 56, 56);
                else if (file == "panel_starters")
                    importer.spriteBorder = new Vector4(48, 48, 48, 48);
                else if (file == "panel_drawer_button")
                    importer.spriteBorder = new Vector4(32, 32, 32, 32);
                importer.SaveAndReimport();
            }
        }

        private static TMP_FontAsset EnsureFontAsset(string fontSearch, string outputName)
        {
            string outputPath = Generated + "/" + outputName + ".asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
            if (existing != null)
                return existing;
            string[] guids = AssetDatabase.FindAssets(fontSearch + " t:Font", new[] { Root + "/Fonts" });
            if (guids.Length == 0)
                return null;
            Font source = AssetDatabase.LoadAssetAtPath<Font>(
                AssetDatabase.GUIDToAssetPath(guids[0])
            );
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source);
            asset.name = outputName;
            AssetDatabase.CreateAsset(asset, outputPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static Sprite Sprite(string name)
        {
            string[] guids = AssetDatabase.FindAssets(
                name + " t:Sprite",
                new[] { Root + "/Sprites" }
            );
            return guids.Length == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<Sprite>(
                    AssetDatabase.GUIDToAssetPath(guids[0])
                );
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }
    }
}
