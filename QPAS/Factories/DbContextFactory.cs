using EntityModel;
using System;

namespace QPAS
{
    public class DbContextFactory : IContextFactory
    {
        public DbContextFactory(Func<IQpasDbContext> CreateContext)
        {
            _createContext = CreateContext;
        }

        private Func<IQpasDbContext> _createContext;

        public IQpasDbContext Get()
        {
            return _createContext();
        }
    }
}
