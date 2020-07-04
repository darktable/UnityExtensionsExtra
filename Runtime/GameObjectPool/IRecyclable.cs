
namespace UnityExtensions
{
    public interface IActivatable
    {
        void Activate();
    }

    public interface IActivatable<T>
    {
        void Activate(T a);
    }

    public interface IActivatable<T1, T2>
    {
        void Activate(T1 a, T2 b);
    }

    public interface IActivatable<T1, T2, T3>
    {
        void Activate(T1 a, T2 b, T3 c);
    }

    public interface IActivatable<T1, T2, T3, T4>
    {
        void Activate(T1 a, T2 b, T3 c, T4 d);
    }

    public interface IDeactivatable
    {
        void Deactivate();
    }

    public interface IRecyclable : IActivatable, IDeactivatable { }

    public interface IRecyclable<T> : IActivatable<T>, IDeactivatable { }

    public interface IRecyclable<T1, T2> : IActivatable<T1, T2>, IDeactivatable { }

    public interface IRecyclable<T1, T2, T3> : IActivatable<T1, T2, T3>, IDeactivatable { }

    public interface IRecyclable<T1, T2, T3, T4> : IActivatable<T1, T2, T3, T4>, IDeactivatable { }

} // namespace UnityExtensions