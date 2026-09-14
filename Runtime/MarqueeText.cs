using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MarqueeText
{
    /// <summary>
    /// Lightweight horizontal marquee for TextMeshProUGUI.
    /// Attach it to a TextMeshProUGUI object and resize that object's RectTransform
    /// to define the viewport. Text scrolls only when it overflows.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(RectMask2D))]
    [AddComponentMenu("UI/Marquee Text (TextMeshPro)")]
    [DisallowMultipleComponent]
    public sealed class MarqueeText : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Horizontal direction in which overflowing text moves.")]
        private MarqueeDirection _direction = MarqueeDirection.RightToLeft;

        [SerializeField]
        [Tooltip("Continuous, Restart, or Ping Pong behavior.")]
        private MarqueeScrollMode _scrollMode = MarqueeScrollMode.Continuous;

        [SerializeField, Min(0.01f)]
        [Tooltip("Scroll speed in pixels per second.")]
        private float _speed = 60f;

        [SerializeField, Min(0f)]
        [Tooltip("Initial delay before movement begins.")]
        private float _startDelay = 1f;

        [SerializeField, Min(0f)]
        [Tooltip("Space between copies in Continuous mode.")]
        private float _gap = 50f;

        [FormerlySerializedAs("_loopDelay")]
        [SerializeField, Min(0f)]
        [Tooltip("Pause at an edge in Restart and Ping Pong modes.")]
        private float _endPause = 1f;

        [SerializeField]
        [Tooltip("Continue scrolling while Time.timeScale is zero.")]
        private bool _useUnscaledTime;

        private readonly MarqueeEngine _engine = new MarqueeEngine();
        private TextMeshProUGUI _sourceText;
        private RectTransform _viewport;
        private MarqueeRenderer _renderer;
        private UnityAction _markDirtyAction;
        private bool _callbacksRegistered;
        private bool _isDirty = true;
        private bool _isPlaying = true;
        private bool _isPreviewing;

        /// <summary>Gets or sets the scroll speed in pixels per second.</summary>
        public float Speed
        {
            get => _speed;
            set
            {
                _speed = Mathf.Max(0.01f, value);
                ConfigureEngine();
            }
        }

        /// <summary>Whether playback is currently allowed to advance.</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>Whether the current text is wider than its viewport.</summary>
        public bool IsOverflowing => _engine.IsOverflowing;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            RegisterSourceCallbacks();
            _isPlaying = true;
            _isDirty = true;
            _engine.Restart();
        }

        private void OnDisable()
        {
            UnregisterSourceCallbacks();
            _isPreviewing = false;
            _renderer?.Deactivate();
        }

        private void OnDestroy()
        {
            UnregisterSourceCallbacks();
            _renderer?.Dispose();
            _renderer = null;
        }

        private void OnRectTransformDimensionsChange()
        {
            _isDirty = true;
        }

        private void OnValidate()
        {
            _speed = Mathf.Max(0.01f, _speed);
            _startDelay = Mathf.Max(0f, _startDelay);
            _gap = Mathf.Max(0f, _gap);
            _endPause = Mathf.Max(0f, _endPause);
            ConfigureEngine();
            _isDirty = true;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Tick(deltaTime);
        }

        /// <summary>Resumes from the current position.</summary>
        public void Play()
        {
            _isPlaying = true;
        }

        /// <summary>Pauses at the current position.</summary>
        public void Pause()
        {
            _isPlaying = false;
        }

        /// <summary>Returns to the initial position, applies Start Delay, and plays.</summary>
        public void Restart()
        {
            EnsureReferences();
            _isPlaying = true;

            if (_isDirty)
            {
                Refresh();
                return;
            }

            _engine.Restart();
            if (_engine.IsOverflowing)
            {
                _renderer?.Apply(_engine, _direction);
            }
        }

        private void Tick(float deltaTime)
        {
            if (_isDirty)
            {
                Refresh();
            }

            if (!_isPlaying || !_engine.IsOverflowing)
            {
                return;
            }

            if (_engine.Tick(deltaTime))
            {
                _renderer.Apply(_engine, _direction);
            }
        }

        private void Refresh()
        {
            EnsureReferences();
            ConfigureEngine();
            _renderer.Measure(out float viewportWidth, out float contentWidth);
            _engine.SetGeometry(viewportWidth, contentWidth);
            _engine.Restart();
            _renderer.Prepare(_engine.IsOverflowing, _scrollMode, contentWidth);

            if (_engine.IsOverflowing)
            {
                _renderer.Apply(_engine, _direction);
            }

            _isDirty = false;
        }

        private void ConfigureEngine()
        {
            _engine.Configure(_scrollMode, _speed, _gap, _startDelay, _endPause);
        }

        private void EnsureReferences()
        {
            if (_sourceText == null)
            {
                _sourceText = GetComponent<TextMeshProUGUI>();
            }

            if (_viewport == null)
            {
                _viewport = GetComponent<RectTransform>();
            }

            if (_markDirtyAction == null)
            {
                _markDirtyAction = MarkDirty;
            }

            if (_renderer == null && _sourceText != null && _viewport != null)
            {
                _renderer = new MarqueeRenderer(_sourceText, _viewport);
            }

            ConfigureEngine();
        }

        private void RegisterSourceCallbacks()
        {
            if (_callbacksRegistered || _sourceText == null)
            {
                return;
            }

            // Text, font, size, wrapping, and most layout-affecting TMP changes invoke this.
            _sourceText.RegisterDirtyLayoutCallback(_markDirtyAction);
            _sourceText.RegisterDirtyMaterialCallback(_markDirtyAction);
            _callbacksRegistered = true;
        }

        private void UnregisterSourceCallbacks()
        {
            if (!_callbacksRegistered || _sourceText == null)
            {
                return;
            }

            _sourceText.UnregisterDirtyLayoutCallback(_markDirtyAction);
            _sourceText.UnregisterDirtyMaterialCallback(_markDirtyAction);
            _callbacksRegistered = false;
        }

        private void MarkDirty()
        {
            _isDirty = true;
        }

        internal bool IsEditorPreviewing => _isPreviewing;
        internal float OffsetForTests => _engine.Offset;
        internal RectTransform PrimaryRectForTests => _renderer?.PrimaryRect;

        internal void BeginEditorPreview()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureReferences();
            RegisterSourceCallbacks();
            _isPreviewing = true;
            _isPlaying = true;
            _isDirty = true;
            Refresh();
        }

        internal void TickEditorPreview(float deltaTime)
        {
            if (_isPreviewing && !Application.isPlaying)
            {
                Tick(Mathf.Min(0.1f, Mathf.Max(0f, deltaTime)));
            }
        }

        internal void EndEditorPreview()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!_isPreviewing && _renderer == null)
            {
                return;
            }

            _isPreviewing = false;
            _renderer?.Dispose();
            _renderer = null;
            _engine.Restart();
            _isDirty = true;
        }

        internal void TickForTests(float deltaTime) => Tick(deltaTime);
        internal void RefreshForTests() => Refresh();
    }
}
