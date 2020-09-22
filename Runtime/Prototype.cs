using System;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    public class Prototype : ScriptableAsset
    {
        [SerializeField, HideInInspector] int _subCount;

#if UNITY_EDITOR

        static Dictionary<Type, List<Type>> _subTypes = new Dictionary<Type, List<Type>>();
        UnityEditor.Editor _cachedEditor;

        List<Type> GetSubTypes()
        {
            var thisType = GetType();

            if (!_subTypes.TryGetValue(thisType, out var list))
            {
                var types = TypeCache.GetTypesDerivedFrom(typeof(ISubPrototype));
                foreach (var t in types)
                {
                    if (!t.IsAbstract && !t.IsGenericType)
                    {
                        var type = t;
                        while (type != typeof(Prototype))
                        {
                            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SubPrototype<>))
                            {
                                if (type.GetGenericArguments()[0] == thisType)
                                {
                                    if (list == null) list = new List<Type>();
                                    list.Add(t);
                                }
                                break;
                            }
                            type = type.BaseType;
                        }
                    }
                }

                _subTypes.Add(thisType, list);
            }

            return list;
        }


        [CustomEditor(typeof(Prototype), true)]
        [CanEditMultipleObjects]
        public class PrototypeEditor : ScriptableEditor
        {
            new Prototype target => (Prototype)base.target;

            void ShowAddMenu(Rect buttonRect)
            {
                var types = target.GetSubTypes();
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
                instance.name = ((Type)data).Name;
                ((ISubPrototype)instance).super = target;

                Undo.RegisterCreatedObjectUndo(instance, "Add Sub-Prototype");

                AssetDatabase.AddObjectToAsset(instance, target);

                Undo.RecordObject(target, "Add Sub-Prototype");
                target._subCount++;

                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();

                Selection.activeObject = instance;
            }

            void Remove()
            {
                if (target is ISubPrototype s)
                {
                    var super = s.super;
                    Undo.RecordObject(super, "Delete SubPrototype");

                    super._subCount--;

                    Undo.DestroyObjectImmediate(target);

                    EditorUtility.SetDirty(super);
                    AssetDatabase.SaveAssets();

                    Selection.activeObject = super;
                }
            }

            void OnSelfInspectorGUI()
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var rect = EditorGUILayout.GetControlRect();
                rect.width -= rect.height + 5;

                using (var scope = ChangeCheckScope.New())
                {
                    var newName = EditorGUI.DelayedTextField(rect, target.name, EditorStyles.boldLabel);
                    if (scope.changed && !string.IsNullOrWhiteSpace(newName))
                    {
                        if (target is ISubPrototype)
                        {
                            target.name = newName;

                            EditorUtility.SetDirty(target);
                            AssetDatabase.SaveAssets();
                        }
                        else
                        {
                            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(target), newName);
                        }
                    }
                }

                rect.x = rect.xMax + 5;
                rect.width = rect.height;

                if (target._subCount > 0 || !(target is ISubPrototype))
                {
                    GUI.Label(rect, target._subCount.ToString(), EditorStyles.centeredGreyMiniLabel);
                }
                else if (GUI.Button(rect, "X", EditorStyles.centeredGreyMiniLabel))
                {
                    Remove();
                }

                EditorGUILayout.EndVertical();

                if (target) target.OnInspectorGUI(this);
            }

            public override void OnInspectorGUI()
            {
                if (target is ISubPrototype s)
                {
                    using (var supers = PoolSingleton<Stack<Prototype>>.instance.GetTemp())
                    {
                        supers.item.Clear();

                        while (s.super)
                        {
                            supers.item.Push(s.super);

                            if (s.super is ISubPrototype i) s = i;
                            else break;
                        }

                        foreach (var i in supers.item)
                        {
                            CreateCachedEditor(i, null, ref i._cachedEditor);
                            ((PrototypeEditor)i._cachedEditor).OnSelfInspectorGUI();
                        }

                        supers.item.Clear();
                    }
                }

                OnSelfInspectorGUI();

                if (target.GetSubTypes() != null)
                {
                    var rect = EditorGUILayout.GetControlRect(true);
                    rect.xMin += EditorGUIUtility.labelWidth;

                    if (GUI.Button(rect, "Add Sub-Prototype", EditorStyles.miniButton))
                    {
                        ShowAddMenu(rect);
                    }
                }
            }
        }

#endif
    }


    internal interface ISubPrototype
    {
        Prototype super { get; set; }
    }


    public class SubPrototype<SuperPrototype> : Prototype, ISubPrototype where SuperPrototype : Prototype
    {
        [SerializeField, HideInInspector] SuperPrototype _super;

        public SuperPrototype super => _super;

        Prototype ISubPrototype.super { get => _super; set => _super = (SuperPrototype)value; }
    }
}
