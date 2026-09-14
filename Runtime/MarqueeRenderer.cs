using TMPro;
using UnityEngine;

namespace MarqueeText
{
    /// <summary>
    /// Owns the small amount of Unity/TMP-specific rendering required by the marquee.
    /// Internal renderers are created only when overflowing text needs them.
    /// </summary>
    internal sealed class MarqueeRenderer
    {
        private const string PrimaryName = "__MarqueeText_Primary";
        private const string GhostName = "__MarqueeText_Ghost";

        private readonly TextMeshProUGUI _source;
        private readonly RectTransform _viewport;

        private TextMeshProUGUI _primary;
        private TextMeshProUGUI _ghost;
        private RectTransform _primaryRect;
        private RectTransform _ghostRect;
        private bool _sourceHidden;
        private bool _sourceCullBeforeHide;

        internal MarqueeRenderer(TextMeshProUGUI source, RectTransform viewport)
        {
            _source = source;
            _viewport = viewport;
        }

        internal RectTransform PrimaryRect => _primaryRect;

        internal void Measure(out float viewportWidth, out float contentWidth)
        {
            viewportWidth = Mathf.Max(0f, _viewport.rect.width);

            // preferredWidth computes the unwrapped width. Unlike the old implementation,
            // this does not also force a mesh build or calculate preferredHeight.
            contentWidth = Mathf.Max(0f, _source.preferredWidth);
        }

        internal void Prepare(bool isOverflowing, MarqueeScrollMode mode, float contentWidth)
        {
            if (!isOverflowing)
            {
                SetSourceHidden(false);
                SetActive(_primary, false);
                SetActive(_ghost, false);
                return;
            }

            EnsurePrimary();
            SynchronizeProperties(_primary);
            SetRendererSize(_primaryRect, contentWidth);
            SetActive(_primary, true);

            if (mode == MarqueeScrollMode.Continuous)
            {
                EnsureGhost();
                SynchronizeProperties(_ghost);
                SetRendererSize(_ghostRect, contentWidth);
                SetActive(_ghost, true);
            }
            else
            {
                SetActive(_ghost, false);
            }

            SetSourceHidden(true);
        }

        internal void Apply(MarqueeEngine engine, MarqueeDirection direction)
        {
            if (_primaryRect == null || !engine.IsOverflowing)
            {
                return;
            }

            float primaryX;
            float ghostX;

            if (direction == MarqueeDirection.RightToLeft)
            {
                primaryX = -engine.Offset;
                ghostX = primaryX + engine.Period;
            }
            else
            {
                float startX = engine.ViewportWidth - engine.ContentWidth;
                primaryX = startX + engine.Offset;
                ghostX = primaryX - engine.Period;
            }

            SetAnchoredX(_primaryRect, primaryX);
            if (_ghost != null && _ghost.gameObject.activeSelf)
            {
                SetAnchoredX(_ghostRect, ghostX);
            }
        }

        internal void Deactivate()
        {
            SetSourceHidden(false);
            SetActive(_primary, false);
            SetActive(_ghost, false);
        }

        internal void Dispose()
        {
            Deactivate();
            DestroyObject(_primary != null ? _primary.gameObject : null);
            DestroyObject(_ghost != null ? _ghost.gameObject : null);
            _primary = null;
            _ghost = null;
            _primaryRect = null;
            _ghostRect = null;
        }

        private void EnsurePrimary()
        {
            if (_primary != null)
            {
                return;
            }

            _primary = CreateRenderer(PrimaryName);
            _primaryRect = _primary.rectTransform;
        }

        private void EnsureGhost()
        {
            if (_ghost != null)
            {
                return;
            }

            _ghost = CreateRenderer(GhostName);
            _ghostRect = _ghost.rectTransform;
        }

        private TextMeshProUGUI CreateRenderer(string objectName)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.SetActive(false);
            child.layer = _source.gameObject.layer;
            child.hideFlags = HideFlags.HideInHierarchy |
                              HideFlags.DontSaveInEditor |
                              HideFlags.DontSaveInBuild |
                              HideFlags.NotEditable;
            child.transform.SetParent(_viewport, false);

            var rect = (RectTransform)child.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;

            var text = child.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.maskable = true;
            return text;
        }

