using System;
using System.Runtime.Serialization;

namespace QuerySpecification
{
    /// <summary>
    /// 分页参数
    /// </summary>
    [DataContract]
    [Serializable]
    public class PagerArgs
    {
        /// <summary>
        /// 每页条数
        /// </summary>
        [DataMember]
        public int ItemsPerPage { get; set; }

        /// <summary>
        /// 第几页
        /// </summary>
        [DataMember]
        public int PageNumber { get; set; }
    }
}