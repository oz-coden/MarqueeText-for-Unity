using UnityEditor;
using UnityEngine;
using MarqueeComponent = global::MarqueeText.MarqueeText;

namespace MarqueeText.Editor
{
    [CustomEditor(typeof(MarqueeComponent))]
    [CanEditMultipleObjects]
    internal sealed class MarqueeTextEditor : UnityEditor.Editor
    {
        private const double PreviewInterval = 1.0 / 30.0;

        private SerializedProperty _direction;
        private SerializedProperty _scrollMode;
        private SerializedProperty _speed;
        private SerializedProperty _startDelay;
        private SerializedProperty _gap;
        private SerializedProperty _endPause;
        private SerializedProperty _useUnscaledTime;
        private bool _showAdvanced;
        private bool _previewUpdateRegistered;
        private double _lastPreviewTime;

        private void OnEnable()
        {
            _direction = serializedObject.FindProperty("_direction");
            _scrollMode = serializedObject.FindProperty("_scrollMode");
            _speed = serializedObject.FindProperty("_speed");
            _startDelay = serializedObject.FindProperty("_startDelay");
            _gap = serializedObject.FindProperty("_gap");
            _endPause = serializedObject.FindProperty("_endPause");
            _useUnscaledTime = serializedObject.FindProperty("_useUnscaledTime");
        }

        private void OnDisable()
        {
            StopPreview();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_scrollMode);
            EditorGUILayout.PropertyField(_direction);
            EditorGUILayout.PropertyField(_speed);
            EditorGUILayout.PropertyField(_startDelay);

            MarqueeScrollMode mode = (MarqueeScrollMode)_scrollMode.enumValueIndex;
            if (mode == MarqueeScrollMode.Continuous)
            {
                EditorGUILayout.PropertyField(_gap);
            }
            else
            {
                EditorGUILayout.PropertyField(_endPause);
            }

            EditorGUILayout.Space(4f);
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (_showAdvanced)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_useUnscaledTime);
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            if (targets.Length == 1)
            {
                DrawControls((MarqueeComponent)target);
            }
            else if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Edit Mode Preview is available for one selection at a time.", MessageType.Info);
            }
        }

        private void DrawControls(MarqueeComponent marquee)
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(marquee.IsPlaying ? "Pause" : "Play"))
                {
                    if (marquee.IsPlaying) marquee.Pause();
                    else marquee.Play();
                }

                if (GUILayout.Button("Restart"))
                {
                    marquee.Restart();
                }
                EditorGUILayout.EndHorizontal();
                return;
            }

            bool previewing = marquee.IsEditorPreviewing;
            if (GUILayout.Button(previewing ? "Stop Preview" : "Preview", GUILayout.Height(24f)))
            {
                if (previewing) StopPreview();
                else StartPreview(marquee);
            }

            if (previewing && GUILayout.Button("Restart Preview"))
            {
                marquee.Restart();
            }
        }

        private void StartPreview(MarqueeComponent marquee)
        {
            marquee.BeginEditorPreview();
            _lastPreviewTime = EditorApplication.timeSinceStartup;
            RegisterPreviewUpdate();
            RepaintPreviewViews();
        }

        private void StopPreview()
        {
            UnregisterPreviewUpdate();

            if (!Application.isPlaying && target is MarqueeComponent marquee)
            {
                marquee.EndEditorPreview();
                RepaintPreviewViews();
            }
        }

        private void RegisterPreviewUpdate()
        {
            if (_previewUpdateRegistered)
            {
                return;
            }

            EditorApplication.update += OnPreviewUpdate;
            _previewUpdateRegistered = true;
        }

        private void UnregisterPreviewUpdate()
        {
            if (!_previewUpdateRegistered)
            {
                return;
            }

            EditorApplication.update -= OnPreviewUpdate;
            _previewUpdateRegistered = false;
        }

        private void OnPreviewUpdate()
        {
            if (Application.isPlaying || target is not MarqueeComponent marquee || !marquee.IsEditorPreviewing)
            {
                StopPreview();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            double elapsed = now - _lastPreviewTime;
            if (elapsed < PreviewInterval)
            {
                return;
            }

            _lastPreviewTime = now;
            marquee.TickEditorPreview((float)elapsed);
            RepaintPreviewViews();
        }

        private void RepaintPreviewViews()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            Repaint();
        }
    }
}
