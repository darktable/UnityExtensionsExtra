using System;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// 一维混合事件设置
    /// </summary>
    [Serializable]
    public struct BlendEventSettings1D
    {
        [Min(0)]
        public float startDelay;

        [Min(MathUtilities.OneMillionth)]
        public float duration;

        public float value;

        [Tooltip("Input and output are normalized.")]
        public AnimationCurve attenuation;
    }


    /// <summary>
    /// 二维混合事件设置
    /// </summary>
    [Serializable]
    public struct BlendEventSettings2D
    {
        [Min(0)]
        public float startDelay;

        [Min(MathUtilities.OneMillionth)]
        public float duration;

        public Vector2 value;

        [Tooltip("Input and output are normalized.")]
        public AnimationCurve attenuation;
    }

} // namespace UnityExtensions