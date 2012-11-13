namespace Common.interfaces
{
    public interface IUnitOfWork
    {
        void StartTransaction(object rootItem);
        void Commit();
        void Rollback();
    }
}
