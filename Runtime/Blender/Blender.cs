using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class Blender<T> : ScriptableComponent
    {
        [SerializeField]
        T _baseChannelValue;

        List<Channel> _channels;
        List<Event> _events;

        bool _channelsChanged = true;
        T _channelsOutput;

        bool _hasEventsOutput = false;
        T _eventsOutput;

        /// <summary>
        /// 混合通道
        /// </summary>
        public class Channel : IDisposable
        {
            T _value;
            Blender<T> _blender;

            /// <summary>
            /// 通道值
            /// </summary>
            public T value
            {
                get => _value;
                set
                {
                    if (!_blender.Equals(_value, value))
                    {
                        _value = value;
                        _blender._channelsChanged = true;
                    }
                }
            }

            internal Channel(T value, Blender<T> blender)
            {
                _value = value;
                _blender = blender;

                _blender._channels.Add(this);
                _blender._channelsChanged = true;
            }

            public void Dispose()
            {
                _blender._channels.Remove(this);
                _blender._channelsChanged = true;
                _blender = null;
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

            public float currentScaleFactor => Mathf.Max(attenuation.Evaluate(time01), 0f);
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
        /// 创建通道 
        /// 当通道不再使用时，必须使用 Dispose 移除
        /// </summary>
        public Channel CreateChannel(T value)
        {
            if (_channels == null) _channels = new List<Channel>(4);
            var channel = new Channel(value, this);
            return channel;
        }

        /// <summary>
        /// 创建事件
        /// 事件在超过作用时间后自动移除
        /// </summary>
        /// <param name="attenuation"> 曲线的 Y 值被截断为非负数 </param>
        public void CreateEvent(float startDelay, float duration, T value, AnimationCurve attenuation)
        {
            if (_events == null) _events = new List<Event>(4);
            _events.Add(new Event
            {
                time01 = 0,
                value = value,
                startDelay = startDelay,
                duration = duration,
                attenuation = attenuation,
            });
        }

        /// <summary>
        /// 如果使用了事件，需要以一定频率调用此方法
        /// </summary>
        public void UpdateEvents(float deltaTime)
        {
            _hasEventsOutput = false;
            if (!RuntimeUtilities.IsNullOrEmpty(_events))
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

                        if (_hasEventsOutput)
                        {
                            _eventsOutput = Blend(_eventsOutput, Scale(e.value, e.currentScaleFactor));
                        }
                        else
                        {
                            _hasEventsOutput = true;
                            _eventsOutput = Scale(e.value, e.currentScaleFactor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 创建的通道总数（不包含基本通道）
        /// </summary>
        public int createdChannelCount => _channels == null ? 0 : _channels.Count;

        /// <summary>
        /// 基本通道值
        /// </summary>
        public T baseChannelValue
        {
            get => _baseChannelValue;
            set
            {
                if (!Equals(_baseChannelValue, value))
                {
                    _baseChannelValue = value;
                    _channelsChanged = true;
                }
            }
        }

        /// <summary>
        /// 所有通道混合后的输出值
        /// </summary>
        public T channelsOutputValue
        {
            get
            {
                if (_channelsChanged)
                {
                    _channelsOutput = _baseChannelValue;
                    if (!RuntimeUtilities.IsNullOrEmpty(_channels))
                    {
                        for (int i = 0; i < _channels.Count; i++)
                            _channelsOutput = Blend(_channelsOutput, _channels[i].value);
                    }
                    _channelsChanged = false;
                }
                return _channelsOutput;
            }
        }

        /// <summary>
        /// 最终输出值
        /// </summary>
        public T outputValue => _hasEventsOutput ? Blend(channelsOutputValue, _eventsOutput) : channelsOutputValue;

#if UNITY_EDITOR
        protected virtual void Reset()
        {
            _channels = null;
            _events = null;

            _channelsChanged = true;
            _hasEventsOutput = false;
        }

        void OnValidate()
        {
            _channelsChanged = true;
        }
#endif

    } // class Blender<T>


    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class Blender1D : Blender<float>
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
        public void CreateEvent(BlendingEventPreset1D preset)
        {
            CreateEvent(preset.startDelay, preset.duration, preset.value, preset.attenuation);
        }

    } // class Blender1D


    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class Blender2D : Blender<Vector2>
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
        public void CreateEvent(BlendingEventPreset2D preset)
        {
            CreateEvent(preset.startDelay, preset.duration, preset.value, preset.attenuation);
        }

    } // class Blender2D


    /// <summary>
    /// 加法混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class AdditiveBlender1D : Blender1D
    {
        public sealed override float Blend(float a, float b)
        {
            return a + b;
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            baseChannelValue = 0;
        }
#endif
    }


    /// <summary>
    /// 乘法混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class MultiplyBlender1D : Blender1D
    {
        public sealed override float Blend(float a, float b)
        {
            return a * b;
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            baseChannelValue = 1;
        }
#endif
    }


    /// <summary>
    /// 最大值混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class MaximumBlender1D : Blender1D
    {
        public sealed override float Blend(float a, float b)
        {
            return Mathf.Max(a, b);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            baseChannelValue = 0;
        }
#endif
    }


    /// <summary>
    /// 最小值混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class MinimumBlender1D : Blender1D
    {
        public sealed override float Blend(float a, float b)
        {
            return Mathf.Min(a, b);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            baseChannelValue = 1;
        }
#endif
    }


    /// <summary>
    /// 加法混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class AdditiveBlender2D : Blender2D
    {
        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return a + b;
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            baseChannelValue = new Vector2(0, 0);
        }
#endif
    }


    /// <summary>
    /// 乘法混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class MultiplyBlender2D : Blender2D
    {
        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Scale(a, b);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            baseChannelValue = new Vector2(1, 1);
        }
#endif
    }


    /// <summary>
    /// 最大值混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class MaximumBlender2D : Blender2D
    {
        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Max(a, b);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            baseChannelValue = new Vector2(0, 0);
        }
#endif
    }


    /// <summary>
    /// 最小值混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class MinimumBlender2D : Blender2D
    {
        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Min(a, b);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            baseChannelValue = new Vector2(1, 1);
        }
#endif
    }

} // namespace UnityExtensions