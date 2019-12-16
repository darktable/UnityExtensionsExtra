#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace UnityExtensions.Editor
{
    [System.Serializable]
    public class MeasurementSettings : EditorSettings<MeasurementSettings>
    {
        [System.NonSerialized] public Transform startTrans;
        public Vector3 startPos;
        [System.NonSerialized] public Transform endTrans;
        public Vector3 endPos = new Vector3(2, 3, 4);

        public bool showXYZ = true;
        public bool showYZ;
        public bool showXZ;
        public bool showXY;

        public bool showMoveTools = true;
    }

    class MeasurementWindow : SettingsWindow<MeasurementSettings>
    {
        [MenuItem("Window/Unity Extensions/Measurement")]
        static void ShowWindow()
        {
            var instance = GetWindow<MeasurementWindow>();
            instance.titleContent = new GUIContent("Measurement");
            instance.autoRepaintOnSceneChange = true;
            instance.ShowUtility();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SceneView.duringSceneGui -= OnSceneGUI;
            Tools.hidden = false;
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Start", EditorStyles.boldLabel);

            using (var scope = ChangeCheckScope.New(this))
            {
                var rect = EditorGUILayout.GetControlRect();
                rect.width -= 8 + rect.height * 3;
                var newStartTrans = EditorGUI.ObjectField(rect, GUIContent.none, settings.startTrans, typeof(Transform), true) as Transform;

                rect.x = rect.xMax + 8;
                rect.width = rect.height * 1.5f;
                if (GUI.Button(rect, EditorGUIUtilities.TempContent("S", null, "Use selection"), EditorStyles.miniButtonLeft)) newStartTrans = Selection.activeTransform;

                rect.x = rect.xMax;
                if (GUI.Button(rect, EditorGUIUtilities.TempContent("C", null, "Clear reference"), EditorStyles.miniButtonRight)) newStartTrans = null;

                if (scope.changed) settings.startTrans = newStartTrans;
            }

            if (settings.startTrans)
            {
                using (var scope = ChangeCheckScope.New(settings.startTrans))
                {
                    settings.startPos = EditorGUILayout.Vector3Field(GUIContent.none, settings.startTrans.position);
                    if (scope.changed) settings.startTrans.position = settings.startPos;
                }
            }
            else
            {
                using (var scope = ChangeCheckScope.New(this))
                {
                    var newStartPos = EditorGUILayout.Vector3Field(GUIContent.none, settings.startPos);
                    if (scope.changed) settings.startPos = newStartPos;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("End", EditorStyles.boldLabel);

            using (var scope = ChangeCheckScope.New(this))
            {
                var rect = EditorGUILayout.GetControlRect();
                rect.width -= 8 + rect.height * 3;
                var newEndTrans = EditorGUI.ObjectField(rect, GUIContent.none, settings.endTrans, typeof(Transform), true) as Transform;

                rect.x = rect.xMax + 8;
                rect.width = rect.height * 1.5f;
                if (GUI.Button(rect, EditorGUIUtilities.TempContent("S", null, "Use selection"), EditorStyles.miniButtonLeft)) newEndTrans = Selection.activeTransform;

                rect.x = rect.xMax;
                if (GUI.Button(rect, EditorGUIUtilities.TempContent("C", null, "Clear reference"), EditorStyles.miniButtonRight)) newEndTrans = null;

                if (scope.changed) settings.endTrans = newEndTrans;
            }

            if (settings.endTrans)
            {
                using (var scope = ChangeCheckScope.New(settings.endTrans))
                {
                    settings.endPos = EditorGUILayout.Vector3Field(GUIContent.none, settings.endTrans.position);
                    if (scope.changed) settings.endTrans.position = settings.endPos;
                }
            }
            else
            {
                using (var scope = ChangeCheckScope.New(this))
                {
                    var newEndPos = EditorGUILayout.Vector3Field(GUIContent.none, settings.endPos);
                    if (scope.changed) settings.endPos = newEndPos;
                }
            }

            Vector3 distance = settings.endPos - settings.startPos;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Distance", EditorStyles.boldLabel);
            EditorGUILayout.FloatField(GUIContent.none, distance.magnitude);

            using (HorizontalLayoutScope.New())
            {
                using (VerticalLayoutScope.New())
                {
                    using (LabelWidthScope.New(20))
                    {
                        EditorGUILayout.FloatField("YZ", distance.yz().magnitude);
                        EditorGUILayout.FloatField("XZ", distance.xz().magnitude);
                        EditorGUILayout.FloatField("XY", distance.xy().magnitude);
                    }
                }

                EditorGUILayout.Space();

                using (VerticalLayoutScope.New())
                {
                    using (LabelWidthScope.New(14))
                    {
                        EditorGUILayout.FloatField("X", distance.x);
                        EditorGUILayout.FloatField("Y", distance.y);
                        EditorGUILayout.FloatField("Z", distance.z);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visualization", EditorStyles.boldLabel);
            settings.showXYZ = GUILayout.Toggle(settings.showXYZ, "Show XYZ 3D", EditorStyles.miniButton);
            settings.showYZ = GUILayout.Toggle(settings.showYZ, "Show YZ Plane", EditorStyles.miniButton);
            settings.showXZ = GUILayout.Toggle(settings.showXZ, "Show XZ Plane", EditorStyles.miniButton);
            settings.showXY = GUILayout.Toggle(settings.showXY, "Show XY Plane", EditorStyles.miniButton);
            EditorGUILayout.Space();
            settings.showMoveTools = GUILayout.Toggle(settings.showMoveTools, "Show Move Tools", EditorStyles.miniButton);
            Tools.hidden = !GUILayout.Toggle(!Tools.hidden, "Show Unity Tools", EditorStyles.miniButton);
        }

        void OnSceneGUI(SceneView scene)
        {
            if (settings.showMoveTools)
            {
                if (settings.startTrans)
                {
                    using (var scope = ChangeCheckScope.New(settings.startTrans))
                    {
                        settings.startPos = Handles.PositionHandle(settings.startTrans.position, Tools.pivotRotation == PivotRotation.Local ? settings.startTrans.rotation : Quaternion.identity);
                        if (scope.changed) settings.startTrans.position = settings.startPos;
                    }
                }
                else
                {
                    using (var scope = ChangeCheckScope.New(this))
                    {
                        var newStartPos = Handles.PositionHandle(settings.startPos, Quaternion.identity);
                        if (scope.changed)
                        {
                            settings.startPos = newStartPos;
                            Repaint();
                        }
                    }
                }

                if (settings.endTrans)
                {
                    using (var scope = ChangeCheckScope.New(settings.endTrans))
                    {
                        settings.endPos = Handles.PositionHandle(settings.endTrans.position, Tools.pivotRotation == PivotRotation.Local ? settings.endTrans.rotation : Quaternion.identity);
                        if (scope.changed) settings.endTrans.position = settings.endPos;
                    }
                }
                else
                {
                    using (var scope = ChangeCheckScope.New(this))
                    {
                        var newEndPos = Handles.PositionHandle(settings.endPos, Quaternion.identity);
                        if (scope.changed)
                        {
                            settings.endPos = newEndPos;
                            Repaint();
                        }
                    }
                }
            }

            Vector3 distance = settings.endPos - settings.startPos;
            Vector3 temp;
            float length;

            if (settings.showYZ)
            {
                temp = new Vector3(settings.endPos.x, settings.startPos.y, settings.startPos.z);

                length = Mathf.Abs(distance.x);
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(1f, 0.4f, 0.4f);
                    Handles.DrawLine(settings.startPos, temp);
                    Handles.Label((settings.startPos + temp) * 0.5f, "X: " + length, EditorStyles.whiteBoldLabel);
                }

                length = distance.yz().magnitude;
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(0.3f, 0.9f, 0.9f);
                    Handles.DrawLine(settings.endPos, temp);
                    Handles.Label((settings.endPos + temp) * 0.5f, "YZ: " + length, EditorStyles.whiteBoldLabel);
                }
            }

            if (settings.showXZ)
            {
                temp = new Vector3(settings.startPos.x, settings.endPos.y, settings.startPos.z);

                length = Mathf.Abs(distance.y);
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(0.4f, 0.9f, 0.4f);
                    Handles.DrawLine(settings.startPos, temp);
                    Handles.Label((settings.startPos + temp) * 0.5f, "Y: " + length, EditorStyles.whiteBoldLabel);
                }

                length = distance.xz().magnitude;
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(1f, 0.4f, 1f);
                    Handles.DrawLine(settings.endPos, temp);
                    Handles.Label((settings.endPos + temp) * 0.5f, "XZ: " + length, EditorStyles.whiteBoldLabel);
                }
            }

            if (settings.showXY)
            {
                temp = new Vector3(settings.startPos.x, settings.startPos.y, settings.endPos.z);

                length = Mathf.Abs(distance.z);
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(0.4f, 0.7f, 1f);
                    Handles.DrawLine(settings.startPos, temp);
                    Handles.Label((settings.startPos + temp) * 0.5f, "Z: " + length, EditorStyles.whiteBoldLabel);
                }

                length = distance.xy().magnitude;
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(0.85f, 0.85f, 0.3f);
                    Handles.DrawLine(settings.endPos, temp);
                    Handles.Label((settings.endPos + temp) * 0.5f, "XY: " + length, EditorStyles.whiteBoldLabel);
                }
            }

            if (settings.showXYZ)
            {
                length = distance.magnitude;
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = Color.white;
                    Handles.DrawLine(settings.startPos, settings.endPos);
                    Handles.Label((settings.startPos + settings.endPos) * 0.5f, "XYZ: " + length, EditorStyles.whiteBoldLabel);
                }
            }
        }

    } // class MeasurementWindow

} // namespace UnityExtensions.Editor

#endif