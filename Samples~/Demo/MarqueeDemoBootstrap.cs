using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MarqueeComponent = global::MarqueeText.MarqueeText;

namespace MarqueeText.Samples
{
    /// <summary>Builds the self-contained sample UI when the demo scene starts.</summary>
    [DisallowMultipleComponent]
    public sealed class MarqueeDemoBootstrap : MonoBehaviour
    {
        private static readonly Color Background = new Color32(18, 22, 31, 255);
        private static readonly Color Panel = new Color32(35, 42, 56, 255);
        private static readonly Color Accent = new Color32(103, 232, 192, 255);

        private void Start()
        {
            Canvas canvas = CreateCanvas();
            CreateLabel(canvas.transform, "MarqueeText", 34f, 235f, Accent);
            CreateLabel(
                canvas.transform,
                "Add MarqueeText to a TextMeshProUGUI component. It moves only when the text overflows.",
                18f,
                190f,
                Color.white);

            CreateRow(
                canvas.transform,
                90f,
                "Overflowing:  This long message scrolls continuously while preserving a stable, allocation-free steady state.",
                70f);
            CreateRow(canvas.transform, 5f, "Fits: This short label stays still.", 70f);

            CreateLabel(
                canvas.transform,
                "Select a marquee and try Direction, Mode, Speed, Delay, Gap, and End Pause in the Inspector.",
                16f,
                -95f,
                new Color(0.75f, 0.8f, 0.9f));
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject(
                "Demo Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(Image));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = canvasObject.GetComponent<Image>();
            background.color = Background;
            background.raycastTarget = false;
            return canvas;
        }

        private static void CreateRow(Transform parent, float y, string value, float speed)
        {
            var panelObject = new GameObject("Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            var panelRect = (RectTransform)panelObject.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 58f);
            panelRect.anchoredPosition = new Vector2(0f, y);
            Image panel = panelObject.GetComponent<Image>();
            panel.color = Panel;
            panel.raycastTarget = false;

            var textObject = new GameObject(
                "Text (TMP) + MarqueeText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(700f, 42f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = 23f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = Color.white;
            text.raycastTarget = false;

            MarqueeComponent marquee = textObject.AddComponent<MarqueeComponent>();
            marquee.Speed = speed;
        }

        private static void CreateLabel(Transform parent, string value, float size, float y, Color color)
        {
            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(820f, 60f);
            rect.anchoredPosition = new Vector2(0f, y);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
        }
    }
}
