using System;
using System.Runtime.Serialization;

namespace QuerySpecification
{
    /// <summary>
    /// 分页参数
    /// </summary>
    [DataContract]
    [Serializable]
    public class Pagination
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pageSize">每页条数</param>
        /// <param name="pageIndex">第几页(从第1页开始)</param>
        /// <param name="pageCount">连续取多少页（默认只取1页）</param>
        public Pagination(int pageSize, int pageIndex, int pageCount = 1)
        {
            if (pageSize < 1)
            {
                pageSize = 1;
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            if (pageCount < 1)
            {
                pageCount = 1;
            }

            PageSize = pageSize;
            PageIndex = pageIndex;
            PageCount = pageCount;
        }

        /// <summary>
        /// 每页条数
        /// </summary>
        [DataMember]
        public int PageSize { get; set; }

        /// <summary>
        /// 第几页(从第1页开始)
        /// </summary>
        [DataMember]
        public int PageIndex { get; set; }

        /// <summary>
        /// 连续取多少页（默认只取1页）
        /// </summary>
        [DataMember]
        public int PageCount { get; set; }
    }
}