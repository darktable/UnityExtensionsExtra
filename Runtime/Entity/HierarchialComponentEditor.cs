#if UNITY_EDITOR
#define SHOW_DEBUG_INFO

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityExtensions.Editor;

// paste，delete, withundo, instantiate, prefab
namespace UnityExtensions
{
    /// <summary>
    /// HierarchialComponent
    /// </summary>
    public partial class HierarchialComponent<T> : ScriptableComponent where T : HierarchialComponent<T>
    {
        T _parentTracked;
        T _nextTracked;
        T _previousTracked;
        T _firstChildTracked;

        bool _showView = false;

        ViewItem _viewItem;
        ViewItem viewItem => _viewItem ?? (_viewItem = new ViewItem((T)this));

        TreeViewState _viewState;
        TreeViewState viewState => _viewState ?? (_viewState = new TreeViewState());

        void TrackData()
        {
            _parentTracked = _parent;
            _nextTracked = _nextSibling;
            _previousTracked = _prevSibling;
            _firstChildTracked = _firstChild;
        }

        void SetChildrenItemsDirty() => _viewItem?.SetChildrenDirty();

        protected virtual View CreateView() => new View((T)this);

        protected virtual void Reset()
        {
            _parent = _parentTracked;
            _nextSibling = _nextTracked;
            _prevSibling = _previousTracked;
            _firstChild = _firstChildTracked;
        }

        protected virtual void OnValidate()
        {
            if (_parent != _parentTracked || _nextSibling != _nextTracked || _prevSibling != _previousTracked || _firstChild != _firstChildTracked)
            {
                if (_parentTracked) _parentTracked.SetChildrenItemsDirty();
                if (_parent) _parent.SetChildrenItemsDirty();

                TrackData();
            }
        }

        protected void FindParentInHierarchyWithUndo()
        {
            if (!parent && transform.parent)
            {
                var newParent = transform.parent.GetComponentInParent<T>();
                if (newParent && !newParent.IsDescendantOf((T)this))
                {
                    AsLastChildUndoRecord(newParent, "FindParentInHierarchy");
                    AsLastChild(newParent);
                }
            }
        }

        protected void FindChildrenInHierarchyWithUndo()
        {
            var objects = new List<T>();
            GetComponentsInChildren(true, objects);

            for (int i = objects.Count - 1; i >= 0; i--)
            {
                var obj = objects[i];
                if (obj.parent || this.IsDescendantOf(obj))
                {
                    objects.RemoveAt(i);
                }
                else
                {
                    Undo.RecordObject(obj, "FindChildrenInHierarchy");
                }
            }

            if (objects.Count > 0)
            {
                objects[0].AsLastChildUndoRecord((T)this, "FindChildrenInHierarchy");

                foreach (var obj in objects)
                {
                    obj.AsLastChild((T)this);
                }
            }
        }

        protected void DetachParentUndoRecord(string name)
        {
            if (_parent)
            {
                if (_parent._firstChild == this)
                {
                    Undo.RecordObject(_parent, name);
                }
                else
                {
                    Undo.RecordObject(_prevSibling, name);
                }

                if (_nextSibling)
                {
                    Undo.RecordObject(_nextSibling, name);
                }

                Undo.RecordObject(this, name);
            }
        }

        protected void DetachChildrenUndoRecord(string name)
        {
            T child = _firstChild;
            while (child)
            {
                Undo.RecordObject(child, name);
                child = child._nextSibling;
            }

            if (_firstChild) Undo.RecordObject(this, name);
        }

        protected void AsFirstChildUndoRecord(T newParent, string name)
        {
            ValidateNewParent(newParent);
            if (newParent._firstChild == this) return;
            DetachParentUndoRecord(name);

            if (newParent._firstChild)
            {
                Undo.RecordObject(newParent._firstChild, name);
            }

            Undo.RecordObject(newParent, name);
            Undo.RecordObject(this, name);
        }

        protected void AsLastChildUndoRecord(T newParent, string name)
        {
            ValidateNewParent(newParent);
            if (newParent.lastChild == this) return;
            DetachParentUndoRecord(name);

            if (newParent._firstChild)
            {
                Undo.RecordObject(newParent._firstChild._prevSibling, name);
                Undo.RecordObject(newParent._firstChild, name);
            }
            else
            {
                Undo.RecordObject(newParent, name);
            }

            Undo.RecordObject(this, name);
        }

