namespace Common.interfaces
{
    public interface IPropertyManagerInfo
    {
        string PropertyName { get; }
        PropertyManagerInfoType InfoType { get; }
        object GetValue(object obj);
        void SetValue(object obj, object value);
        T GetReferenceValue<T>(object obj) where T : class;
    }
}
