#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace UnityExtensions.Editor
{
    class MeasurementWindow : SerializableWindowSingleton<MeasurementWindow>
    {
        public Transform startTrans;
        public Vector3 startPos;
        public Transform endTrans;
        public Vector3 endPos = new Vector3(2, 3, 4);

        public bool showXYZ = true;
        public bool showYZ;
        public bool showXZ;
        public bool showXY;
            
        public bool showMoveTools = true;


        [MenuItem("Window/Unity Extensions/Measurement")]
        static void ShowWindow()
        {
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
                var newStartTrans = EditorGUI.ObjectField(rect, GUIContent.none, startTrans, typeof(Transform), true) as Transform;

                rect.x = rect.xMax + 8;
                rect.width = rect.height * 1.5f;
                if (GUI.Button(rect, EditorGUIUtilities.TempContent("S", null, "Use selection"), EditorStyles.miniButtonLeft)) newStartTrans = Selection.activeTransform;

                rect.x = rect.xMax;
                if (GUI.Button(rect, EditorGUIUtilities.TempContent("C", null, "Clear reference"), EditorStyles.miniButtonRight)) newStartTrans = null;

                if (scope.changed) startTrans = newStartTrans;
            }

            if (startTrans)
            {
                using (var scope = ChangeCheckScope.New(startTrans))
                {
                    startPos = EditorGUILayout.Vector3Field(GUIContent.none, startTrans.position);
                    if (scope.changed) startTrans.position = startPos;
                }
            }
            else
            {
                using (var scope = ChangeCheckScope.New(this))
                {
                    var newStartPos = EditorGUILayout.Vector3Field(GUIContent.none, startPos);
                    if (scope.changed) startPos = newStartPos;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("End", EditorStyles.boldLabel);

            using (var scope = ChangeCheckScope.New(this))
            {
                var rect = EditorGUILayout.GetControlRect();
                rect.width -= 8 + rect.height * 3;
                var newEndTrans = EditorGUI.ObjectField(rect, GUIContent.none, endTrans, typeof(Transform), true) as Transform;

                rect.x = rect.xMax + 8;
                rect.width = rect.height * 1.5f;
                if (GUI.Button(rect, EditorGUIUtilities.TempContent("S", null, "Use selection"), EditorStyles.miniButtonLeft)) newEndTrans = Selection.activeTransform;

                rect.x = rect.xMax;
                if (GUI.Button(rect, EditorGUIUtilities.TempContent("C", null, "Clear reference"), EditorStyles.miniButtonRight)) newEndTrans = null;

                if (scope.changed) endTrans = newEndTrans;
            }

            if (endTrans)
            {
                using (var scope = ChangeCheckScope.New(endTrans))
                {
                    endPos = EditorGUILayout.Vector3Field(GUIContent.none, endTrans.position);
                    if (scope.changed) endTrans.position = endPos;
                }
            }
            else
            {
                using (var scope = ChangeCheckScope.New(this))
                {
                    var newEndPos = EditorGUILayout.Vector3Field(GUIContent.none, endPos);
                    if (scope.changed) endPos = newEndPos;
                }
            }

            Vector3 distance = endPos - startPos;

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
            showXYZ = GUILayout.Toggle(showXYZ, "Show XYZ 3D", EditorStyles.miniButton);
            showYZ = GUILayout.Toggle(showYZ, "Show YZ Plane", EditorStyles.miniButton);
            showXZ = GUILayout.Toggle(showXZ, "Show XZ Plane", EditorStyles.miniButton);
            showXY = GUILayout.Toggle(showXY, "Show XY Plane", EditorStyles.miniButton);
            EditorGUILayout.Space();
            showMoveTools = GUILayout.Toggle(showMoveTools, "Show Move Tools", EditorStyles.miniButton);
            Tools.hidden = !GUILayout.Toggle(!Tools.hidden, "Show Unity Tools", EditorStyles.miniButton);
        }


        void OnSceneGUI(SceneView scene)
        {
            if (showMoveTools)
            {
                if (startTrans)
                {
                    using (var scope = ChangeCheckScope.New(startTrans))
                    {
                        startPos = Handles.PositionHandle(startTrans.position, Tools.pivotRotation == PivotRotation.Local ? startTrans.rotation : Quaternion.identity);
                        if (scope.changed) startTrans.position = startPos;
                    }
                }
                else
                {
                    using (var scope = ChangeCheckScope.New(this))
                    {
                        var newStartPos = Handles.PositionHandle(startPos, Quaternion.identity);
                        if (scope.changed)
                        {
                            startPos = newStartPos;
                            Repaint();
                        }
                    }
                }

                if (endTrans)
                {
                    using (var scope = ChangeCheckScope.New(endTrans))
                    {
                        endPos = Handles.PositionHandle(endTrans.position, Tools.pivotRotation == PivotRotation.Local ? endTrans.rotation : Quaternion.identity);
                        if (scope.changed) endTrans.position = endPos;
                    }
                }
                else
                {
                    using (var scope = ChangeCheckScope.New(this))
                    {
                        var newEndPos = Handles.PositionHandle(endPos, Quaternion.identity);
                        if (scope.changed)
                        {
                            endPos = newEndPos;
                            Repaint();
                        }
                    }
                }
            }

            Vector3 distance = endPos - startPos;
            Vector3 temp;
            float length;

            if (showYZ)
            {
                temp = new Vector3(endPos.x, startPos.y, startPos.z);

                length = Mathf.Abs(distance.x);
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(1f, 0.4f, 0.4f);
                    Handles.DrawLine(startPos, temp);
                    Handles.Label((startPos + temp) * 0.5f, "X: " + length, EditorStyles.whiteBoldLabel);
                }

                length = distance.yz().magnitude;
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(0.3f, 0.9f, 0.9f);
                    Handles.DrawLine(endPos, temp);
                    Handles.Label((endPos + temp) * 0.5f, "YZ: " + length, EditorStyles.whiteBoldLabel);
                }
            }

            if (showXZ)
            {
                temp = new Vector3(startPos.x, endPos.y, startPos.z);

                length = Mathf.Abs(distance.y);
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(0.4f, 0.9f, 0.4f);
                    Handles.DrawLine(startPos, temp);
                    Handles.Label((startPos + temp) * 0.5f, "Y: " + length, EditorStyles.whiteBoldLabel);
                }

                length = distance.xz().magnitude;
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(1f, 0.4f, 1f);
                    Handles.DrawLine(endPos, temp);
                    Handles.Label((endPos + temp) * 0.5f, "XZ: " + length, EditorStyles.whiteBoldLabel);
                }
            }

            if (showXY)
            {
                temp = new Vector3(startPos.x, startPos.y, endPos.z);

                length = Mathf.Abs(distance.z);
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(0.4f, 0.7f, 1f);
                    Handles.DrawLine(startPos, temp);
                    Handles.Label((startPos + temp) * 0.5f, "Z: " + length, EditorStyles.whiteBoldLabel);
                }

                length = distance.xy().magnitude;
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = new Color(0.85f, 0.85f, 0.3f);
                    Handles.DrawLine(endPos, temp);
                    Handles.Label((endPos + temp) * 0.5f, "XY: " + length, EditorStyles.whiteBoldLabel);
                }
            }

            if (showXYZ)
            {
                length = distance.magnitude;
                if (length > Mathf.Epsilon)
                {
                    GUI.contentColor = Handles.color = Color.white;
                    Handles.DrawLine(startPos, endPos);
                    Handles.Label((startPos + endPos) * 0.5f, "XYZ: " + length, EditorStyles.whiteBoldLabel);
                }
            }
        }

    } // class MeasurementWindow

} // namespace UnityExtensions.Editor

#endif