using System.Collections.Generic;

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

        public bool HasChanges(IDictionary<string, ChangeablePropertyInfo> properties)
        {
            foreach (string propertyName in AllChanges.Keys)
            {
                ChangeablePropertyInfo changeablePropertyInfo;
                if (properties.TryGetValue(propertyName, out changeablePropertyInfo) && (changeablePropertyInfo.InfoType != ChangeablePropertyInfoType.Association))
                    return true;
            }
            return false;
        }
    }

}