        protected void AsSiblingBeforeUndoRecord(T newNext, string name)
        {
            ValidateNewSibling(newNext);
            if (this == newNext || this._nextSibling == newNext) return;

            ValidateNewParent(newNext._parent);
            DetachParentUndoRecord(name);

            if (newNext._parent._firstChild == newNext)
            {
                Undo.RecordObject(newNext._parent, name);
            }
            else
            {
                Undo.RecordObject(newNext._prevSibling, name);
            }

            Undo.RecordObject(newNext, name);
            Undo.RecordObject(this, name);
        }

        protected void AsSiblingAfterUndoRecord(T newPrevious, string name)
        {
            ValidateNewSibling(newPrevious);
            if (this == newPrevious || this.previousSibling == newPrevious) return;

            ValidateNewParent(newPrevious._parent);
            DetachParentUndoRecord(name);

            if (newPrevious._nextSibling)
            {
                Undo.RecordObject(newPrevious._nextSibling, name);
            }

            Undo.RecordObject(newPrevious, name);
            Undo.RecordObject(this, name);
        }


        public class ViewItem : TreeViewItem
        {
            public T data { get; private set; }
            bool _childrenDirty = true;

            public void SetChildrenDirty() => _childrenDirty = true;

            public ViewItem(T data) : base(data.GetInstanceID()) => this.data = data;

            public override string displayName { get => data.gameObject.name; set => data.gameObject.name = value; }

            public override int depth
            {
                get => data.depth - View.currentDepth - 1;
                set => throw new System.NotImplementedException();
            }

            public override bool hasChildren => data.hasChildren;

            public override List<TreeViewItem> children
            {
                get
                {
                    if (_childrenDirty)
                    {
                        _childrenDirty = false;

                        if (base.children == null) base.children = new List<TreeViewItem>();
                        base.children.Clear();

                        foreach (var child in data.children) base.children.Add(child.viewItem);
                    }
                    return base.children;
                }
                set => throw new System.NotImplementedException();
            }

            public override TreeViewItem parent
            {
                get => data.parent ? data.parent.viewItem : null;
                set => throw new System.NotImplementedException();
            }
        }


        protected class View : TreeView, System.IDisposable
        {
            T _root;
            bool _dirty;
            internal static int currentDepth;

            static List<View> _instances = new List<View>();

            static View() => Undo.undoRedoPerformed += SetDirty;

            public static void SetDirty()
            {
                foreach (var view in _instances) view._dirty = true;
            }

            public View(T root) : base(root.viewState)
            {
                _root = root;
                _dirty = true;
                showBorder = true;
                rowHeight = EditorGUIUtility.singleLineHeight;

                _instances.Add(this);
            }

            public void Dispose()
            {
                _instances.Remove(this);
            }

            protected override TreeViewItem BuildRoot()
            {
                return _root.viewItem;
            }

