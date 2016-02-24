using System;

namespace QuerySpecification
{
    internal static class ExtensionMethods
    {
        public static bool IsBoolean(this Type type)
        {
            if (type == null)
            {
                return false;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    {
                        if (!type.IsGenericType || !(type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        {
                            return false;
                        }
                        return Nullable.GetUnderlyingType(type).IsBoolean();
                    }
                case TypeCode.DBNull:
                    {
                        return false;
                    }
                case TypeCode.Boolean:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        public static bool IsNumericType(this Type type)
        {
            if (type == null)
            {
                return false;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    {
                        if (!type.IsGenericType || !(type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        {
                            return false;
                        }
                        return Nullable.GetUnderlyingType(type).IsNumericType();
                    }
                case TypeCode.DBNull:
                case TypeCode.Boolean:
                case TypeCode.Char:
                    {
                        return false;
                    }
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        public static string ToEnglishNumber(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return input.Replace(",", "").Replace("۰", "0").Replace("۱", "1").Replace("۲", "2").Replace("۳", "3").Replace("۴", "4").Replace("۵", "5").Replace("۶", "6").Replace("۷", "7").Replace("۸", "8").Replace("۹", "9");
        }
    }
}