        private void SynchronizeProperties(TextMeshProUGUI target)
        {
            if (target.text != _source.text) target.text = _source.text;
            if (target.font != _source.font) target.font = _source.font;
            if (target.fontSharedMaterial != _source.fontSharedMaterial)
                target.fontSharedMaterial = _source.fontSharedMaterial;
            if (target.fontSize != _source.fontSize) target.fontSize = _source.fontSize;
            if (target.enableAutoSizing != _source.enableAutoSizing)
                target.enableAutoSizing = _source.enableAutoSizing;
            if (target.fontSizeMin != _source.fontSizeMin) target.fontSizeMin = _source.fontSizeMin;
            if (target.fontSizeMax != _source.fontSizeMax) target.fontSizeMax = _source.fontSizeMax;
            if (target.fontStyle != _source.fontStyle) target.fontStyle = _source.fontStyle;
            if (target.fontWeight != _source.fontWeight) target.fontWeight = _source.fontWeight;
            if (target.color != _source.color) target.color = _source.color;
            if (target.enableVertexGradient != _source.enableVertexGradient)
                target.enableVertexGradient = _source.enableVertexGradient;
            target.colorGradient = _source.colorGradient;
            if (target.colorGradientPreset != _source.colorGradientPreset)
                target.colorGradientPreset = _source.colorGradientPreset;
            if (target.characterSpacing != _source.characterSpacing)
                target.characterSpacing = _source.characterSpacing;
            if (target.wordSpacing != _source.wordSpacing) target.wordSpacing = _source.wordSpacing;
            if (target.lineSpacing != _source.lineSpacing) target.lineSpacing = _source.lineSpacing;
            if (target.paragraphSpacing != _source.paragraphSpacing)
                target.paragraphSpacing = _source.paragraphSpacing;
            if (target.richText != _source.richText) target.richText = _source.richText;
            if (target.parseCtrlCharacters != _source.parseCtrlCharacters)
                target.parseCtrlCharacters = _source.parseCtrlCharacters;
            if (target.isRightToLeftText != _source.isRightToLeftText)
                target.isRightToLeftText = _source.isRightToLeftText;
            if (target.alignment != _source.alignment) target.alignment = _source.alignment;
            if (target.extraPadding != _source.extraPadding) target.extraPadding = _source.extraPadding;
            if (target.spriteAsset != _source.spriteAsset) target.spriteAsset = _source.spriteAsset;
            if (target.styleSheet != _source.styleSheet) target.styleSheet = _source.styleSheet;
            if (target.textStyle != _source.textStyle) target.textStyle = _source.textStyle;
            if (target.tintAllSprites != _source.tintAllSprites)
                target.tintAllSprites = _source.tintAllSprites;
            if (target.margin != _source.margin) target.margin = _source.margin;

            if (target.textWrappingMode != TextWrappingModes.NoWrap)
                target.textWrappingMode = TextWrappingModes.NoWrap;
            if (target.overflowMode != TextOverflowModes.Overflow)
                target.overflowMode = TextOverflowModes.Overflow;
        }

        private void SetRendererSize(RectTransform rect, float contentWidth)
        {
            Vector2 size = new Vector2(contentWidth, Mathf.Max(0f, _viewport.rect.height));
            if ((rect.sizeDelta - size).sqrMagnitude > 0.0001f)
            {
                rect.sizeDelta = size;
            }
        }

        private void SetSourceHidden(bool hidden)
        {
            if (_sourceHidden == hidden || _source.canvasRenderer == null)
            {
                return;
            }

            if (hidden)
            {
                _sourceCullBeforeHide = _source.canvasRenderer.cull;
                _source.canvasRenderer.cull = true;
            }
            else
            {
                _source.canvasRenderer.cull = _sourceCullBeforeHide;
            }

            _sourceHidden = hidden;
        }

        private static void SetAnchoredX(RectTransform rect, float x)
        {
            Vector2 current = rect.anchoredPosition;
            if (Mathf.Approximately(current.x, x) && Mathf.Approximately(current.y, 0f))
            {
                return;
            }

            rect.anchoredPosition = new Vector2(x, 0f);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active)
            {
                component.gameObject.SetActive(active);
            }
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
