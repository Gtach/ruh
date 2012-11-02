using System;

namespace UnitOfWork
{
    /// <summary>
    /// Attribute class to mark a public property of an IChangeable instance as not to be change-tracked
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NotTrackedAttribute : Attribute
    {
    }
}
