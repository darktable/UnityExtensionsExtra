using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// 混合器
    /// </summary>
    public interface IBlender<T>
    {
        bool Equals(T x, T y);
        T Blend(T x, T y);
        T Scale(T x, float y);
    }


    /// <summary>
    /// 混合控制器
    /// 用于控制多种输入单一输出的效果，比如控制器震动、屏幕特效等
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event
    /// </summary>
    public abstract class BaseBlender<T> : ScriptableComponent, IBlender<T>
    {
        [SerializeField, GetSet("baseChannelValue")]
        T _baseChannelValue;

        [SerializeField, GetSet("outputScale")]
        float _outputScale = 1f;

        List<Channel> _channels = new List<Channel>(4);
        bool _anyChannelChanged = true;
        T _channelsOutput;

        List<Event> _events = new List<Event>(4);

        bool _outputChanged;
        T _finalOutput;


        /// <summary>
        /// 混合通道
        /// </summary>
        public class Channel : IDisposable
        {
            T _value;
            BaseBlender<T> _controller;

            /// <summary>
            /// 通道输出值
            /// </summary>
            public T value
            {
                get { return _value; }
                set
                {
                    if (_controller != null && !_controller.Equals(_value, value))
                    {
                        _value = value;
                        _controller._anyChannelChanged = true;
                    }
                }
            }

            public Channel(T value, BaseBlender<T> controller)
            {
                _value = value;
                _controller = controller;
                _controller._anyChannelChanged = true;
            }

            public void Dispose()
            {
                if (_controller != null)
                {
                    _controller._channels.Remove(this);
                    _controller._anyChannelChanged = true;
                    _controller = null;
                }
            }
        }


        // 混合事件
        private struct Event
        {
            public float time01;
            public T value;
            public float startDelay;
            public float duration;
            public AnimationCurve attenuation;

            public float scale
            {
                get { return Mathf.Max(attenuation.Evaluate(time01), 0f); }
            }
        }


        /// <summary>
        /// 判断两个值是否相等
        /// </summary>
        public abstract bool Equals(T a, T b);


        /// <summary>
        /// 混合值
        /// </summary>
        public abstract T Blend(T a, T b);


        /// <summary>
        /// 缩放值
        /// </summary>
        public abstract T Scale(T a, float b);


        /// <summary>
        /// 基本通道值
        /// </summary>
        public T baseChannelValue
        {
            get { return _baseChannelValue; }
            set
            {
                if (!Equals(_baseChannelValue, value))
                {
                    _baseChannelValue = value;
                    _anyChannelChanged = true;
                }
            }
        }


        /// <summary>
        /// 输出缩放
        /// </summary>
        public float outputScale
        {
            get { return _outputScale; }
            set
            {
                value = Mathf.Max(value, 0f);
                if (_outputScale != value)
                {
                    _outputScale = value;
                    _outputChanged = true;
                }
            }
        }


        public T lastOutput
        {
            get { return _finalOutput; }
        }


        /// <summary>
        /// 创建通道 
        /// 当通道不再使用时，必须使用 Dispose 移除
        /// </summary>
        public Channel CreateChannel(T value)
        {
            var channel = new Channel(value, this);
            _channels.Add(channel);
            return channel;
        }


        /// <summary>
        /// 创建事件
        /// 事件在超过作用时间后自动移除
        /// </summary>
        public void CreateEvent(float startDelay, float duration, T value, AnimationCurve attenuation)
        {
            _events.Add(new Event
            {
                time01 = 0,
                value = value,
                startDelay = startDelay,
                duration = duration,
                attenuation = attenuation,
            });
        }


        private void UpdateEvents(float deltaTime)
        {
            Event e;
            float dt;

            for (int i = 0; i < _events.Count; i++)
            {
                e = _events[i];
                dt = deltaTime;

                if (e.startDelay > 0f)
                {
                    e.startDelay -= deltaTime;
                    if (e.startDelay <= 0f)
                    {
                        dt = -e.startDelay;
                        e.startDelay = 0f;
                    }
                    _events[i] = e;
                }

                if (e.startDelay == 0f)
                {
                    e.time01 += dt / e.duration;

                    if (e.time01 >= 1f)
                    {
                        _events.RemoveAt(i--);
                        e.time01 = 1f;
                    }
                    else _events[i] = e;

                    _finalOutput = Blend(_finalOutput, Scale(e.value, e.scale));
                }
            }
        }


        /// <summary>
        /// 在必要的时候执行更新（比如子类实现时以一定频率调用）
        /// </summary>
        /// <returns> 如果输出发生变化返回 true, 否则返回 false </returns>
        public bool Refresh(float deltaTime)
        {
            if (_anyChannelChanged)
            {
                _anyChannelChanged = false;
                _channelsOutput = _baseChannelValue;

                for (int i = 0; i < _channels.Count; i++)
                {
                    _channelsOutput = Blend(_channelsOutput, _channels[i].value);
                }

                _outputChanged = true;
            }

            if (_outputChanged || _events.Count > 0)
            {
                _outputChanged = false;
                _finalOutput = _channelsOutput;

                if (_events.Count > 0)
                {
                    UpdateEvents(deltaTime);
                
                    // 即使事件完全移除仍需再刷新一次
                    _outputChanged = true;
                }

                _finalOutput = Scale(_finalOutput, _outputScale);
                return true;
            }

            return false;
        }


#if UNITY_EDITOR

        void Reset()
        {
            _channels.Clear();
            _events.Clear();
            _anyChannelChanged = true;
        }


        void OnValidate()
        {
            _anyChannelChanged = true;
        }


        protected class BaseBlenderEditor : BaseEditor<BaseBlender<T>>
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                EditorGUILayout.Space();

                using (DisabledScope.New(true))
                {
                    using (HorizontalLayoutScope.New())
                    {
                        EditorGUILayout.HelpBox("Channels: " + (target._channels.Count + 1), MessageType.None, true);
                        EditorGUILayout.HelpBox("Events: " + target._events.Count, MessageType.None, true);
                    }
                }
            }
        }

