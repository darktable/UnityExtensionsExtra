using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class Blender<T>
    {
        List<Channel> _channels;
        List<Event> _events;

        bool _channelsChanged;
        T _channelsOutput;

        bool _hasEventsOutput;
        T _eventsOutput;

        /// <summary>
        /// 任意通道的新增、删除、修改都会触发
        /// </summary>
        public event Action onChannelsChange;

        void SetChannelsChanged()
        {
            _channelsChanged = true;
            onChannelsChange?.Invoke();
        }

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
                        _blender.SetChannelsChanged();
                    }
                }
            }

            internal Channel(T value, Blender<T> blender)
            {
                _value = value;
                _blender = blender;

                _blender._channels.Add(this);
                _blender.SetChannelsChanged();
            }

            public void Dispose()
            {
                _blender._channels.Remove(this);
                _blender.SetChannelsChanged();
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

        public Blender() => _channelsOutput = defaultValue;

        public abstract T defaultValue { get; }

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
        /// 通道总数
        /// </summary>
        public int channelCount => _channels == null ? 0 : _channels.Count;

        /// <summary>
        /// 所有通道混合后的输出值
        /// </summary>
        public T channelsOutputValue
        {
            get
            {
                if (_channelsChanged)
                {
                    _channelsOutput = defaultValue;
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

    } // class Blender<T>


    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class BoolBlender : Blender<bool>
    {
        public sealed override bool Scale(bool a, float b)
        {
            return b > 0 && a;
        }

        public sealed override bool Equals(bool a, bool b)
        {
            return a == b;
        }

    } // class BoolBlender


    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class FloatBlender : Blender<float>
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
        public void CreateEvent(FloatBlendingEventPreset preset)
        {
            CreateEvent(preset.startDelay, preset.duration, preset.value, preset.attenuation);
        }

    } // class FloatBlender


    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class Vector2Blender : Blender<Vector2>
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
        public void CreateEvent(Vector2BlendingEventPreset preset)
        {
            CreateEvent(preset.startDelay, preset.duration, preset.value, preset.attenuation);
        }

    } // class Vector2Blender


    /// <summary>
    /// 与混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class BoolAndBlender : BoolBlender
    {
        public override bool defaultValue => true;

        public sealed override bool Blend(bool a, bool b)
        {
            return a && b;
        }
    }


    /// <summary>
    /// 或混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class BoolOrBlender : BoolBlender
    {
        public override bool defaultValue => false;

        public sealed override bool Blend(bool a, bool b)
        {
            return a || b;
        }
    }


    /// <summary>
    /// 加法混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class FloatAdditiveBlender : FloatBlender
    {
        public override float defaultValue => 0f;

        public float channelsAverageValue => channelsOutputValue / channelCount;

        public sealed override float Blend(float a, float b)
        {
            return a + b;
        }
    }


    /// <summary>
    /// 乘法混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class FloatMultiplyBlender : FloatBlender
    {
        public override float defaultValue => 1f;

        public sealed override float Blend(float a, float b)
        {
            return a * b;
        }
    }


    /// <summary>
    /// 最大值混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class FloatMaximumBlender : FloatBlender
    {
        public override float defaultValue => float.MinValue;

        public sealed override float Blend(float a, float b)
        {
            return Mathf.Max(a, b);
        }
    }


    /// <summary>
    /// 最小值混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class FloatMinimumBlender : FloatBlender
    {
        public override float defaultValue => float.MaxValue;

        public sealed override float Blend(float a, float b)
        {
            return Mathf.Min(a, b);
        }
    }


    /// <summary>
    /// 加法混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class Vector2AdditiveBlender : Vector2Blender
    {
        public override Vector2 defaultValue => new Vector2();

        public Vector2 channelsAverageValue => channelsOutputValue / channelCount;

        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return a + b;
        }
    }


    /// <summary>
    /// 乘法混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class Vector2MultiplyBlender : Vector2Blender
    {
        public override Vector2 defaultValue => new Vector2(1f, 1f);

        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Scale(a, b);
        }
    }


    /// <summary>
    /// 最大值混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class Vector2MaximumBlender : Vector2Blender
    {
        public override Vector2 defaultValue => new Vector2(float.MinValue, float.MinValue);

        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Max(a, b);
        }
    }


    /// <summary>
    /// 最小值混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class Vector2MinimumBlender : Vector2Blender
    {
        public override Vector2 defaultValue => new Vector2(float.MaxValue, float.MaxValue);

        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Min(a, b);
        }
    }

} // namespace UnityExtensions