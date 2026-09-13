using TMPro;
using UnityEditor;
using UnityEngine;
using MarqueeText;

namespace MarqueeText.Editor
{
    [CustomEditor(typeof(MarqueeText))]
    [CanEditMultipleObjects]
    public class MarqueeTextEditor : UnityEditor.Editor
    {
        private SerializedProperty _directionProp;
        private SerializedProperty _scrollModeProp;
        private SerializedProperty _speedProp;
        private SerializedProperty _gapProp;
        private SerializedProperty _startDelayProp;
        private SerializedProperty _loopDelayProp;
        private SerializedProperty _useUnscaledTimeProp;
        private SerializedProperty _triggerModeProp;
        private SerializedProperty _previewInEditorProp;

        private SerializedProperty _onScrollStartedProp;
        private SerializedProperty _onScrollLoopCompletedProp;
        private SerializedProperty _onScrollPausedProp;
        private SerializedProperty _onScrollResumedProp;

        private bool _showEvents = false;

        private void OnEnable()
        {
            _directionProp = serializedObject.FindProperty("_direction");
            _scrollModeProp = serializedObject.FindProperty("_scrollMode");
            _speedProp = serializedObject.FindProperty("_speed");
            _gapProp = serializedObject.FindProperty("_gap");
            _startDelayProp = serializedObject.FindProperty("_startDelay");
            _loopDelayProp = serializedObject.FindProperty("_loopDelay");
            _useUnscaledTimeProp = serializedObject.FindProperty("_useUnscaledTime");
            _triggerModeProp = serializedObject.FindProperty("_triggerMode");
            _previewInEditorProp = serializedObject.FindProperty("_previewInEditor");

            _onScrollStartedProp = serializedObject.FindProperty("onScrollStarted");
            _onScrollLoopCompletedProp = serializedObject.FindProperty("onScrollLoopCompleted");
            _onScrollPausedProp = serializedObject.FindProperty("onScrollPaused");
            _onScrollResumedProp = serializedObject.FindProperty("onScrollResumed");

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private double _lastRepaintTime;

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying && target != null)
            {
                var marquee = target as MarqueeText;
                if (marquee != null && marquee.PreviewInEditor)
                {
                    double now = EditorApplication.timeSinceStartup;
                    // Throttle editor preview repaints to ~60fps to prevent CPU/GPU overload
                    if (now - _lastRepaintTime >= 0.016)
                    {
                        _lastRepaintTime = now;
                        EditorApplication.QueuePlayerLoopUpdate();
                        SceneView.RepaintAll();
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var marquee = (MarqueeText)target;

            // Status Card
            DrawStatusCard(marquee);

            EditorGUILayout.Space(6);

            // Scroll Settings Group
            EditorGUILayout.LabelField("Scroll Settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_directionProp);
                EditorGUILayout.PropertyField(_scrollModeProp);
                EditorGUILayout.PropertyField(_speedProp);

                if (_scrollModeProp.enumValueIndex == (int)MarqueeScrollMode.Continuous)
                {
                    EditorGUILayout.PropertyField(_gapProp);
                }
            }

            EditorGUILayout.Space(6);

            // Timing Settings Group
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_startDelayProp);

                if (_scrollModeProp.enumValueIndex != (int)MarqueeScrollMode.Continuous)
                {
                    EditorGUILayout.PropertyField(_loopDelayProp);
                }

                EditorGUILayout.PropertyField(_useUnscaledTimeProp);
            }

            EditorGUILayout.Space(6);

            // Trigger & Behavior Group
            EditorGUILayout.LabelField("Trigger & Behavior", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_triggerModeProp);
                EditorGUILayout.PropertyField(_previewInEditorProp);
            }

            EditorGUILayout.Space(6);

            // Controls (Editor Playback & Test Preview)
            if (targets.Length == 1)
            {
                DrawPlaybackControls(marquee);
            }

            EditorGUILayout.Space(6);

            // Events
            _showEvents = EditorGUILayout.Foldout(_showEvents, "Events", true, EditorStyles.foldoutHeader);
            if (_showEvents)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(_onScrollStartedProp);
                    EditorGUILayout.PropertyField(_onScrollLoopCompletedProp);
                    EditorGUILayout.PropertyField(_onScrollPausedProp);
                    EditorGUILayout.PropertyField(_onScrollResumedProp);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStatusCard(MarqueeText marquee)
        {
            bool isHorizontal = marquee.Direction == MarqueeDirection.RightToLeft || marquee.Direction == MarqueeDirection.LeftToRight;
            float textDim = isHorizontal ? marquee.TextWidth : marquee.TextHeight;
            float viewDim = isHorizontal ? marquee.ViewportWidth : marquee.ViewportHeight;
            string dimLabel = isHorizontal ? "Width" : "Height";

            bool overflowing = marquee.IsOverflowing;

            Color defaultBg = GUI.backgroundColor;
            GUI.backgroundColor = overflowing ? new Color(0.3f, 0.8f, 0.4f, 0.4f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUI.backgroundColor = defaultBg;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Status:", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                if (overflowing)
                {
                    GUILayout.Label("● Overflowing (Active)", EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label("○ Fits within bounds (Static)", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Text {dimLabel}:", EditorStyles.miniLabel, GUILayout.Width(70));
                GUILayout.Label($"{textDim:F1} px", EditorStyles.miniBoldLabel);
                GUILayout.Label($"Viewport {dimLabel}:", EditorStyles.miniLabel, GUILayout.Width(90));
                GUILayout.Label($"{viewDim:F1} px", EditorStyles.miniBoldLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPlaybackControls(MarqueeText marquee)
        {
            EditorGUILayout.LabelField(Application.isPlaying ? "Playback Controls" : "Edit Mode Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (!Application.isPlaying)
            {
                bool isPreviewing = marquee.PreviewInEditor;
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = isPreviewing ? new Color(1f, 0.45f, 0.45f) : new Color(0.45f, 0.9f, 0.55f);

                if (GUILayout.Button(isPreviewing ? "⏹ Stop Preview" : "▶ Test Preview", GUILayout.Height(26)))
                {
                    marquee.PreviewInEditor = !isPreviewing;
                    if (marquee.PreviewInEditor)
                    {
                        marquee.Restart();
                    }
                    else
                    {
                        marquee.ResetPosition();
                    }
                    EditorUtility.SetDirty(marquee);
                }
                GUI.backgroundColor = prevBg;

                if (isPreviewing)
                {
                    if (GUILayout.Button("🔄 Restart", GUILayout.Height(26), GUILayout.Width(75)))
                    {
                        marquee.Restart();
                    }
                }
            }
            else
            {
                if (GUILayout.Button(marquee.IsPlaying ? "⏸ Pause" : "▶ Play", GUILayout.Height(24)))
                {
                    if (marquee.IsPlaying) marquee.Pause();
                    else marquee.Play();
                }

                if (GUILayout.Button("🔄 Restart", GUILayout.Height(24)))
                {
                    marquee.Restart();
                }

                if (GUILayout.Button("⏹ Reset", GUILayout.Height(24)))
                {
                    marquee.ResetPosition();
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
