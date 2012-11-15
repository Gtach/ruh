using System.Collections.Generic;
using System.Linq;
using Common.interfaces;

namespace ChangeTrack
{
    public struct Change
    {
        public object OldValue;
        public object NewValue;

        public bool IsBubbleUpChange 
        {
            get { return OldValue == NewValue; } 
        }
    }

    public class Changes
    {
        public readonly IDictionary<string, Change> AllChanges = new Dictionary<string, Change>();

        public void Add(string propertyName, Change change)
        {
            if (!AllChanges.ContainsKey(propertyName))
                AllChanges.Add(propertyName, change);
        }

        public bool HasChanges(object obj, IPropertyManager propertyManager)
        {
            return AllChanges.Keys.Any(propertyName => propertyManager.GetInfoType(obj, propertyName) != PropertyManagerInfoType.Association);
        }
    }

}
