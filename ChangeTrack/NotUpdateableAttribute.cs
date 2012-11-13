using System;

namespace ChangeTrack
{
    /// <summary>
    /// Attribute class to mark a IChangeTrackable instance as not to be updateable. 
    /// All updates need to be converted to inserts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NotUpdateableAttribute : Attribute
    {
    }
}
