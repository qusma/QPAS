// -----------------------------------------------------------------------
// <copyright file="GenericRepository.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EntityModel;

namespace QPAS
{
    public class GenericRepository<T> : IDisposable where T : class
    {
        internal IDBContext Context;
        internal DbSet<T> DBSet;

        public void Dispose()
        {
            if (Context != null)
            {
                Context.Dispose();
                Context = null;
            }
        }
        public GenericRepository(IDBContext context)
        {
            Context = context;
            DBSet = context.Set<T>();
        }

        public virtual IQueryable<T> Get(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<T> query = DBSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return orderBy(query);
            }
            else
            {
                return query;
            }
        }

        public virtual void Add(T entity)
        {
            DBSet.Add(entity);
        }

        public virtual void Delete(T entityToDelete)
        {
            if (Context.Entry(entityToDelete).State == EntityState.Detached)
            {
                DBSet.Attach(entityToDelete);
            }
            DBSet.Remove(entityToDelete);
        }

        public virtual async Task Save()
        {
            await Context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}