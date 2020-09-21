using System;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PrototypeAttribute : Attribute
    {
        public readonly Type parentType;

        public PrototypeAttribute(Type parentType)
        {
            this.parentType = parentType;
        }
    }


    public class Prototype : ScriptableAsset
    {
        [SerializeField, Disable] internal Prototype _parent;
        [SerializeField, HideInInspector] Prototype[] _subPrototypes;

#if UNITY_EDITOR

        UnityEditor.Editor _cachedEditor;

        [CustomEditor(typeof(Prototype), true)]
        [CanEditMultipleObjects]
        public class PrototypeEditor : ScriptableEditor
        {
            static Dictionary<Type, List<Type>> _subPrototypeTypes = new Dictionary<Type, List<Type>>();
            ReorderableList _list;
            new Prototype target => (Prototype)base.target;

            List<Type> GetSubPrototypeTypeList()
            {
                var targetType = target.GetType();

                if (!_subPrototypeTypes.TryGetValue(targetType, out var list))
                {
                    var types = TypeCache.GetTypesWithAttribute<PrototypeAttribute>();
                    foreach (var t in types)
                    {
                        if (!t.IsAbstract && t.IsSubclassOf(typeof(Prototype)))
                        {
                            var attribute = (PrototypeAttribute)t.GetCustomAttributes(typeof(PrototypeAttribute), false)[0];
                            if (attribute.parentType.IsAssignableFrom(targetType))
                            {
                                if (list == null) list = new List<Type>();
                                list.Add(t);
                            }
                        }
                    }
                    _subPrototypeTypes.Add(targetType, list);
                }
                return list;
            }

            void OnEnable()
            {
                if (target._subPrototypes == null) target._subPrototypes = new Prototype[0];
                _list = new ReorderableList(target._subPrototypes, typeof(Prototype), true, true, true, true);

                _list.drawHeaderCallback = DrawHeaderCallback;
                _list.drawElementBackgroundCallback = DrawElementBackgroundCallback;
                _list.drawElementCallback = DrawElementCallback;
                _list.onCanAddCallback = OnCanAddCallback;
                _list.onAddDropdownCallback = OnAddDropdownCallback;
                _list.onCanRemoveCallback = OnCanRemoveCallback;
                _list.onRemoveCallback = OnRemoveCallback;
                _list.onReorderCallback = OnReorderCallback;
            }

            void DrawHeaderCallback(Rect rect)
            {
                EditorGUI.LabelField(rect, "Sub-Prototypes", EditorStyles.miniLabel);
            }

            void DrawElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                if (isActive) EditorGUI.DrawRect(rect, new Color(0.1f, 0.5f, 1f, 0.75f));
            }

            void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                var item = (Prototype)_list.list[index];

                using (var scope = ChangeCheckScope.New(item))
                {
                    EditorStyles.label.CalcMinMaxWidth(EditorGUIUtilities.TempContent(item.name), out _, out var width);
                    rect.width = width;
                    var newName = EditorGUI.DelayedTextField(rect, item.name, EditorStyles.label);
                    if (!string.IsNullOrWhiteSpace(newName) && scope.changed)
                    {
                        item.name = newName;
                        EditorUtility.SetDirty(item);
                        
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
            }

            bool OnCanAddCallback(ReorderableList list)
            {
                return GetSubPrototypeTypeList() != null;
            }

            void OnAddDropdownCallback(Rect buttonRect, ReorderableList list)
            {
                var types = GetSubPrototypeTypeList();
                GenericMenu menu = new GenericMenu();

                foreach (var t in types)
                {
                    menu.AddItem(new GUIContent(t.Name), false, OnAddCallback, t);
                }

                menu.DropDown(buttonRect);
            }

            void OnAddCallback(object data)
            {
                var instance = (Prototype)CreateInstance((Type)data);
                instance.name = $"New {data}";
                instance._parent = target;

                AssetDatabase.AddObjectToAsset(instance, target);
                Undo.RegisterCreatedObjectUndo(instance, instance.name);

                Undo.RecordObject(target, target.name);
                ArrayUtility.Add(ref target._subPrototypes, instance);

                EditorUtility.SetDirty(target);
                EditorUtility.SetDirty(instance);

                _list.list = target._subPrototypes;
                _list.index = _list.list.Count - 1;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            bool OnCanRemoveCallback(ReorderableList list)
            {
                return list.index >= 0 && RuntimeUtilities.IsNullOrEmpty(((Prototype)list.list[list.index])._subPrototypes);
            }

            void OnRemoveCallback(ReorderableList list)
            {
                Undo.RecordObject(target, target.name);

                ArrayUtility.RemoveAt(ref target._subPrototypes, list.index);

                Undo.DestroyObjectImmediate((Prototype)list.list[list.index]);

                EditorUtility.SetDirty(target);

                _list.list = target._subPrototypes;
                _list.index--;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            void OnReorderCallback(ReorderableList list)
            {
                EditorUtility.SetDirty(target);
            }

            public override void OnInspectorGUI()
            {
                EditorGUILayout.Space();

                var size = EditorStyles.boldLabel.CalcSize(EditorGUIUtilities.TempContent(target.name));
                var rect = EditorGUILayout.GetControlRect(false, size.y);
                EditorGUI.LabelField(rect, target.name, EditorStyles.boldLabel);

                rect.y = rect.center.y;
                rect.height = 2f;
                rect.xMin += size.x + 4;
                if (rect.width > 0f) EditorGUI.DrawRect(rect, EditorGUIUtilities.labelNormalColor);

                EditorGUILayout.Space();

                target.OnInspectorGUI(this);

                EditorGUILayout.Space();

                if (_list.list != target._subPrototypes) _list.list = target._subPrototypes;
                if (_list.index >= _list.list.Count) _list.index = -1;

                _list.DoLayoutList();

                if (_list.index >= 0 && _list.index < _list.count)
                {
                    var selection = (Prototype)_list.list[_list.index];
                    CreateCachedEditor(selection, null, ref selection._cachedEditor);
                    selection._cachedEditor.OnInspectorGUI();
                }
            }
        }

#endif
    }


    public class Prototype<TParent> : Prototype where TParent : Prototype
    {
        public TParent parent => (TParent)_parent;
    }
}
