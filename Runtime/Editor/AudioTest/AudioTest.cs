#if UNITY_EDITOR

using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;

namespace UnityExtensions.Editor
{
    class AudioTest : ScriptableAssetSingleton<AudioTest>
    {
        [SerializeField, GetSet("enableTest")]
        bool _enableTest = true;

        public Texture2D playImage = default;
        public Texture2D stopImage = default;

        bool _lastEnableTest = false;

        public bool enableTest
        {
            get => _enableTest;
            set
            {
                if (_enableTest != value)
                {
                    _enableTest = value;
                    _lastEnableTest = _enableTest;

                    if (_enableTest)
                        EditorApplication.hierarchyWindowItemOnGUI += ItemGUI;
                    else
                        EditorApplication.hierarchyWindowItemOnGUI -= ItemGUI;
                }
            }
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.delayCall += () =>
            {
                if (instance.enableTest)
                    EditorApplication.hierarchyWindowItemOnGUI += ItemGUI;
            };
        }

        void OnValidate()
        {
            if (_lastEnableTest != _enableTest)
            {
                _enableTest = _lastEnableTest;
                enableTest = !_enableTest;
            }
        }

        static void ItemGUI(int instanceID, Rect rect)
        {
            var go = (GameObject)EditorUtility.InstanceIDToObject(instanceID);
            if (go && go.TryGetComponent(out AudioSource source))
            {
                bool disabled = !source.isPlaying && (!source.clip || !source.isActiveAndEnabled);
                using (GUIColorScope.New(disabled ? new Color(0.5f, 0.5f, 0.5f) : new Color(1f, 0.75f, 0f)))
                {
                    using (DisabledScope.New(disabled))
                    {
                        rect.xMin = rect.xMax - rect.height;
                        if (GUI.Button(rect, source.isPlaying ? instance.stopImage : instance.playImage, GUIStyle.none))
                        {
                            if (source.isPlaying) source.Stop();
                            else source.Play();
                        }
                    }
                }
            }
        }

        [MenuItem("Tools/Open Audio Test Scene")]
        static void OpenScene()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Audio Test";

            var listener = new GameObject("Audio Listener");
            listener.transform.ResetLocal();
            listener.AddComponent<AudioListener>();

            EditorSceneManager.MoveGameObjectToScene(listener, scene);

            var source = new GameObject("Audio Source");
            source.transform.position = new Vector3(0, 0, 2);
            source.transform.rotation = Quaternion.Euler(0, 180, 0);
            source.AddComponent<AudioSource>();

            EditorSceneManager.MoveGameObjectToScene(source, scene);

            Selection.activeGameObject = source;
            if (SceneView.lastActiveSceneView)
            {
                SceneView.lastActiveSceneView.LookAtDirect(listener.transform.position, Quaternion.Euler(90, 0, 0));
                SceneView.lastActiveSceneView.orthographic = true;
            }
        }

        [MenuItem("Assets/Create/Unity Extensions/Editor/Audio Test")]
        static void CreateAsset()
        {
            CreateOrSelectAsset(false);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected, typeof(AudioListener))]
        static void DrawAudioListener(AudioListener listener, GizmoType type)
        {
            if (instance.enableTest)
            {
                using (HandlesColorScope.New(new Color(1f, 0.5f, 0f, listener.isActiveAndEnabled ? 1f : 0.25f)))
                {
                    var scale = HandleUtility.GetHandleSize(listener.transform.position);
                    Handles.ArrowHandleCap(0, listener.transform.position, listener.transform.rotation, 0.6f * scale, EventType.Repaint);
                    Handles.DrawWireDisc(listener.transform.position, listener.transform.up, 0.125f * scale);
                }
            }
        }
    }
}

#endif