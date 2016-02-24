using System;
using System.Runtime.Serialization;

namespace QuerySpecification
{
    [DataContract]
    [Serializable]
    public enum Operator
    {
        /// <summary>
        /// 等于
        /// </summary>
        [EnumMember]
        Equal = 1,

        /// <summary>
        /// 不等于
        /// </summary>
        [EnumMember]
        NotEqual = 2,

        /// <summary>
        /// 包含（仅限于集合，不能用于字符串）
        /// </summary>
        [EnumMember]
        Contain = 3,

        /// <summary>
        /// 不包含（仅限于集合，不能用于字符串）
        /// </summary>
        [EnumMember]
        NotContain = 4,

        /// <summary>
        /// 包含（只能用于字符串查询）
        /// </summary>
        [EnumMember]
        Like = 5,

        /// <summary>
        /// 不包含（只能用于字符串查询）
        /// </summary>
        [EnumMember]
        NotLike = 6,

        /// <summary>
        /// 开始于（只能用于字符串查询）
        /// </summary>
        [EnumMember]
        StartsWith = 7,

        /// <summary>
        /// 不开始于（只能用于字符串查询）
        /// </summary>
        [EnumMember]
        NotStartsWith = 8,

        /// <summary>
        /// 结尾于（只能用于字符串查询）
        /// </summary>
        [EnumMember]
        EndsWith = 9,

        /// <summary>
        /// 不结尾于（只能用于字符串查询）
        /// </summary>
        [EnumMember]
        NotEndsWith = 10,

        /// <summary>
        /// 大于
        /// </summary>
        [EnumMember]
        GreaterThan = 11,

        /// <summary>
        /// 大于等于
        /// </summary>
        [EnumMember]
        GreaterThanOrEqual = 12,

        /// <summary>
        /// 小于
        /// </summary>
        [EnumMember]
        LessThan = 13,

        /// <summary>
        /// 小于等于
        /// </summary>
        [EnumMember]
        LessThanOrEqual = 14,

        /// <summary>
        /// 为空
        /// </summary>
        [EnumMember]
        IsNull = 15,

        /// <summary>
        /// 不为空
        /// </summary>
        [EnumMember]
        IsNotNull = 16,

        /// <summary>
        /// 空
        /// </summary>
        [EnumMember]
        None = 17
    }
}