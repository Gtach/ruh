using System;

namespace UnitOfWork
{
    /// <summary>
    /// Attribute class to mark a IChangeable instance as not to be updateable. 
    /// All updates need to be converted to inserts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NotUpdateableAttribute : Attribute
    {
    }
}
