using System;

namespace ChangeTrack
{
    /// <summary>
    /// Attribute class to mark a public property of an IChangeTrackable instance as not to be change-tracked
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NotTrackedAttribute : Attribute
    {
    }
}
