
namespace UnityExtensions
{
    public abstract class BaseBlendingChannel<T>
    {
        public abstract T value { get; set; }
        public Blender<T> parent { get; internal set; }
    }

    /// <summary>
    /// 混合通道
    /// </summary>
    public class BlendingChannel<T> : BaseBlendingChannel<T>
    {
        T _value;

        /// <summary>
        /// 通道值
        /// </summary>
        public override T value
        {
            get => _value;
            set
            {
                if (parent != null)
                {
                    if (!parent.Equals(_value, value))
                    {
                        _value = value;
                        parent.OnChannelsChange();
                    }
                }
                else _value = value;
            }
        }

        public BlendingChannel(T value) => _value = value;
    }

} // namespace UnityExtensions