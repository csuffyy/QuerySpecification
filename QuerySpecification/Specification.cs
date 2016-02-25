using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace QuerySpecification
{
    /// <summary>
    /// 查询规约（可包含查询、分页、排序、扩展等操作）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    [DataContract]
    [Serializable]
    public class Specification<TEntity> where TEntity : class
    {
        private List<Expression<Func<TEntity, object>>> includedNavigationPropertiesExpression;

        /// <summary>
        /// 查询条件
        /// </summary>
        [DataMember]
        public Criteria<TEntity> Criteria { get; set; }

        /// <summary>
        /// 导航属性
        /// </summary>
        [DataMember]
        public List<string> IncludedNavigationProperties { get; set; }

        public List<Expression<Func<TEntity, object>>> IncludedNavigationPropertiesExpression
        {
            get
            {
                return includedNavigationPropertiesExpression;
            }
            set
            {
                if (value != null)
                {
                    if (IncludedNavigationProperties == null)
                    {
                        IncludedNavigationProperties = new List<string>();
                    }

                    foreach (var expression in value)
                    {
                        var selectorString = expression.Body.ToString();
                        IncludedNavigationProperties.Add(selectorString.Remove(0, selectorString.IndexOf('.') + 1));
                    }
                }
                else
                {
                    IncludedNavigationProperties = new List<string>();
                }

                includedNavigationPropertiesExpression = value;
            }
        }

        /// <summary>
        /// 分页参数
        /// </summary>
        [DataMember]
        public Pagination Pagination { get; set; }

        /// <summary>
        /// 排序条件
        /// </summary>
        [DataMember]
        public SortCondition<TEntity> SortCondition { get; set; }

        /// <summary>
        /// 转换查询规约的实体类型
        /// </summary>
        public Specification<TDestination> Cast<TDestination>() where TDestination : class
        {
            var result = new Specification<TDestination>();
            if (IncludedNavigationProperties != null && IncludedNavigationProperties.Count > 0)
            {
                result.IncludedNavigationProperties = new List<string>();
                foreach (var includedNavigationProperty in IncludedNavigationProperties)
                {
                    var stringBuilder = new StringBuilder(includedNavigationProperty);
                    result.IncludedNavigationProperties.Add(stringBuilder.ToString());
                }
            }
            if (Pagination != null)
            {
                result.Pagination = new Pagination(Pagination.PageSize, Pagination.PageIndex);
            }
            result.Criteria = Criteria.Cast<TDestination>();
            if (SortCondition != null)
            {
                result.SortCondition = SortCondition.Cast<TDestination>();
            }
            return result;
        }

        /// <summary>
        /// 转为Json字符串
        /// </summary>
        /// <returns>Json字符串</returns>
        public string ToJson()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter());
            return json;
        }

        /// <summary>
        /// 保存当前查询条件
        /// </summary>
        /// <param name="fileName">文件名称</param>
        public void Save(string fileName)
        {
            var json = ToJson();
            File.WriteAllText(fileName, json);
        }

        /// <summary>
        /// 从Json字符串中加载查询条件
        /// </summary>
        /// <param name="jsonString">Json字符串</param>
        /// <returns>查询条件</returns>
        public static Specification<TEntity> LoadFromString(string jsonString)
        {
            try
            {
                var spec = JsonConvert.DeserializeObject<Specification<TEntity>>(jsonString);
                return spec;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 从文件中加载查询条件
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <returns>查询条件</returns>
        public static Specification<TEntity> LoadFromFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }

            try
            {
                var jsonString = File.ReadAllText(fileName);
                var spec = JsonConvert.DeserializeObject<Specification<TEntity>>(jsonString);
                return spec;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}