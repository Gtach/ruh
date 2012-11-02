namespace UnitOfWork
{
    /// <summary>
    /// State a tracked IChangeable instance is in
    /// </summary>
    public enum ChangeState
    {
        /// <summary>instance is not attached to change tracking</summary>
        Unattached,
        /// <summary>instance is not changed</summary>
        Unchanged,
        /// <summary>instance has changes</summary>
        Changed,
        /// <summary>instance is new</summary>
        New,
        /// <summary>instance is deleted</summary>
        Deleted
    }
}
