using Common.interfaces;

namespace UnitOfWork
{
    /// <summary>
    /// Interface for changeable classes. Adds extension methods when using UnitOfWork namespace
    /// </summary>
    public interface IChangeable : INotifyingObject
    {
    }
}
