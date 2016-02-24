using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace QuerySpecification
{
    [DataContract]
    [Serializable]
    public class SortCondition<TEntity>
    {
        private Type entityType;

        private ParameterExpression parameterExpression;

        private ParameterExpression orderableParameterExpression;

        [DataMember]
        protected List<SortItem> SortItems = new List<SortItem>();

        private SortCondition()
        {
        }

        public SortCondition<TDestionation> Cast<TDestionation>() where TDestionation : class
        {
            var sortCondition = new SortCondition<TDestionation>()
            {
                entityType = typeof(TDestionation),
                parameterExpression = parameterExpression,
                orderableParameterExpression = orderableParameterExpression
            };
            var result = sortCondition;
            foreach (var sortItem in SortItems)
            {
                var sortItem1 = new SortItem()
                {
                    PropertySelector = sortItem.PropertySelector,
                    SortDirection = sortItem.SortDirection
                };
                result.SortItems.Add(sortItem1);
            }
            return result;
        }

        public Expression<Func<IEnumerable<TEntity>, IOrderedEnumerable<TEntity>>> GetIEnumerableSortingExpression()
        {
            var sortExpression = GetSortingExpression(typeof(TEntity), true);
            if (sortExpression == null)
            {
                return null;
            }
            var parameterExpressionArray = new ParameterExpression[] { orderableParameterExpression };
            return Expression.Lambda<Func<IEnumerable<TEntity>, IOrderedEnumerable<TEntity>>>(sortExpression, parameterExpressionArray);
        }

        public Expression<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>> GetIQueryableSortingExpression()
        {
            var sortExpression = GetSortingExpression(typeof(TEntity));
            if (sortExpression == null)
            {
                return null;
            }
            var parameterExpressionArray = new ParameterExpression[] { orderableParameterExpression };
            return Expression.Lambda<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(sortExpression, parameterExpressionArray);
        }

        private static string GetSelectorStringFromExpression<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            var selectorString = selectorExpression.Body.ToString();
            return selectorString.Remove(0, selectorString.IndexOf('.') + 1);
        }

        private Expression GetSortingExpression(Type destinationType, bool isIEnumerable = false)
        {
            if (SortItems == null || SortItems.Count <= 0)
            {
                return null;
            }
            entityType = destinationType;
            if (destinationType == null)
            {
                entityType = typeof(TEntity);
            }
            parameterExpression = Expression.Parameter(entityType, "entity");
            orderableParameterExpression = Expression.Parameter((isIEnumerable ? typeof(IEnumerable<TEntity>) : typeof(IQueryable<TEntity>)), "f");
            var orderableType = typeof(Queryable);
            if (isIEnumerable)
            {
                orderableType = typeof(Enumerable);
            }
            MethodInfo orderByMethodInfo = null;
            Expression resultExpression = null;
            foreach (var sortingItem in SortItems)
            {
                var memberExpression = GetSortMemberExpression(sortingItem.PropertySelector, entityType, parameterExpression);
                switch (sortingItem.SortDirection)
                {
                    case SortDirection.Ascending:
                        {
                            if (resultExpression == null)
                            {
                                var methodInfo = orderableType.GetMethods().First((MethodInfo method) =>
                                {
                                    if (method.Name != "OrderBy")
                                    {
                                        return false;
                                    }
                                    return method.GetParameters().Length == 2;
                                });
                                var type = new Type[] { entityType, memberExpression.Type };
                                orderByMethodInfo = methodInfo.MakeGenericMethod(type);
                                break;
                            }
                            else
                            {
                                var methodInfo1 = orderableType.GetMethods().First(method =>
                                {
                                    if (method.Name != "ThenBy")
                                    {
                                        return false;
                                    }
                                    return method.GetParameters().Length == 2;
                                });
                                var typeArray = new Type[] { entityType, memberExpression.Type };
                                orderByMethodInfo = methodInfo1.MakeGenericMethod(typeArray);
                                break;
                            }
                        }
                    case SortDirection.Descending:
                        {
                            if (resultExpression == null)
                            {
                                var methodInfo2 = orderableType.GetMethods().First(method =>
                                {
                                    if (method.Name != "OrderByDescending")
                                    {
                                        return false;
                                    }
                                    return method.GetParameters().Length == 2;
                                });
                                var type1 = new Type[] { entityType, memberExpression.Type };
                                orderByMethodInfo = methodInfo2.MakeGenericMethod(type1);
                                break;
                            }
                            else
                            {
                                var methodInfo3 = orderableType.GetMethods().First(method =>
                                {
                                    if (method.Name != "ThenByDescending")
                                    {
                                        return false;
                                    }
                                    return method.GetParameters().Length == 2;
                                });
                                var typeArray1 = new Type[] { entityType, memberExpression.Type };
                                orderByMethodInfo = methodInfo3.MakeGenericMethod(typeArray1);
                                break;
                            }
                        }
                }
                var parameterExpressionArray = new ParameterExpression[] { parameterExpression };
                var lambdaExpression = Expression.Lambda(memberExpression, parameterExpressionArray);
                var methodCallExpression = (resultExpression == null ? Expression.Call(orderByMethodInfo, orderableParameterExpression, lambdaExpression) : Expression.Call(orderByMethodInfo, resultExpression, lambdaExpression));
                resultExpression = methodCallExpression;
            }
            return resultExpression;
        }

        private static MemberExpression GetSortMemberExpression(string selectorString, Type parameterExpressionType, ParameterExpression parameterExpression)
        {
            if (string.IsNullOrWhiteSpace(selectorString))
            {
                throw new ArgumentNullException("selectorString", "Selector string is not valid");
            }
            var propertyParts = selectorString.Split(new char[] { '.' });
            if (propertyParts.Any(new Func<string, bool>(string.IsNullOrWhiteSpace)))
            {
                throw new Exception(string.Format("Selector string \"{0}\" format is not valid.", selectorString));
            }
            var firstPartOfSelector = propertyParts[0].ToString(CultureInfo.InvariantCulture);
            var propertyInThisType = parameterExpressionType.GetProperty(firstPartOfSelector);
            if (propertyInThisType == null)
            {
                throw new Exception(string.Format("Selector string \"{0}\" is not exist in type \"{1}\".", selectorString, parameterExpressionType.Name));
            }
            var me = Expression.Property(parameterExpression, propertyInThisType);
            if (propertyParts.Length == 1)
            {
                return me;
            }
            return GetSortMemberExpression(string.Join(".", propertyParts, 1, propertyParts.Length - 1), me);
        }

        private static MemberExpression GetSortMemberExpression(string selectorString, MemberExpression memberExpression)
        {
            if (string.IsNullOrWhiteSpace(selectorString))
            {
                throw new ArgumentNullException("selectorString", "Selector string is not valid");
            }
            var propertyParts = selectorString.Split(new char[] { '.' });
            if (propertyParts.Any(new Func<string, bool>(string.IsNullOrWhiteSpace)))
            {
                throw new Exception(string.Format("Selector string \"{0}\" format is not valid.", selectorString));
            }
            var firstPartOfSelector = propertyParts[0].ToString(CultureInfo.InvariantCulture);
            var propertyInThisType = memberExpression.Type.GetProperty(firstPartOfSelector);
            if (propertyInThisType == null)
            {
                throw new Exception(string.Format("Selector string \"{0}\" is not exist in type \"{1}\".", selectorString, memberExpression.Type.Name));
            }
            var me = Expression.Property(memberExpression, propertyInThisType);
            var result = propertyParts.Length != 1
                ? GetSortMemberExpression(string.Join(".", propertyParts, 1, propertyParts.Length - 1), me)
                : Expression.Property(memberExpression, propertyInThisType);
            return result;
        }

        public static SortCondition<TEntity> OrderBy<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            if (selectorExpression == null)
            {
                throw new ArgumentException("Selector string can not be null or empty", "selectorExpression");
            }
            var result = new SortCondition<TEntity>()
            {
                entityType = typeof(TEntity)
            };
            var sortItem = new SortItem()
            {
                PropertySelector = GetSelectorStringFromExpression(selectorExpression),
                SortDirection = SortDirection.Ascending
            };
            result.SortItems.Add(sortItem);
            return result;
        }

        public static SortCondition<TEntity> OrderBy(string propertySelector)
        {
            if (string.IsNullOrEmpty(propertySelector))
            {
                throw new ArgumentException("Selector string can not be null or empty", "propertySelector");
            }
            var result = new SortCondition<TEntity>()
            {
                entityType = typeof(TEntity)
            };
            var sortItem = new SortItem()
            {
                PropertySelector = propertySelector,
                SortDirection = SortDirection.Ascending
            };
            result.SortItems.Add(sortItem);
            return result;
        }

        public static SortCondition<TEntity> OrderByDescending<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            if (selectorExpression == null)
            {
                throw new ArgumentException("Selector string can not be null or empty", "selectorExpression");
            }
            var result = new SortCondition<TEntity>()
            {
                entityType = typeof(TEntity)
            };
            var sortItem = new SortItem()
            {
                PropertySelector = GetSelectorStringFromExpression(selectorExpression),
                SortDirection = SortDirection.Descending
            };
            result.SortItems.Add(sortItem);
            return result;
        }

        public static SortCondition<TEntity> OrderByDescending(string propertySelector)
        {
            if (string.IsNullOrEmpty(propertySelector))
            {
                throw new ArgumentException("Selector string can not be null or empty", "propertySelector");
            }
            var result = new SortCondition<TEntity>()
            {
                entityType = typeof(TEntity)
            };
            var sortItem = new SortItem()
            {
                PropertySelector = propertySelector,
                SortDirection = SortDirection.Descending
            };
            result.SortItems.Add(sortItem);
            return result;
        }

        public SortCondition<TEntity> ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            if (selectorExpression == null)
            {
                throw new ArgumentException("Selector string can not be null or empty", "selectorExpression");
            }
            var sortItem = new SortItem()
            {
                PropertySelector = GetSelectorStringFromExpression(selectorExpression),
                SortDirection = SortDirection.Ascending
            };
            SortItems.Add(sortItem);
            return this;
        }

        public SortCondition<TEntity> ThenBy(string propertySelector)
        {
            if (string.IsNullOrEmpty(propertySelector))
            {
                throw new ArgumentException("Selector string can not be null or empty", "propertySelector");
            }
            var sortItem = new SortItem()
            {
                PropertySelector = propertySelector,
                SortDirection = SortDirection.Ascending
            };
            SortItems.Add(sortItem);
            return this;
        }

        public SortCondition<TEntity> ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            if (selectorExpression == null)
            {
                throw new ArgumentException("Selector string can not be null or empty", "selectorExpression");
            }
            var sortItem = new SortItem()
            {
                PropertySelector = GetSelectorStringFromExpression(selectorExpression),
                SortDirection = SortDirection.Descending
            };
            SortItems.Add(sortItem);
            return this;
        }

        public SortCondition<TEntity> ThenByDescending(string propertySelector)
        {
            if (string.IsNullOrEmpty(propertySelector))
            {
                throw new ArgumentException("Selector string can not be null or empty", "propertySelector");
            }
            var sortItem = new SortItem()
            {
                PropertySelector = propertySelector,
                SortDirection = SortDirection.Descending
            };
            SortItems.Add(sortItem);
            return this;
        }
    }
}