using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RLM.Models.Interfaces
{
    public interface IRlmDbData : IRlmDbDataLegacy
    {
        string DatabaseName { get; set; }

        void CreateTable<T>();
        void Create<T>(T entity);
        void Create<T>(IEnumerable<T> entity);
        void Update<T>(T entity);
        IEnumerable<T> FindAll<T>();
        IEnumerable<TResult> FindAll<TBase, TResult>();
        T FindByID<T>(long id);
        TResult FindByID<TBase, TResult>(long id);
        T FindByName<T>(string name);
        TResult FindByName<TBase, TResult>(string name);
        void DropDB(string dbName);
        int Count<T>();
        TResult Max<TSource, TResult>(Expression<Func<TSource, TResult>> propertyExpr);

        // Load network specific functions
        int CountBestSolutions();
        IEnumerable<BestSolution> LoadBestSolutions();
        IEnumerable<Session> LoadVisibleSessions();

        void Initialize();
    }
}
