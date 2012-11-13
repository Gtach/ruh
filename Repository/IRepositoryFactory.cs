using Common.interfaces;

namespace Repository
{
    public interface IRepositoryFactory
    {
        IRepository Create();
    }
}