            public void GUILayout()
            {
                currentDepth = _root.depth;

                if (_dirty)
                {
                    _dirty = false;
                    Reload();
                }

                var rect = EditorGUILayout.GetControlRect(false, totalHeight + rowHeight - 1);
                OnGUI(rect);

                if (_root.isLeaf)
                {
                    EditorGUI.LabelField(rect, "Drag a child here.", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    rect.yMax -= 1;
                    rect.xMax -= 1;
                    rect.xMin += 1;
                    rect.yMin = rect.yMax - rowHeight + 1;

                    var color = EditorGUIUtilities.labelNormalColor;
                    color.a = 0.15f;
                    EditorGUI.DrawRect(rect, color);

                    using (DisabledScope.New(!HasSelection() || !HasFocus()))
                    {
                        rect.xMax += 1;
                        rect.xMin = rect.xMax - rowHeight * 1.5f;
                        if (GUI.Button(rect, "-", EditorStyles.miniButtonMid))
                        {
                            RemoveSelection();
                        }
                    }
                }
            }

            protected override void SingleClickedItem(int id)
            {
                EditorGUIUtility.PingObject(id);
            }

            protected override bool CanRename(TreeViewItem item)
            {
                return true;
            }

            protected override void RenameEnded(RenameEndedArgs args)
            {
                if (args.acceptedRename)
                {
                    var gameObject = ((T)EditorUtility.InstanceIDToObject(args.itemID)).gameObject;
                    Undo.RecordObject(gameObject, "Rename");
                    gameObject.name = args.newName;
                }
            }

            protected override bool CanStartDrag(CanStartDragArgs args)
            {
                return true;
            }

            protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
            {
                var ids = args.draggedItemIDs;
                var targets = new T[ids.Count];

                for (int i = 0; i < ids.Count; i++)
                {
                    targets[i] = (T)EditorUtility.InstanceIDToObject(ids[i]);
                }

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = targets;
                string title = targets.Length == 1 ? targets[0].name : "< Multiple >";
                DragAndDrop.StartDrag(title);
            }

            protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
            {
                if (!RuntimeUtilities.IsNullOrEmpty(DragAndDrop.objectReferences))
                {
                    if (args.performDrop)
                    {
                        var newParent = args.parentItem == null ? _root : ((ViewItem)args.parentItem).data;
                        int index = args.dragAndDropPosition == DragAndDropPosition.BetweenItems ? args.insertAtIndex : -1;
                        if (index >= newParent.childCount) index = -1;
                        OnDropObjects(DragAndDrop.objectReferences, newParent, index);
                        DragAndDrop.AcceptDrag();
                    }

                    switch (args.dragAndDropPosition)
                    {
                        case DragAndDropPosition.UponItem:
                            return DragAndDropVisualMode.Link;

                        case DragAndDropPosition.BetweenItems:
                        case DragAndDropPosition.OutsideItems:
                            return DragAndDropVisualMode.Move;
                    }
                }
                return DragAndDropVisualMode.Generic;
            }

            void RemoveSelection()
            {
                List<T> items = new List<T>();
                foreach (var id in GetSelection())
                {
                    items.Add((T)EditorUtility.InstanceIDToObject(id));
                }

                for (int i = items.Count - 1; i >= 0; i--)
                {
                    var current = items[i];
                    foreach (var item in items)
                    {
                        if (current != item && current.IsDescendantOf(item))
                        {
                            items.RemoveAt(i);
                            break;
                        }
                    }
                }

                foreach (var item in items)
                {
                    item.DetachParent();
                }
            }

            void OnDropObjects(Object[] objects, T newParent, int insertIndex)
            {
                List<T> items = new List<T>();

                foreach (var obj in objects)
                {
                    T item = null;

                    if (obj is GameObject go)
                    {
                        if (go.scene == _root.gameObject.scene)
                        {
                            go.TryGetComponent<T>(out item);
                        }
                    }
                    else if (obj is T cmpt)
                    {
                        if (cmpt.gameObject.scene == _root.gameObject.scene)
                        {
                            item = cmpt;
                        }
                    }

                    if (item && !newParent.IsDescendantOf(item))
                    {
                        items.Add(item);
                    }
                }

                if (items.Count > 0)
                {
                    if (insertIndex < 0)
                    {
                        foreach (var item in items)
                        {
                            item.AsLastChild(newParent);
                        }
                    }
                    else
                    {
                        var newNext = ((ViewItem)newParent.viewItem.children[insertIndex]).data;
                        foreach (var item in items)
                        {
                            item.AsSiblingBefore(newNext);
                        }
                    }
                }
            }
        }


        //[CustomEditor(typeof(T), true)]
        //[CanEditMultipleObjects]
        protected class Editor : BaseEditor<T>
        {
            View _view;

            protected virtual void OnDisable()
            {
                _view?.Dispose();
                _view = null;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                EditorGUILayout.Space();

                var newParent = EditorGUILayout.ObjectField("Parent", target.parent, typeof(T), true);
                if (newParent != target.parent)
                {
                    // UNdo
                    target.parent = (T)newParent;
                }

#if SHOW_DEBUG_INFO
                using (DisabledScope.New(true))
                {
                    EditorGUILayout.ObjectField("Prev Sibling", target.previousSibling, typeof(T), true);
                    EditorGUILayout.ObjectField("Next Sibling", target.nextSibling, typeof(T), true);
                    EditorGUILayout.ObjectField("First Child", target.firstChild, typeof(T), true);
                    EditorGUILayout.ObjectField("Last Child", target.lastChild, typeof(T), true);
                }
#endif

                target._showView = EditorGUILayout.Foldout(target._showView, "Children", true);
                if (target._showView)
                {
                    if (serializedObject.isEditingMultipleObjects)
                    {
                        EditorGUILayout.HelpBox("Multi-object editing not supported.", MessageType.None);
                    }
                    else
                    {
                        if (_view == null) _view = target.CreateView();
                        _view.GUILayout();
                    }
                }
            }
        }

    } // class HierarchialComponent<T>

} // namespace UnityExtensions

#endif
