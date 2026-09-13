using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarqueeText
{
    /// <summary>
    /// Component that automatically scrolls TextMeshPro text like an LED electric bulletin board / ticker.
    /// Attach to any GameObject with a TMP_Text component to enable seamless marquee scrolling.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    [RequireComponent(typeof(RectMask2D))]
    [AddComponentMenu("UI/Marquee Text (TMPro)")]
    [DisallowMultipleComponent]
    public class MarqueeText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Scroll Settings")]
        [Tooltip("Direction in which the text scrolls.")]
        [SerializeField] private MarqueeDirection _direction = MarqueeDirection.RightToLeft;

        [Tooltip("Scrolling and looping behavior.")]
        [SerializeField] private MarqueeScrollMode _scrollMode = MarqueeScrollMode.Continuous;

        [Tooltip("Scroll speed in pixels per second.")]
        [SerializeField] private float _speed = 60f;

        [Tooltip("Spacing in pixels between successive text repetitions in Continuous mode.")]
        [SerializeField] private float _gap = 50f;

        [Header("Timing")]
        [Tooltip("Initial delay in seconds before scrolling starts.")]
        [SerializeField] private float _startDelay = 1.0f;

        [Tooltip("Delay in seconds when reaching the end/pause point in Restart or PingPong mode.")]
        [SerializeField] private float _loopDelay = 1.0f;

        [Tooltip("Use unscaled time (scrolls even when Time.timeScale = 0).")]
        [SerializeField] private bool _useUnscaledTime = false;

        [Header("Trigger Conditions")]
        [Tooltip("Condition required for scrolling to occur.")]
        [SerializeField] private MarqueeTriggerMode _triggerMode = MarqueeTriggerMode.OnlyIfOverflowing;

        [Header("Editor Preview")]
        [Tooltip("Preview the marquee animation inside the Unity Scene/Game view while in Edit Mode.")]
        [SerializeField] private bool _previewInEditor = false;

        [Header("Events")]
        public UnityEvent onScrollStarted = new UnityEvent();
        public UnityEvent onScrollLoopCompleted = new UnityEvent();
        public UnityEvent onScrollPaused = new UnityEvent();
        public UnityEvent onScrollResumed = new UnityEvent();

        // References
        private TMP_Text _sourceText;
        private RectTransform _viewportRect;
        private RectMask2D _mask;

        // Sub-text renderers for clean clipping and seamless looping
        private TMP_Text _primaryText;
        private TMP_Text _ghostText;
        private RectTransform _primaryRect;
        private RectTransform _ghostRect;

        // Pure computation engine
        private readonly MarqueeCore _core = new MarqueeCore();

        // State flags
        private bool _isPlaying = true;
        private bool _isHovered = false;
        private bool _isDirty = true;

        // Cached values for change detection guards
        private string _lastText;
        private TMP_FontAsset _lastFont;
        private float _lastFontSize = -1f;
        private Vector2 _lastViewportSize;

#if UNITY_EDITOR
        private double _lastEditorTime;
#endif

        #region Public Properties

        public MarqueeDirection Direction
        {
            get => _direction;
            set { _direction = value; _core.Direction = value; ResetPosition(); }
        }

        public MarqueeScrollMode ScrollMode
        {
            get => _scrollMode;
            set { _scrollMode = value; _core.ScrollMode = value; ResetPosition(); }
        }

        public float Speed
        {
            get => _speed;
            set { _speed = Mathf.Max(0.01f, value); _core.Speed = _speed; }
        }

        public float Gap
        {
            get => _gap;
            set { _gap = Mathf.Max(0f, value); _core.Gap = _gap; MarkDirty(); }
        }

        public float StartDelay
        {
            get => _startDelay;
            set { _startDelay = Mathf.Max(0f, value); _core.StartDelay = _startDelay; }
        }

        public float LoopDelay
        {
            get => _loopDelay;
            set { _loopDelay = Mathf.Max(0f, value); _core.LoopDelay = _loopDelay; }
        }

        public bool UseUnscaledTime { get => _useUnscaledTime; set => _useUnscaledTime = value; }
        public MarqueeTriggerMode TriggerMode { get => _triggerMode; set => _triggerMode = value; }
        public bool PreviewInEditor { get => _previewInEditor; set => _previewInEditor = value; }

        public bool IsOverflowing => _core.IsOverflowing;
        public bool IsPlaying => _isPlaying;
        public bool IsScrolling => _isPlaying && ShouldScroll() && _core.CurrentState == MarqueeCore.State.Scrolling;
        public float TextWidth => _core.ContentWidth;
        public float TextHeight => _core.ContentHeight;
        public float ViewportWidth => _core.ViewportWidth;
        public float ViewportHeight => _core.ViewportHeight;

        #endregion

        #region Unity Lifecycle

        private void Awake() => Initialize();

        private void OnEnable()
        {
            Initialize();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMProTextChanged);
            ResetPosition();
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMProTextChanged);
            RestoreSourceText();
            // Don't destroy children on disable to prevent GC allocs and lag spikes during UI open/close!
            if (_primaryText != null) _primaryText.gameObject.SetActive(false);
            if (_ghostText != null) _ghostText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            RestoreSourceText();
            CleanupChildren();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_viewportRect == null) return;
            Vector2 currentSize = _viewportRect.rect.size;
            if (_lastViewportSize != currentSize)
            {
                _lastViewportSize = currentSize;
                MarkDirty();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying && !_previewInEditor)
            {
                if (_isDirty) Refresh();
                _core.Reset();
                ApplyStaticPosition();
#if UNITY_EDITOR
                _lastEditorTime = 0;
#endif
                return;
            }

            if (_isDirty)
            {
                Refresh();
            }

            if (ShouldScroll())
            {
                float dt;
                if (Application.isPlaying)
                {
                    dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                }
                else
                {
#if UNITY_EDITOR
                    double now = UnityEditor.EditorApplication.timeSinceStartup;
                    dt = _lastEditorTime > 0 ? (float)(now - _lastEditorTime) : 0.0166f;
                    _lastEditorTime = now;
                    if (dt > 0.1f) dt = 0.0166f;
#else
                    dt = 0.0166f;
#endif
                }

                if (dt > 0f) _core.Update(dt);
                ApplyScrollingPositions();
            }
            else
            {
                _core.Reset();
                ApplyStaticPosition();
            }
        }

        private void OnValidate()
        {
            _speed = Mathf.Max(0.01f, _speed);
            _gap = Mathf.Max(0f, _gap);
            _startDelay = Mathf.Max(0f, _startDelay);
            _loopDelay = Mathf.Max(0f, _loopDelay);

            SyncCoreSettings();
            MarkDirty();
        }

        #endregion

        #region Public Controls

        public void Play()
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                onScrollResumed?.Invoke();
            }
        }

        public void Pause()
        {
            if (_isPlaying)
            {
                _isPlaying = false;
                onScrollPaused?.Invoke();
            }
        }

        public void Stop()
        {
            _isPlaying = false;
            ResetPosition();
            onScrollPaused?.Invoke();
        }

        public void Restart()
        {
            _isPlaying = true;
            ResetPosition();
            onScrollStarted?.Invoke();
        }

        public void SetText(string newText)
        {
            if (_sourceText == null || _sourceText.text == newText) return;
            _sourceText.text = newText;
            Refresh();
            ResetPosition();
        }

        public void ResetPosition()
        {
            _core.Reset();
            if (ShouldScroll()) ApplyScrollingPositions();
            else ApplyStaticPosition();
        }

        #endregion

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (_triggerMode == MarqueeTriggerMode.OnHover) ResetPosition();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (_triggerMode == MarqueeTriggerMode.OnHover) ResetPosition();
        }

        #endregion

        #region Internal Implementation

        private void Initialize()
        {
            if (_sourceText == null) _sourceText = GetComponent<TMP_Text>();
            if (_viewportRect == null) _viewportRect = GetComponent<RectTransform>();
            if (_mask == null && !TryGetComponent(out _mask))
            {
                _mask = gameObject.AddComponent<RectMask2D>();
            }

            SyncCoreSettings();
            _core.OnScrollStarted -= HandleCoreScrollStarted;
            _core.OnScrollStarted += HandleCoreScrollStarted;
            _core.OnScrollLoopCompleted -= HandleCoreScrollLoopCompleted;
            _core.OnScrollLoopCompleted += HandleCoreScrollLoopCompleted;

            SetupChildren();
            HideSourceTextRendering();
            Refresh();
            ResetPosition();
        }

        private void SyncCoreSettings()
        {
            _core.Direction = _direction;
            _core.ScrollMode = _scrollMode;
            _core.Speed = _speed;
            _core.Gap = _gap;
            _core.StartDelay = _startDelay;
            _core.LoopDelay = _loopDelay;
        }

        private void HandleCoreScrollStarted() => onScrollStarted?.Invoke();
        private void HandleCoreScrollLoopCompleted() => onScrollLoopCompleted?.Invoke();

        private void OnTMProTextChanged(UnityEngine.Object obj)
        {
            if (obj == _sourceText && _sourceText != null)
            {
                // Only mark dirty if text, font, or size has actually changed
                if (_sourceText.text != _lastText ||
                    _sourceText.font != _lastFont ||
                    !Mathf.Approximately(_sourceText.fontSize, _lastFontSize))
                {
                    MarkDirty();
                }
            }
        }

        private void MarkDirty() => _isDirty = true;

        private void Refresh()
        {
            _isDirty = false;
            SetupChildren();
            HideSourceTextRendering();
            SynchronizeProperties();

            if (_sourceText != null)
            {
                _lastText = _sourceText.text;
                _lastFont = _sourceText.font;
                _lastFontSize = _sourceText.fontSize;
            }
            if (_viewportRect != null)
            {
                _lastViewportSize = _viewportRect.rect.size;
            }

            UpdateLayout();
        }

        private void SetupChildren()
        {
            if (_primaryText == null)
            {
                _primaryText = FindOrCreateChild("__Marquee_Primary");
                _primaryRect = _primaryText.rectTransform;
            }
            if (_ghostText == null)
            {
                _ghostText = FindOrCreateChild("__Marquee_Ghost");
                _ghostRect = _ghostText.rectTransform;
            }
        }

        private TMP_Text FindOrCreateChild(string childName)
        {
            Transform existing = transform.Find(childName);
            GameObject childObj = existing != null ? existing.gameObject : new GameObject(childName);

            if (existing == null)
            {
                childObj.transform.SetParent(transform, false);
            }

            childObj.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;

            var rect = childObj.GetComponent<RectTransform>();
            if (rect == null) rect = childObj.AddComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;

            var tmp = childObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = childObj.AddComponent<TextMeshProUGUI>();

            tmp.raycastTarget = false;
            tmp.maskable = true;
            return tmp;
        }

        private void CleanupChildren()
        {
            DestroyHelper(_primaryText != null ? _primaryText.gameObject : null);
            DestroyHelper(_ghostText != null ? _ghostText.gameObject : null);
            _primaryText = null;
            _ghostText = null;
        }

        private void DestroyHelper(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        private void HideSourceTextRendering()
        {
            if (_sourceText != null)
            {
                if (_sourceText.canvasRenderer != null) _sourceText.canvasRenderer.cull = true;
                _sourceText.maskable = false;
            }
        }

        private void RestoreSourceText()
        {
            if (_sourceText != null)
            {
                if (_sourceText.canvasRenderer != null) _sourceText.canvasRenderer.cull = false;
                _sourceText.maskable = true;
            }
        }

        private void SynchronizeProperties()
        {
            if (_sourceText == null || _primaryText == null) return;

            CopyTMPProperties(_sourceText, _primaryText);
            if (_ghostText != null) CopyTMPProperties(_sourceText, _ghostText);
        }

        private void CopyTMPProperties(TMP_Text source, TMP_Text target)
        {
            if (target.text != source.text) target.text = source.text;
            if (target.font != source.font) target.font = source.font;
            if (target.fontSharedMaterial != source.fontSharedMaterial) target.fontSharedMaterial = source.fontSharedMaterial;
            if (!Mathf.Approximately(target.fontSize, source.fontSize)) target.fontSize = source.fontSize;
            if (!Mathf.Approximately(target.fontSizeMin, source.fontSizeMin)) target.fontSizeMin = source.fontSizeMin;
            if (!Mathf.Approximately(target.fontSizeMax, source.fontSizeMax)) target.fontSizeMax = source.fontSizeMax;
            if (target.fontStyle != source.fontStyle) target.fontStyle = source.fontStyle;
            if (target.fontWeight != source.fontWeight) target.fontWeight = source.fontWeight;
            if (target.color != source.color) target.color = source.color;
            if (target.enableVertexGradient != source.enableVertexGradient) target.enableVertexGradient = source.enableVertexGradient;
            if (target.characterSpacing != source.characterSpacing) target.characterSpacing = source.characterSpacing;
            if (target.wordSpacing != source.wordSpacing) target.wordSpacing = source.wordSpacing;
            if (target.lineSpacing != source.lineSpacing) target.lineSpacing = source.lineSpacing;
            if (target.paragraphSpacing != source.paragraphSpacing) target.paragraphSpacing = source.paragraphSpacing;
            if (target.richText != source.richText) target.richText = source.richText;
            if (target.alignment != source.alignment) target.alignment = source.alignment;

            bool isHorizontal = _core.IsHorizontal;
            TextWrappingModes wrapMode = isHorizontal ? TextWrappingModes.NoWrap : source.textWrappingMode;
            if (target.textWrappingMode != wrapMode) target.textWrappingMode = wrapMode;
            if (target.overflowMode != TextOverflowModes.Overflow) target.overflowMode = TextOverflowModes.Overflow;
        }

        private void UpdateLayout()
        {
            if (_sourceText == null || _viewportRect == null || _primaryText == null) return;

            _core.ViewportWidth = _viewportRect.rect.width;
            _core.ViewportHeight = _viewportRect.rect.height;

            _primaryText.ForceMeshUpdate();
            _core.ContentWidth = _primaryText.preferredWidth;
            _core.ContentHeight = _primaryText.preferredHeight;

            Vector2 textSize = new Vector2(_core.ContentWidth, _core.ContentHeight);
            if (_primaryRect != null) _primaryRect.sizeDelta = textSize;
            if (_ghostRect != null) _ghostRect.sizeDelta = textSize;

            if (ShouldScroll()) ApplyScrollingPositions();
            else ApplyStaticPosition();
        }

        private bool ShouldScroll()
        {
            if (!_isPlaying) return false;

            return _triggerMode switch
            {
                MarqueeTriggerMode.OnlyIfOverflowing => _core.IsOverflowing,
                MarqueeTriggerMode.Always => true,
                MarqueeTriggerMode.OnHover => _isHovered && (_core.IsOverflowing || true),
                MarqueeTriggerMode.Manual => true,
                _ => _core.IsOverflowing
            };
        }

        private void ApplyScrollingPositions()
        {
            if (_primaryRect == null) return;

            _core.CalculatePositions(out Vector2 primaryPos, out Vector2 ghostPos, out bool showGhost);
            _primaryRect.anchoredPosition = primaryPos;

            if (_ghostRect != null)
            {
                if (_ghostText.gameObject.activeSelf != showGhost)
                {
                    _ghostText.gameObject.SetActive(showGhost);
                }
                if (showGhost) _ghostRect.anchoredPosition = ghostPos;
            }
        }

        private void ApplyStaticPosition()
        {
            if (_primaryRect == null || _sourceText == null) return;

            TextAlignmentOptions align = _sourceText.alignment;
            int hAlign = (align == TextAlignmentOptions.Center || align == TextAlignmentOptions.Top ||
                          align == TextAlignmentOptions.Bottom || align == TextAlignmentOptions.Midline) ? 0 :
                         (align == TextAlignmentOptions.Right || align == TextAlignmentOptions.TopRight ||
                          align == TextAlignmentOptions.BottomRight || align == TextAlignmentOptions.MidlineRight) ? 1 : -1;

            int vAlign = (align == TextAlignmentOptions.Top || align == TextAlignmentOptions.TopLeft ||
                          align == TextAlignmentOptions.TopRight) ? 1 :
                         (align == TextAlignmentOptions.Bottom || align == TextAlignmentOptions.BottomLeft ||
                          align == TextAlignmentOptions.BottomRight) ? -1 : 0;

            _primaryRect.anchoredPosition = _core.CalculateStaticPosition(hAlign, vAlign);
            if (_ghostText != null && _ghostText.gameObject.activeSelf)
            {
                _ghostText.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
