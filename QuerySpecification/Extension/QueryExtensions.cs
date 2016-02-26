using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
            if (specification == null)
            {
                throw new NullReferenceException(string.Format("Specification<{0}>", typeof(TEntity).Name));
            }

            //查询
            var predicate = specification.Criteria.GetExpression().Compile();
            var temp = source.Where(predicate);

            //排序
            if (specification.SortCondition != null)
            {
                var sortingFunc = specification.SortCondition.GetIEnumerableSortingExpression().Compile();
                temp = sortingFunc(temp);
            }

            //分页
            if (specification.Pagination != null)
            {
                var skip = specification.Pagination.PageIndex > 1
                    ? specification.Pagination.PageSize * (specification.Pagination.PageIndex - 1)
                    : 0;
                var take = specification.Pagination.PageSize * specification.Pagination.PageCount;

                temp = temp.Skip(skip).Take(take);
            }

            return temp;
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
            if (specification == null)
            {
                throw new NullReferenceException(string.Format("Specification<{0}>", typeof(TEntity).Name));
            }

            //扩展
            if (specification.IncludedNavigationProperties != null)
            {
                foreach (var property in specification.IncludedNavigationProperties)
                {
                    try
                    {
                        source = (IQueryable<TEntity>)((dynamic)source).Include(property);
                    }
                    catch
                    {
                        break;
                    }
                }
            }

            //查询
            var predicate = specification.Criteria.GetExpression();
            var temp = source.Where(predicate);

            //排序
            if (specification.SortCondition != null)
            {
                var sortingFunc = specification.SortCondition.GetIQueryableSortingExpression().Compile();
                temp = sortingFunc(temp);
            }

            //分页
            if (specification.Pagination != null)
            {
                var skip = specification.Pagination.PageIndex > 1
                    ? specification.Pagination.PageSize * (specification.Pagination.PageIndex - 1)
                    : 0;
                var take = specification.Pagination.PageSize * specification.Pagination.PageCount;

                temp = temp.Skip(skip).Take(take);
            }

            return temp;
        }
    }
}