#endif

    } // class BaseBlender<T>


    /// <summary>
    /// 混合效果控制器
    /// 用于控制多种输入单一输出的效果，比如控制器震动、屏幕特效等
    /// 每一种控制来源选择使用 Channel 或 Event
    /// </summary>
    public abstract class Blender1D : BaseBlender<float>
    {
        public sealed override float Scale(float a, float b)
        {
            return a * b;
        }


        public sealed override bool Equals(float a, float b)
        {
            return a == b;
        }


        /// <summary>
        /// 创建事件
        /// 事件在超过作用时间后自动移除
        /// </summary>
        public void CreateEvent(BlendEventSettings1D settings)
        {
            CreateEvent(settings.startDelay, settings.duration, settings.value, settings.attenuation);
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(Blender1D), true)]
        class Blender1DEditor : BaseBlenderEditor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                using (DisabledScope.New(true))
                {
                    EditorGUILayout.FloatField("Last Output", target.lastOutput);
                }
            }
        }

#endif

    } // Blender1D


    /// <summary>
    /// 混合效果控制器
    /// 用于控制多种输入单一输出的效果，比如控制器震动、屏幕特效等
    /// 每一种控制来源选择使用 Channel 或 Event
    /// </summary>
    public abstract class Blender2D : BaseBlender<Vector2>
    {
        public sealed override Vector2 Scale(Vector2 a, float b)
        {
            return a * b;
        }


        public sealed override bool Equals(Vector2 a, Vector2 b)
        {
            return a.x == b.x && a.y == b.y;
        }


        /// <summary>
        /// 创建事件
        /// 事件在超过作用时间后自动移除
        /// </summary>
        public void CreateEvent(BlendEventSettings2D settings)
        {
            CreateEvent(settings.startDelay, settings.duration, settings.value, settings.attenuation);
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(Blender2D), true)]
        class Blender2DEditor : BaseBlenderEditor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                using (DisabledScope.New(true))
                {
                    EditorGUILayout.Vector2Field("Last Output", target.lastOutput);
                }
            }
        }

#endif

    } // Blender2D


    /// <summary>
    /// 加法混合效果控制器
    /// 用于控制多种输入单一输出的效果，比如控制器震动、屏幕特效等
    /// 每一种控制来源选择使用 Channel 或 Event
    /// </summary>
    public class AdditiveBlender1D : Blender1D
    {
        public sealed override float Blend(float a, float b)
        {
            return a + b;
        }
    }


    /// <summary>
    /// 乘法混合效果控制器
    /// 用于控制多种输入单一输出的效果，比如控制器震动、屏幕特效等
    /// 每一种控制来源选择使用 Channel 或 Event
    /// </summary>
    public class MultiplyBlender1D : Blender1D
    {
        public sealed override float Blend(float a, float b)
        {
            return a * b;
        }
    }


    /// <summary>
    /// Max 混合效果控制器
    /// 用于控制多种输入单一输出的效果，比如控制器震动、屏幕特效等
    /// 每一种控制来源选择使用 Channel 或 Event
    /// </summary>
    public class MaximumBlender1D : Blender1D
    {
        public sealed override float Blend(float a, float b)
        {
            return Mathf.Max(a, b);
        }
    }


    /// <summary>
    /// 加法混合效果控制器
    /// 用于控制多种输入单一输出的效果，比如控制器震动、屏幕特效等
    /// 每一种控制来源选择使用 Channel 或 Event
    /// </summary>
    public class AdditiveBlender2D : Blender2D
    {
        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return a + b;
        }
    }


    /// <summary>
    /// 乘法混合效果控制器
    /// 用于控制多种输入单一输出的效果，比如控制器震动、屏幕特效等
    /// 每一种控制来源选择使用 Channel 或 Event
    /// </summary>
    public class MultiplyBlender2D : Blender2D
    {
        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Scale(a, b);
        }
    }


    /// <summary>
    /// Max 混合效果控制器
    /// 用于控制多种输入单一输出的效果，比如控制器震动、屏幕特效等
    /// 每一种控制来源选择使用 Channel 或 Event
    /// </summary>
    public class MaximumBlender2D : Blender2D
    {
        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Max(a, b);
        }
    }

} // namespace UnityExtensions