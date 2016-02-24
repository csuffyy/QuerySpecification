using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Reflection;

namespace QuerySpecification
{
    /// <summary>
    /// 集合查询扩展方法类
    /// </summary>
    public static class QueryExtensions
    {
        /// <summary>
        /// 根据指定的查询规约对数据源进行查询
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="specification">查询规约</param>
        /// <returns>查询到的数据集合</returns>
        public static IEnumerable<TEntity> Query<TEntity>(this IEnumerable<TEntity> source, Specification<TEntity> specification)
            where TEntity : class
        {
            var predicate = specification.Criteria.GetExpression().Compile();

            //查询
            var temp = source.Where(predicate);

            //分页
            if (specification.PagerArgs != null)
            {
                var skip = specification.PagerArgs.PageNumber > 1
                    ? specification.PagerArgs.ItemsPerPage * (specification.PagerArgs.PageNumber - 1)
                    : 0;
                var take = specification.PagerArgs.ItemsPerPage;

                temp = temp.Skip(skip).Take(take);
            }

            if (specification.SortCondition == null)
            {
                return temp;
            }

            //排序
            var sortingFunc = specification.SortCondition.GetIEnumerableSortingExpression().Compile();
            var entities = sortingFunc(temp);
            return entities;
        }

        /// <summary>
        /// 根据指定的查询规约对数据源进行查询
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="specification">查询规约</param>
        /// <returns>查询到的数据集合</returns>
        public static IQueryable<TEntity> Query<TEntity>(this IQueryable<TEntity> source, Specification<TEntity> specification)
            where TEntity : class
        {
            //扩展
            if (specification.IncludedNavigationProperties.Any())
            {
                source = specification.IncludedNavigationProperties.Aggregate(source, (current, path) => current.Include(path));
            }

            //查询
            var predicate = specification.Criteria.GetExpression();

            var temp = source.Where(predicate);

            //分页
            if (specification.PagerArgs != null)
            {
                var skip = specification.PagerArgs.PageNumber > 1
                    ? specification.PagerArgs.ItemsPerPage * (specification.PagerArgs.PageNumber - 1)
                    : 0;
                var take = specification.PagerArgs.ItemsPerPage;

                temp = temp.Skip(skip).Take(take);
            }

            if (specification.SortCondition == null)
            {
                return temp;
            }

            //排序
            var sortingFunc = specification.SortCondition.GetIQueryableSortingExpression().Compile();
            var entities = sortingFunc(temp);
            return entities;
        }

        /// <summary>
        /// Specifies the related objects to include in the query results.
        /// </summary>
        /// <remarks>
        /// This extension method calls the Include(String) method of the source <see cref="T:System.Linq.IQueryable`1" /> object,
        /// if such a method exists. If the source <see cref="T:System.Linq.IQueryable`1" /> does not have a matching method,
        /// then this method does nothing. The <see cref="T:System.Data.Entity.Core.Objects.ObjectQuery`1" />, <see cref="T:System.Data.Entity.Core.Objects.ObjectSet`1" />,
        /// <see cref="T:System.Data.Entity.Infrastructure.DbQuery`1" /> and <see cref="T:System.Data.Entity.DbSet`1" /> types all have an appropriate Include method to call.
        /// Paths are all-inclusive. For example, if an include call indicates Include("Orders.OrderLines"), not only will
        /// OrderLines be included, but also Orders.  When you call the Include method, the query path is only valid on
        /// the returned instance of the <see cref="T:System.Linq.IQueryable`1" />. Other instances of <see cref="T:System.Linq.IQueryable`1" />
        /// and the object context itself are not affected. Because the Include method returns the query object,
        /// you can call this method multiple times on an <see cref="T:System.Linq.IQueryable`1" /> to specify multiple paths for the query.
        /// </remarks>
        /// <typeparam name="T"> The type of entity being queried. </typeparam>
        /// <param name="source">
        /// The source <see cref="T:System.Linq.IQueryable`1" /> on which to call Include.
        /// </param>
        /// <param name="path"> The dot-separated list of related objects to return in the query results. </param>
        /// <returns>
        /// A new <see cref="T:System.Linq.IQueryable`1" /> with the defined query path.
        /// </returns>
        public static IQueryable<T> Include<T>(this IQueryable<T> source, string path)
        {
            var objectQuery = source as ObjectQuery<T>;
            if (objectQuery == null)
            {
                return CommonInclude(source, path);
            }

            return objectQuery.Include(path);
        }

        private static T CommonInclude<T>(T source, string path)
        {
            Type type = source.GetType();
            var typeArray = new Type[]
            {
                typeof (string), typeof (IComparable), typeof (ICloneable), typeof (IComparable<string>),
                typeof (IEnumerable<char>), typeof (IEnumerable), typeof (IEquatable<string>), typeof (object)
            };

            var runtimeMethod = type.GetRuntimeMethod("Include", typeArray);
            if (runtimeMethod == null || !typeof(T).IsAssignableFrom(runtimeMethod.ReturnType))
            {
                return source;
            }

            object obj = source;
            object[] objArray = new object[] { path };
            return (T)runtimeMethod.Invoke(obj, objArray);
        }
    }
}