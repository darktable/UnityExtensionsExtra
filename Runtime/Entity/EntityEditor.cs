#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityExtensions.Editor;

namespace UnityExtensions
{
    public partial class Entity<T> : HierarchialComponent<T> where T : Entity<T>
    {
        protected override View CreateView() => new EntityView((T)this);

        protected class EntityView : View
        {
            public EntityView(T entity) : base(entity) { }

            protected override void RowGUI(RowGUIArgs args)
            {
                var rect = args.rowRect;
                rect.xMin += GetContentIndent(args.item);
                var entity = ((ViewItem)args.item).data;

                using (var scope = ChangeCheckScope.New(entity))
                {
                    var toggleRect = rect;
                    toggleRect.width = toggleRect.height;

                    bool result = EditorGUI.Toggle(toggleRect, entity._localActive);
                    if (scope.changed) entity._localActive = result;

                    rect.xMin = toggleRect.xMax - 2;
                    GUI.Label(rect, args.label, (args.selected && args.focused) ? EditorStyles.whiteLabel : EditorStyles.label);
                }
            }

            protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
            {
                float indent = GetContentIndent(item) + rowRect.height;
                rowRect.xMin += indent;
                rowRect.yMax -= 1;
                return rowRect;
            }
        }
    }
}

#endif