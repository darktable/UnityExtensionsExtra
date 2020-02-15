using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// </summary>
    public abstract class Blender<T> : BaseBlendingChannel<T>
    {
        List<BaseBlendingChannel<T>> _channels;
        T _baseValue;
        bool _channelsChanged;
        T _channelsValue;

        /// <summary>
        /// 任意通道的新增、删除、修改都会触发
        /// </summary>
        public event Action onChannelsChange;

        /// <summary>
        /// 通道总数
        /// </summary>
        public int channelCount => _channels == null ? 0 : _channels.Count;

        public T baseValue
        {
            get => _baseValue;
            set
            {
                if (!Equals(_baseValue, value))
                {
                    _baseValue = value;
                    OnChannelsChange();
                }
            }
        }

        /// <summary>
        /// 所有通道混合后的输出值
        /// </summary>
        public T channelsValue
        {
            get
            {
                if (_channelsChanged)
                {
                    _channelsValue = baseValue;
                    if (!RuntimeUtilities.IsNullOrEmpty(_channels))
                    {
                        for (int i = 0; i < _channels.Count; i++)
                            _channelsValue = Blend(_channelsValue, _channels[i].value);
                    }
                    _channelsChanged = false;
                }
                return _channelsValue;
            }
        }

        /// <summary>
        /// Final output
        /// </summary>
        public override T value
        {
            get => channelsValue;
            set => throw new NotSupportedException();
        }

        public Blender(T baseValue)
        {
            _baseValue = baseValue;
            _channelsValue = baseValue;
        }

        public void AddChannel(BaseBlendingChannel<T> channel)
        {
            if (channel.parent != null)
            {
                throw new Exception("Parent of channel is not null.");
            }

            if (_channels == null) _channels = new List<BaseBlendingChannel<T>>(4);

            channel.parent = this;
            _channels.Add(channel);
            OnChannelsChange();
        }

        public void RemoveChannel(BaseBlendingChannel<T> channel)
        {
            if (channel.parent != this)
            {
                throw new Exception("Parent of channel is not this.");
            }

            channel.parent = null;
            _channels.Remove(channel);
            OnChannelsChange();
        }

        internal void OnChannelsChange()
        {
            _channelsChanged = true;
            onChannelsChange?.Invoke();
            parent?.OnChannelsChange();
        }

        /// <summary>
        /// 判断两个值是否相等
        /// </summary>
        public abstract bool Equals(T a, T b);

        /// <summary>
        /// 混合值
        /// </summary>
        public abstract T Blend(T a, T b);

    } // class Blender<T>


    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class EventBlender<T> : Blender<T>
    {
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

        List<Event> _events;
        bool _hasEventsValue;
        T _eventsValue;

        public override T value
        {
            get => _hasEventsValue ? Blend(channelsValue, _eventsValue) : channelsValue;
        }

        public EventBlender(T baseValue) : base(baseValue) { }

        /// <summary>
        /// 创建事件
        /// 事件在超过作用时间后自动移除
        /// </summary>
        /// <param name="attenuation"> 曲线的 Y 值被截断为非负数 </param>
        public void AddEvent(float startDelay, float duration, T value, AnimationCurve attenuation)
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
            bool lastHasEventsValue = _hasEventsValue;
            _hasEventsValue = false;

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

                        if (_hasEventsValue)
                        {
                            _eventsValue = Blend(_eventsValue, Scale(e.value, e.currentScaleFactor));
                        }
                        else
                        {
                            _hasEventsValue = true;
                            _eventsValue = Scale(e.value, e.currentScaleFactor);
                        }
                    }
                }

                if (!lastHasEventsValue || !_hasEventsValue)
                {
                    parent?.OnChannelsChange();
                }
            }
        }

        /// <summary>
        /// 缩放值
        /// </summary>
        public abstract T Scale(T a, float b);

    } // class EventBlender<T>


    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// </summary>
    public abstract class BoolBlender : Blender<bool>
    {
        public BoolBlender(bool baseValue) : base(baseValue) { }

        public sealed override bool Equals(bool a, bool b)
        {
            return a == b;
        }

    } // class BoolBlender


    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class FloatBlender : EventBlender<float>
    {
        public FloatBlender(float baseValue) : base(baseValue) { }

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
            AddEvent(preset.startDelay, preset.duration, preset.value, preset.attenuation);
        }

    } // class FloatBlender


    /// <summary>
    /// 混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public abstract class Vector2Blender : EventBlender<Vector2>
    {
        public Vector2Blender(Vector2 baseValue) : base(baseValue) { }
     
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
            AddEvent(preset.startDelay, preset.duration, preset.value, preset.attenuation);
        }

    } // class Vector2Blender


    /// <summary>
    /// 与混合控制器，用于控制多输入单输出。
    /// 每一种控制来源可根据使用方式选择使用 Channel 或 Event。
    /// </summary>
    public class BoolAndBlender : BoolBlender
    {
        public BoolAndBlender(bool baseValue = true) : base(baseValue) { }

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
        public BoolOrBlender(bool baseValue = false) : base(baseValue) { }

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
        public FloatAdditiveBlender(float baseValue = 0f) : base(baseValue) { }

        public float channelsAverageValue => channelsValue / channelCount;

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
        public FloatMultiplyBlender(float baseValue = 1f) : base(baseValue) { }

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
        public FloatMaximumBlender(float baseValue) : base(baseValue) { }

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
        public FloatMinimumBlender(float baseValue) : base(baseValue) { }

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
        public Vector2AdditiveBlender(Vector2 baseValue = new Vector2()) : base(baseValue) { }

        public Vector2 channelsAverageValue => channelsValue / channelCount;

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
        public Vector2MultiplyBlender(Vector2 baseValue) : base(baseValue) { }

        public Vector2MultiplyBlender() : base(new Vector2(1f, 1f)) { }

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
        public Vector2MaximumBlender(Vector2 baseValue) : base(baseValue) { }

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
        public Vector2MinimumBlender(Vector2 baseValue) : base(baseValue) { }

        public sealed override Vector2 Blend(Vector2 a, Vector2 b)
        {
            return Vector2.Min(a, b);
        }
    }

} // namespace UnityExtensions