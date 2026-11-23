using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace RoomInteraction
{
    /// <summary>Yields the existing Yarn command until a room object is selected.
    /// Never starts a second conversation or replaces the dialogue presenters.</summary>
    public sealed class RoomExploration : MonoBehaviour
    {
        public DialogueRunner runner;
        public Texture2D roomImage;
        public TMP_FontAsset font;
        public RoomInteractable[] items;
        public string resultVariable = "$room_action";
        public string continueLabel = "Continue story  →";
        public bool Waiting { get; private set; }
        public string Selected { get; private set; }
        public bool InspectionVisible => inspection != null && inspection.activeSelf;
        GameObject exploration, inspection;
        CanvasGroup inputGroup;
        readonly List<GameObject> regions = new List<GameObject>();
        static readonly Color Cream = new Color(1f, .97f, .87f, .92f);
        static readonly Color Ink = new Color(.25f, .22f, .16f);

        void Awake()
        {
            if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();
            if (runner == null) { Debug.LogError("RoomExploration needs a DialogueRunner.", this); enabled = false; return; }
            runner.AddCommandHandler<string>("room_explore", Explore);
            runner.AddCommandHandler<string>("room_show", ShowInspection);
            runner.AddCommandHandler("room_hide", HideInspection);
            BuildUI();
        }

        IEnumerator Explore(string room)
        {
            HideInspection();
            Selected = null;
            for (int i = 0; i < items.Length; i++)
                regions[i].SetActive(items[i] != null && items[i].isActiveAndEnabled && items[i].roomId == room);
            Waiting = true;
            exploration.SetActive(true);
            while (Waiting && runner != null && runner.IsDialogueRunning) yield return null;
            exploration.SetActive(false);
            Waiting = false;
            if (runner != null && runner.IsDialogueRunning)
                runner.VariableStorage.SetValue(resultVariable, Selected ?? "continue");
        }

        public void Select(string id)
        {
            if (!Waiting || Blocked()) return;
            if (id != "continue")
            {
                bool found = false;
                for (int i = 0; i < items.Length; i++)
                    if (items[i] != null && items[i].itemId == id && regions[i].activeSelf) found = true;
                if (!found) return;
            }
            Selected = id;
            Waiting = false;
            exploration.SetActive(false);
        }

        bool Blocked() => Time.timeScale == 0 || (GameSession.Instance != null && (GameSession.Instance.IsPaused || GameSession.Instance.IsBusy));
        void Update()
        {
            if (inputGroup != null) inputGroup.interactable = !Blocked();
            if (runner != null && !runner.IsDialogueRunning && (Waiting || InspectionVisible)) Cancel();
        }

        void BuildUI()
        {
            // Above the normal dialogue canvas only while waiting; menus remain above us.
            exploration = CanvasRoot("Room exploration · original scene", 10);
            inputGroup = exploration.AddComponent<CanvasGroup>();
            var fitted = Rect("Background coordinate space", exploration.transform);
            var fit = fitted.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = roomImage != null ? (float)roomImage.width / roomImage.height : 16f / 9f;
            foreach (var item in items)
            {
                var area = Rect(item != null ? item.displayName : "Missing item", fitted);
                if (item == null) { regions.Add(area.gameObject); continue; }
                area.anchorMin = item.region.min; area.anchorMax = item.region.max;
                var hit = area.gameObject.AddComponent<Image>(); hit.color = new Color(1, .92f, .65f, .015f);
                var button = area.gameObject.AddComponent<Button>();
                var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1, 1, 1, 1); button.colors = colors;
                string id = item.itemId; button.onClick.AddListener(() => Select(id));
                var chip = Rect("Object label", area); chip.anchorMin = chip.anchorMax = new Vector2(.5f, .15f); chip.sizeDelta = new Vector2(150, 34);
                chip.gameObject.AddComponent<Image>().color = Cream;
                Label(chip, "·  " + item.displayName, 18);
                regions.Add(area.gameObject);
            }
            var hint = Rect("Exploration hint", exploration.transform);
            hint.anchorMin = hint.anchorMax = new Vector2(.5f, .94f); hint.sizeDelta = new Vector2(440, 42);
            hint.gameObject.AddComponent<Image>().color = Cream;
            Label(hint, "Look around · Click an object to inspect", 20);
            var next = Rect("Continue story", exploration.transform);
            next.anchorMin = next.anchorMax = new Vector2(.85f, .91f); next.sizeDelta = new Vector2(240, 48);
            next.gameObject.AddComponent<Image>().color = Cream;
            Label(next, continueLabel, 21);
            next.gameObject.AddComponent<Button>().onClick.AddListener(() => Select("continue"));
            exploration.SetActive(false);
            // Inspection stays below the existing dialogue canvas (sorting order 0).
            inspection = CanvasRoot("Inspection · original artwork", -1);
            inspection.SetActive(false);
        }

        public void ShowInspection(string id)
        {
            HideInspection();
            var item = System.Array.Find(items, x => x != null && x.itemId == id);
            if (item == null || item.inspectionImage == null) return;
            foreach (Transform child in inspection.transform) Destroy(child.gameObject);
            var shade = Rect("Soft veil", inspection.transform);
            var veil = shade.gameObject.AddComponent<Image>(); veil.color = new Color(.92f, .88f, .76f, .75f); veil.raycastTarget = false;
            var frame = Rect("Enlarged object", inspection.transform);
            frame.anchorMin = new Vector2(.32f, .29f); frame.anchorMax = new Vector2(.68f, .92f);
            var artwork = Rect("Artwork", frame);
            var raw = artwork.gameObject.AddComponent<RawImage>(); raw.texture = item.inspectionImage; raw.uvRect = item.inspectionCrop; raw.raycastTarget = false;
            var fit = artwork.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = item.inspectionImage.width * item.inspectionCrop.width / (item.inspectionImage.height * item.inspectionCrop.height);
            inspection.SetActive(true);
        }

        public void HideInspection() { if (inspection != null) inspection.SetActive(false); }
        void Cancel() { Waiting = false; if (exploration != null) exploration.SetActive(false); HideInspection(); }
        void OnDisable() => Cancel();
        void OnDestroy()
        {
            if (runner != null) foreach (var command in new[] { "room_explore", "room_show", "room_hide" }) runner.RemoveCommandHandler(command);
        }
        GameObject CanvasRoot(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            return go;
        }
        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        void Label(RectTransform parent, string value, float size)
        {
            var text = Rect("Label", parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value; text.fontSize = size; text.color = Ink; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        }
    }
}
