using EntityModel;

namespace QPAS
{
    public interface IContextFactory
    {
        public IQpasDbContext Get();
    }
}
