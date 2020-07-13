using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityExtensions
{
    public class TestEntity : Entity<TestEntity>
    {


#if UNITY_EDITOR

        [ContextMenu("Find Parent In Hierarchy")]
        void FindParentInHierarchy()
        {
            FindParentInHierarchyWithUndo();
        }

        [ContextMenu("Find Children In Hierarchy")]
        void FindChildrenInHierarchy()
        {
            FindChildrenInHierarchyWithUndo();
        }

        [CustomEditor(typeof(TestEntity), true)]
        [CanEditMultipleObjects]
        protected class TestEntityEditor : Editor
        {
        }

#endif
    }
}
