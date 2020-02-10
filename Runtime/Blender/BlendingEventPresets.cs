using System;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// 一维混合事件预设
    /// </summary>
    [Serializable]
    public struct FloatBlendingEventPreset
    {
        [Min(0)]
        public float startDelay;

        [Min(MathUtilities.OneMillionth)]
        public float duration;

        public float value;

        [Tooltip("Input & output are normalized.")]
        public AnimationCurve attenuation;
    }


    /// <summary>
    /// 二维混合事件预设
    /// </summary>
    [Serializable]
    public struct Vector2BlendingEventPreset
    {
        [Min(0)]
        public float startDelay;

        [Min(MathUtilities.OneMillionth)]
        public float duration;

        public Vector2 value;

        [Tooltip("Input & output are normalized.")]
        public AnimationCurve attenuation;
    }

} // namespace UnityExtensions