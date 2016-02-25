using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace QuerySpecification
{
    [DataContract]
    [Serializable]
    public class Criteria<TEntity> where TEntity : class
    {
        private static readonly ObjectIDGenerator idGenerator;

        private static bool firstTime;

        private static readonly Type stringType;

        private List<double> checkedIds;
        private Type entityType;

        static Criteria()
        {
            idGenerator = new ObjectIDGenerator();
            stringType = typeof(string);
        }

        private Criteria() { }

        [DataMember]
        private Condition ConditionContainer { get; set; }

        public Criteria<TEntity> And(Criteria<TEntity> critaria)
        {
            if (critaria == null)
            {
                return this;
            }
            if (entityType != critaria.entityType)
            {
                throw new Exception(string.Format("critaria must be from '{0}' type", entityType.Assembly.FullName));
            }
            ConditionContainer.Tree.NextLogicalOperator = LogicalOperator.And;
            ConditionContainer.Tree.Children.Add(critaria.ConditionContainer.Tree);
            return this;
        }

        public Criteria<TEntity> And<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression,
            Operator operationType, object value)
        {
            if (selectorExpression == null)
            {
                throw new ArgumentException("Selector string can not be null or empty", "selectorExpression");
            }
            var conditionTree = new ConditionTree()
            {
                OperationType = operationType,
                Value = value,
                NextLogicalOperator = LogicalOperator.And,
                SelectorString = GetSelectorStringFromExpression<TProperty>(selectorExpression)
            };
            var newConditionTree = conditionTree;
            newConditionTree.Id = idGenerator.GetId(newConditionTree, out firstTime);
            ConditionContainer.Tree.Children.Add(newConditionTree);
            return this;
        }

        public Criteria<TEntity> And(string selectorString, Operator operationType, object value)
        {
            if (string.IsNullOrWhiteSpace(selectorString))
            {
                throw new ArgumentException("Selector string can not be null or empty", "selectorString");
            }
            var targetPropertyType = GetTargetPropertyType(entityType, selectorString);
            if (value != null && targetPropertyType != value.GetType() && operationType != Operator.Contain &&
                operationType != Operator.NotContain)
            {
                value = ChangeValueType(targetPropertyType, value);
            }
            var conditionTree = new ConditionTree()
            {
                OperationType = operationType,
                Value = value,
                NextLogicalOperator = LogicalOperator.And,
                SelectorString = selectorString
            };
            var newConditionTree = conditionTree;
            newConditionTree.Id = idGenerator.GetId(newConditionTree, out firstTime);
            ConditionContainer.Tree.Children.Add(newConditionTree);
            return this;
        }

        public Criteria<TDestionation> Cast<TDestionation>()
            where TDestionation : class
        {
            var criterium = new Criteria<TDestionation>()
            {
                entityType = typeof(TDestionation)
            };
            var condition = new Condition()
            {
                EntityTypeName = ConditionContainer.EntityTypeName,
                Id = ConditionContainer.Id,
                Tree = CopyConditionTree(ConditionContainer.Tree)
            };
            criterium.ConditionContainer = condition;
            return criterium;
        }

        private static object ChangeValueType(Type targetPropertyType, object value)
        {
            object propertyValue;
            var propertyValueInString = value.ToString().ToEnglishNumber();
            if (targetPropertyType == typeof(DateTime) || targetPropertyType == typeof(DateTime?))
            {
                propertyValue = DateTime.Parse(propertyValueInString);
            }
            else if (targetPropertyType == typeof(DateTimeOffset) || targetPropertyType == typeof(DateTimeOffset?))
            {
                propertyValue = DateTimeOffset.Parse(propertyValueInString);
            }
            else if (targetPropertyType.IsNumericType())
            {
                propertyValueInString = Regex.Replace(propertyValueInString, "[^\\d\\.]+|\\.+$|^\\.+", "");
                try
                {
                    propertyValue = TypeDescriptor.GetConverter(targetPropertyType).ConvertFrom(propertyValueInString);
                }
                catch
                {
                    propertyValue = targetPropertyType.GetField("MinValue").GetRawConstantValue();
                }
            }
            else if (!targetPropertyType.IsBoolean())
            {
                propertyValue = Convert.ChangeType(propertyValueInString, targetPropertyType);
            }
            else
            {
                try
                {
                    var typeConverter = TypeDescriptor.GetConverter(targetPropertyType);
                    propertyValue = typeConverter.ConvertFrom(propertyValueInString.Trim());
                }
                catch
                {
                    propertyValue = false;
                }
                if (propertyValue != null && propertyValue.Equals("") && Nullable.GetUnderlyingType(targetPropertyType) != null)
                {
                    propertyValue = null;
                }
            }
            return propertyValue;
        }

        private Expression ConvertConditionToExpresion(ConditionTree conditionTree, Type parameterExpressionType,
            ParameterExpression parameterExpression)
        {
            var resultExpression = GetConditionExpression(conditionTree, parameterExpressionType,
                parameterExpression);
            foreach (var childrenConditionTree in conditionTree.Children)
            {
                if (checkedIds.Contains(childrenConditionTree.Id))
                {
                    continue;
                }
                checkedIds.Add(childrenConditionTree.Id);
                switch (conditionTree.NextLogicalOperator)
                {
                    case LogicalOperator.And:
                        {
                            resultExpression = Expression.AndAlso(resultExpression,
                                ConvertConditionToExpresion(childrenConditionTree, parameterExpressionType,
                                    parameterExpression));
                            continue;
                        }
                    case LogicalOperator.Or:
                        {
                            resultExpression = Expression.OrElse(resultExpression,
                                ConvertConditionToExpresion(childrenConditionTree, parameterExpressionType,
                                    parameterExpression));
                            continue;
                        }
                    default:
                        {
                            continue;
                        }
                }
            }
            return resultExpression;
        }

        private static ConditionTree CopyConditionTree(ConditionTree sourceConditionTree)
        {
            if (sourceConditionTree == null)
            {
                return null;
            }
            var conditionTree = new ConditionTree()
            {
                Id = sourceConditionTree.Id,
                NextLogicalOperator = sourceConditionTree.NextLogicalOperator,
                OperationType = sourceConditionTree.OperationType,
                SelectorString = sourceConditionTree.SelectorString,
                Value = sourceConditionTree.Value,
                SerializedValue = sourceConditionTree.SerializedValue
            };
            var result = conditionTree;
            if (sourceConditionTree.Children != null && sourceConditionTree.Children.Count > 0)
            {
                result.Children = new List<ConditionTree>();
                foreach (var childrenCondition in sourceConditionTree.Children)
                {
                    var clonedObject = CopyConditionTree(childrenCondition);
                    if (clonedObject == null)
                    {
                        continue;
                    }
                    result.Children.Add(clonedObject);
                }
            }
            return result;
        }

        public static Criteria<TEntity> False()
        {
            var entityType = typeof(TEntity);
            var criterium = new Criteria<TEntity>();
            var condition = new Condition();
            var conditionTree = new ConditionTree()
            {
                OperationType = Operator.None,
                NextLogicalOperator = LogicalOperator.Or,
                Value = TrueFalse.False
            };
            condition.Tree = conditionTree;
            condition.EntityTypeName = entityType.Name;
            criterium.ConditionContainer = condition;
            criterium.entityType = entityType;
            var critaria = criterium;
            critaria.ConditionContainer.Id = idGenerator.GetId(critaria.ConditionContainer, out firstTime);
            critaria.ConditionContainer.Tree.Id = idGenerator.GetId(critaria.ConditionContainer.Tree, out firstTime);
            return critaria;
        }

        private static Expression GetConditionExpression(ConditionTree conditionForConvert, Type parameterExpressionType,
            ParameterExpression parameterExpression)
        {
            Expression result;
            object valueObject;
            Expression[] expressionArray;
            string serializedValue;
            object str;
            string serializedValue1;
            string str1;
            string serializedValue2;
            string str2;
            string serializedValue3;
            string str3;
            if (conditionForConvert == null)
            {
                throw new ArgumentNullException("conditionForConvert", "Condition tree is null");
            }
            if (conditionForConvert.OperationType == Operator.None &&
                string.Equals(conditionForConvert.SerializedValue, 1.ToString(CultureInfo.InvariantCulture),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                var constantExpression = Expression.Constant(1, typeof(int));
                return Expression.Equal(constantExpression, constantExpression);
            }
            if (conditionForConvert.OperationType == Operator.None &&
                string.Equals(conditionForConvert.SerializedValue, 2.ToString(CultureInfo.InvariantCulture),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                var constantExpression = Expression.Constant(1, typeof(int));
                return Expression.NotEqual(constantExpression, constantExpression);
            }
            ConstantExpression rightSide = null;
            Expression collection = null;
            var leftSidePropertyType = GetTargetPropertyType(parameterExpressionType,
                conditionForConvert.SelectorString);
            var isNumericType = leftSidePropertyType.IsNumericType();
            var isString = leftSidePropertyType == stringType;
            MethodInfo trimMethodInfo = null;
            MethodInfo trimStartMethodInfo = null;
            MethodInfo trimEndMethodInfo = null;
            MethodInfo startsWithMethodInfo = null;
            MethodInfo endsWithMethodInfo = null;
            MethodInfo containsMethodInfo = null;
            MethodInfo stringCompareMethodInfo = null;
            Expression argumantsExpression = null;
            var leftSile = GetLeftSide(conditionForConvert.SelectorString, parameterExpressionType,
                parameterExpression);
            switch (conditionForConvert.OperationType)
            {
                case Operator.Contain:
                case Operator.NotContain:
                    {
                        if (!string.IsNullOrEmpty(conditionForConvert.SerializedValue))
                        {
                            serializedValue = conditionForConvert.SerializedValue;
                        }
                        else
                        {
                            serializedValue = null;
                        }

                        if (leftSidePropertyType == stringType)
                        {
                            throw new InvalidOperationException("包含字符串的查询请使用 Like 系列操作符！");
                        }

                        Type numbericGenericType = typeof(ICollection<>).MakeGenericType(leftSidePropertyType);
                        valueObject = JsonConvert.DeserializeObject(serializedValue, numbericGenericType);
                        var typeArray = new Type[] { leftSidePropertyType };
                        containsMethodInfo = numbericGenericType.GetMethod("Contains", typeArray);
                        collection = Expression.Constant(valueObject);

                        break;
                    }
                case Operator.Like:
                case Operator.NotLike:
                    {
                        var type = typeof(string);
                        var typeArray1 = new Type[] { typeof(string) };
                        containsMethodInfo = type.GetMethod("Contains", typeArray1);
                        trimMethodInfo = typeof(string).GetMethod("Trim", new Type[0]);
                        if (!string.IsNullOrEmpty(conditionForConvert.SerializedValue))
                        {
                            serializedValue1 = conditionForConvert.SerializedValue;
                        }
                        else
                        {
                            serializedValue1 = null;
                        }
                        valueObject = JsonConvert.DeserializeObject(serializedValue1, leftSidePropertyType);
                        rightSide = Expression.Constant(valueObject, leftSidePropertyType);
                        break;
                    }
                case Operator.StartsWith:
                case Operator.NotStartsWith:
                    {
                        var type1 = typeof(string);
                        var typeArray2 = new Type[] { typeof(string) };
                        startsWithMethodInfo = type1.GetMethod("StartsWith", typeArray2);
                        trimStartMethodInfo = typeof(string).GetMethod("TrimStart",
                            BindingFlags.Instance | BindingFlags.Public);
                        argumantsExpression = Expression.NewArrayInit(typeof(char), new Expression[0]);
                        if (!string.IsNullOrEmpty(conditionForConvert.SerializedValue))
                        {
                            str1 = conditionForConvert.SerializedValue;
                        }
                        else
                        {
                            str1 = null;
                        }
                        valueObject = JsonConvert.DeserializeObject(str1, leftSidePropertyType);
                        rightSide = Expression.Constant(valueObject, leftSidePropertyType);
                        break;
                    }
                case Operator.EndsWith:
                case Operator.NotEndsWith:
                    {
                        var type2 = typeof(string);
                        var typeArray3 = new Type[] { typeof(string) };
                        endsWithMethodInfo = type2.GetMethod("EndsWith", typeArray3);
                        trimEndMethodInfo = typeof(string).GetMethod("TrimEnd", BindingFlags.Instance | BindingFlags.Public);
                        argumantsExpression = Expression.NewArrayInit(typeof(char), new Expression[0]);
                        if (!string.IsNullOrEmpty(conditionForConvert.SerializedValue))
                        {
                            serializedValue2 = conditionForConvert.SerializedValue;
                        }
                        else
                        {
                            serializedValue2 = null;
                        }
                        valueObject = JsonConvert.DeserializeObject(serializedValue2, leftSidePropertyType);
                        rightSide = Expression.Constant(valueObject, leftSidePropertyType);
                        break;
                    }
                case Operator.GreaterThan:
                case Operator.GreaterThanOrEqual:
                case Operator.LessThan:
                case Operator.LessThanOrEqual:
                    {
                        if (!isString)
                        {
                            if (!string.IsNullOrEmpty(conditionForConvert.SerializedValue))
                            {
                                str2 = conditionForConvert.SerializedValue;
                            }
                            else
                            {
                                str2 = null;
                            }
                            valueObject = JsonConvert.DeserializeObject(str2, leftSidePropertyType);
                            rightSide = Expression.Constant(valueObject, leftSidePropertyType);
                            break;
                        }
                        else
                        {
                            var type3 = typeof(string);
                            var typeArray4 = new Type[] { typeof(string) };
                            stringCompareMethodInfo = type3.GetMethod("CompareTo", typeArray4);
                            if (!string.IsNullOrEmpty(conditionForConvert.SerializedValue))
                            {
                                serializedValue3 = conditionForConvert.SerializedValue;
                            }
                            else
                            {
                                serializedValue3 = null;
                            }
                            valueObject = JsonConvert.DeserializeObject(serializedValue3, leftSidePropertyType);
                            rightSide = Expression.Constant(valueObject, leftSidePropertyType);
                            break;
                        }
                    }
                case Operator.IsNull:
                case Operator.IsNotNull:
                    {
                        rightSide = Expression.Constant(null);
                        valueObject = null;
                        break;
                    }
                default:
                    {
                        if (!string.IsNullOrEmpty(conditionForConvert.SerializedValue))
                        {
                            str3 = conditionForConvert.SerializedValue;
                        }
                        else
                        {
                            str3 = null;
                        }
                        valueObject = JsonConvert.DeserializeObject(str3, leftSidePropertyType);
                        rightSide = Expression.Constant(valueObject, leftSidePropertyType);
                        break;
                    }
            }
            UnaryExpression unaryExpression = null;
            ConstantExpression convertedRightSideConstantExpression = null;
            var nullableDoubleType = typeof(double?);
            MethodInfo stringConvertMethodInfo = null;
            if (isNumericType)
            {
                //Type type4 = typeof(SqlFunctions);
                //Type[] typeArray5 = { nullableDoubleType };
                //stringConvertMethodInfo = typeof(SqlFunctions).GetMethod("StringConvert", new[] { nullableDoubleType });

                stringConvertMethodInfo = typeof(Criteria<>).GetMethod("StringConvert", new[] { nullableDoubleType });

                unaryExpression = Expression.Convert(leftSile, nullableDoubleType);
                if (valueObject == null)
                {
                    str = null;
                }
                else
                {
                    str = valueObject.ToString();
                }
                convertedRightSideConstantExpression = Expression.Constant(str);
            }
            switch (conditionForConvert.OperationType)
            {
                case Operator.Equal:
                case Operator.IsNull:
                    {
                        result = Expression.Equal(leftSile, rightSide);
                        break;
                    }
                case Operator.NotEqual:
                case Operator.IsNotNull:
                    {
                        result = Expression.NotEqual(leftSile, rightSide);
                        break;
                    }
                case Operator.Contain:
                    {
                        expressionArray = new Expression[] { leftSile };
                        result = Expression.Call(collection, containsMethodInfo, expressionArray);
                        break;
                    }
                case Operator.NotContain:
                    {
                        var expressionArray1 = new Expression[] { leftSile };
                        result = Expression.Not(Expression.Call(collection, containsMethodInfo, expressionArray1));
                        break;
                    }
                case Operator.Like:
                    {
                        if (!isNumericType)
                        {
                            var expressionArray2 = new Expression[] { rightSide };
                            result = Expression.Call(leftSile, containsMethodInfo, expressionArray2);
                            break;
                        }
                        else
                        {
                            var methodCallExpression1 = Expression.Call(stringConvertMethodInfo,
                                unaryExpression);
                            var methodCallExpression2 = Expression.Call(methodCallExpression1,
                                trimMethodInfo);
                            var expressionArray3 = new Expression[] { convertedRightSideConstantExpression };
                            result = Expression.Call(methodCallExpression2, containsMethodInfo, expressionArray3);
                            break;
                        }
                    }
                case Operator.NotLike:
                    {
                        if (!isNumericType)
                        {
                            var expressionArray4 = new Expression[] { rightSide };
                            result = Expression.Not(Expression.Call(leftSile, containsMethodInfo, expressionArray4));
                            break;
                        }
                        else
                        {
                            var methodCallExpression1 = Expression.Call(stringConvertMethodInfo,
                                unaryExpression);
                            var methodCallExpression2 = Expression.Call(methodCallExpression1,
                                trimMethodInfo);
                            var expressionArray5 = new Expression[] { convertedRightSideConstantExpression };
                            result = Expression.Call(methodCallExpression2, containsMethodInfo, expressionArray5);
                            result = Expression.Not(result);
                            break;
                        }
                    }
                case Operator.StartsWith:
                    {
                        if (!isNumericType)
                        {
                            var expressionArray6 = new Expression[] { rightSide };
                            result = Expression.Call(leftSile, startsWithMethodInfo, expressionArray6);
                            break;
                        }
                        else
                        {
                            var methodCallExpression1 = Expression.Call(stringConvertMethodInfo,
                                unaryExpression);
                            var expressionArray7 = new Expression[] { argumantsExpression };
                            var methodCallExpression2 = Expression.Call(methodCallExpression1,
                                trimStartMethodInfo, expressionArray7);
                            var expressionArray8 = new Expression[] { convertedRightSideConstantExpression };
                            result = Expression.Call(methodCallExpression2, startsWithMethodInfo, expressionArray8);
                            break;
                        }
                    }
                case Operator.NotStartsWith:
                    {
                        if (!isNumericType)
                        {
                            var expressionArray9 = new Expression[] { rightSide };
                            result = Expression.Not(Expression.Call(leftSile, startsWithMethodInfo, expressionArray9));
                            break;
                        }
                        else
                        {
                            var methodCallExpression1 = Expression.Call(stringConvertMethodInfo,
                                unaryExpression);
                            var expressionArray10 = new Expression[] { argumantsExpression };
                            var methodCallExpression2 = Expression.Call(methodCallExpression1,
                                trimStartMethodInfo, expressionArray10);
                            var expressionArray11 = new Expression[] { convertedRightSideConstantExpression };
                            result = Expression.Call(methodCallExpression2, startsWithMethodInfo, expressionArray11);
                            result = Expression.Not(result);
                            break;
                        }
                    }
                case Operator.EndsWith:
                    {
                        if (!isNumericType)
                        {
                            expressionArray = new Expression[] { rightSide };
                            result = Expression.Call(leftSile, endsWithMethodInfo, expressionArray);
                            break;
                        }
                        else
                        {
                            var methodCallExpression1 = Expression.Call(stringConvertMethodInfo,
                                unaryExpression);
                            expressionArray = new Expression[] { argumantsExpression };
                            var methodCallExpression2 = Expression.Call(methodCallExpression1,
                                trimEndMethodInfo, expressionArray);
                            expressionArray = new Expression[] { convertedRightSideConstantExpression };
                            result = Expression.Call(methodCallExpression2, endsWithMethodInfo, expressionArray);
                            break;
                        }
                    }
                case Operator.NotEndsWith:
                    {
                        if (!isNumericType)
                        {
                            expressionArray = new Expression[] { rightSide };
                            result = Expression.Not(Expression.Call(leftSile, endsWithMethodInfo, expressionArray));
                            break;
                        }
                        else
                        {
                            var methodCallExpression1 = Expression.Call(stringConvertMethodInfo,
                                unaryExpression);
                            expressionArray = new Expression[] { argumantsExpression };
                            var methodCallExpression2 = Expression.Call(methodCallExpression1,
                                trimEndMethodInfo, expressionArray);
                            expressionArray = new Expression[] { convertedRightSideConstantExpression };
                            result = Expression.Call(methodCallExpression2, endsWithMethodInfo, expressionArray);
                            result = Expression.Not(result);
                            break;
                        }
                    }
                case Operator.GreaterThan:
                    {
                        if (!isString)
                        {
                            result = Expression.GreaterThan(leftSile, rightSide);
                            break;
                        }
                        else
                        {
                            expressionArray = new Expression[] { rightSide };
                            result =
                                Expression.GreaterThan(Expression.Call(leftSile, stringCompareMethodInfo, expressionArray),
                                    Expression.Constant(0));
                            break;
                        }
                    }
                case Operator.GreaterThanOrEqual:
                    {
                        if (!isString)
                        {
                            result = Expression.GreaterThanOrEqual(leftSile, rightSide);
                            break;
                        }
                        else
                        {
                            expressionArray = new Expression[] { rightSide };
                            result =
                                Expression.GreaterThanOrEqual(
                                    Expression.Call(leftSile, stringCompareMethodInfo, expressionArray),
                                    Expression.Constant(0));
                            break;
                        }
                    }
                case Operator.LessThan:
                    {
                        if (!isString)
                        {
                            result = Expression.LessThan(leftSile, rightSide);
                            break;
                        }
                        else
                        {
                            expressionArray = new Expression[] { rightSide };
                            result = Expression.LessThan(
                                Expression.Call(leftSile, stringCompareMethodInfo, expressionArray), Expression.Constant(0));
                            break;
                        }
                    }
                case Operator.LessThanOrEqual:
                    {
                        if (!isString)
                        {
                            result = Expression.LessThanOrEqual(leftSile, rightSide);
                            break;
                        }
                        else
                        {
                            expressionArray = new Expression[] { rightSide };
                            result =
                                Expression.LessThanOrEqual(
                                    Expression.Call(leftSile, stringCompareMethodInfo, expressionArray),
                                    Expression.Constant(0));
                            break;
                        }
                    }
                default:
                    {
                        throw new ArgumentException("Argument is not valid beacuse of operation type", "conditionForConvert");
                    }
            }
            return result;
        }

        public Expression<Func<TEntity, bool>> GetExpression()
        {
            checkedIds = new List<double>();
            var entityType = typeof(TEntity);
            var parameterExpression = Expression.Parameter(entityType, "entity");
            var resultExpression = ConvertConditionToExpresion(ConditionContainer.Tree, entityType,
                parameterExpression);
            return Expression.Lambda<Func<TEntity, bool>>(resultExpression,
                new ParameterExpression[] { parameterExpression });
        }

        private static string GetInvariantCultrueString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return input.ToString(CultureInfo.InvariantCulture);
        }

        public static string StringConvert(double? number)
        {
            throw new NotSupportedException(string.Format("StringConvert:{0}", number));
        }

        private static Expression GetLeftSide(string selectorString, Type parameterExpressionType,
            ParameterExpression parameterExpression)
        {
            if (string.IsNullOrWhiteSpace(selectorString))
            {
                throw new ArgumentNullException("selectorString", "Selector string is not valid");
            }
            var propertyParts = selectorString.Split(new char[] { '.' });
            if (propertyParts.Any(string.IsNullOrWhiteSpace))
            {
                throw new Exception(string.Format("Selector string \"{0}\" format is not valid.", selectorString));
            }
            var firstPartOfSelector = GetInvariantCultrueString(propertyParts[0]);
            var propertyInThisType = parameterExpressionType.GetProperty(firstPartOfSelector);
            if (propertyInThisType == null)
            {
                throw new Exception(string.Format("Selector string \"{0}\" is not exist in type \"{1}\".",
                    selectorString, parameterExpressionType.Name));
            }
            Expression expression = Expression.Property(parameterExpression, propertyInThisType);
            if (propertyParts.Length == 1)
            {
                return expression;
            }
            return GetLeftSide(string.Join(".", propertyParts, 1, propertyParts.Length - 1), expression);
        }

        private static Expression GetLeftSide(string selectorString, Expression inputExpression)
        {
            Expression resultExpression;
            var inputExpressionType = inputExpression.Type;
            if (string.IsNullOrWhiteSpace(selectorString))
            {
                throw new ArgumentNullException("selectorString", "Selector string is not valid");
            }
            var propertyParts = selectorString.Split(new char[] { '.' });
            if (propertyParts.Any<string>(new Func<string, bool>(string.IsNullOrWhiteSpace)))
            {
                throw new Exception(string.Format("Selector string \"{0}\" format is not valid.", selectorString));
            }
            var firstPartOfSelector = GetInvariantCultrueString(propertyParts[0]);
            PropertyInfo selectedPropertyInfo = null;
            MethodInfo selectedMethodInfo = null;
            if (firstPartOfSelector.IndexOf('(') <= 0)
            {
                selectedPropertyInfo = inputExpressionType.GetProperty(firstPartOfSelector);
                resultExpression = Expression.Property(inputExpression, selectedPropertyInfo);
            }
            else
            {
                var methodName = firstPartOfSelector.Remove(firstPartOfSelector.IndexOf('('));
                selectedMethodInfo = inputExpressionType.GetMethod(methodName, new Type[0]);
                resultExpression = Expression.Call(inputExpression, selectedMethodInfo);
            }
            if (selectedPropertyInfo == null && selectedMethodInfo == null)
            {
                throw new Exception(string.Format("Selector string \"{0}\" is not exist in type \"{1}\".",
                    selectorString, inputExpression.Type.Name));
            }
            if (propertyParts.Length != 1)
            {
                resultExpression = GetLeftSide(string.Join(".", propertyParts, 1, propertyParts.Length - 1),
                    resultExpression);
            }
            return resultExpression;
        }

        private static string GetSelectorStringFromExpression<TProperty>(
            Expression<Func<TEntity, TProperty>> selectorExpression)
        {
            var selectorString = selectorExpression.Body.ToString();
            return selectorString.Remove(0, selectorString.IndexOf('.') + 1);
        }

        private static Type GetTargetPropertyType(Type entityType, string selectorString)
        {
            if (string.IsNullOrWhiteSpace(selectorString))
            {
                return null;
            }
            var chrArray = new char[] { '.' };

            var propertyParts =
                selectorString.Split(chrArray, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => x.Trim()).ToArray();

            var firstPartOfSelector = propertyParts[0].ToString(CultureInfo.InvariantCulture);
            PropertyInfo selectedPropertyInfo = null;
            MethodInfo selectedMethodInfo = null;
            if (firstPartOfSelector.IndexOf('(') <= 0)
            {
                selectedPropertyInfo = entityType.GetProperty(firstPartOfSelector);
            }
            else
            {
                var methodName = firstPartOfSelector.Remove(firstPartOfSelector.IndexOf('('));
                selectedMethodInfo = entityType.GetMethod(methodName, new Type[0]);
            }
            if (selectedPropertyInfo == null && selectedMethodInfo == null)
            {
                throw new Exception(string.Format("Selector string \"{0}\" is not exist in type \"{1}\".",
                    selectorString, entityType.Name));
            }
            if (propertyParts.Length == 1)
            {
                if (selectedPropertyInfo != null)
                {
                    return selectedPropertyInfo.PropertyType;
                }
                return selectedMethodInfo.ReturnType;
            }
            return GetTargetPropertyType(selectedPropertyInfo.PropertyType,
                string.Join(".", propertyParts, 1, propertyParts.Length - 1));
        }

        public Criteria<TEntity> Or(Criteria<TEntity> critaria)
        {
            if (critaria == null)
            {
                return this;
            }
            if (entityType != critaria.entityType)
            {
                throw new Exception(string.Format("critaria must be from '{0}' type", entityType.Assembly.FullName));
            }
            ConditionContainer.Tree.NextLogicalOperator = LogicalOperator.Or;
            ConditionContainer.Tree.Children.Add(critaria.ConditionContainer.Tree);
            return this;
        }

        public Criteria<TEntity> Or(string selectorString, Operator operationType, object value)
        {
            if (string.IsNullOrWhiteSpace(selectorString))
            {
                throw new ArgumentException("Selector string can not be null or empty", "selectorString");
            }
            var targetPropertyType = GetTargetPropertyType(entityType, selectorString);
            if (targetPropertyType != value.GetType() && operationType != Operator.Contain &&
                operationType != Operator.NotContain)
            {
                value = ChangeValueType(targetPropertyType, value);
            }
            var conditionTree = new ConditionTree()
            {
                OperationType = operationType,
                Value = value,
                NextLogicalOperator = LogicalOperator.Or,
                SelectorString = selectorString
            };
            var newConditionTree = conditionTree;
            newConditionTree.Id = idGenerator.GetId(newConditionTree, out firstTime);
            ConditionContainer.Tree.Children.Add(newConditionTree);
            return this;
        }

        public Criteria<TEntity> Or<TProperty>(Expression<Func<TEntity, TProperty>> selectorExpression,
            Operator operationType, object value)
        {
            if (selectorExpression == null)
            {
                throw new ArgumentException("Selector string can not be null or empty", "selectorExpression");
            }
            var conditionTree = new ConditionTree()
            {
                OperationType = operationType,
                Value = value,
                NextLogicalOperator = LogicalOperator.Or,
                SelectorString = GetSelectorStringFromExpression(selectorExpression)
            };
            var newConditionTree = conditionTree;
            newConditionTree.Id = idGenerator.GetId(newConditionTree, out firstTime);
            ConditionContainer.Tree.Children.Add(newConditionTree);
            return this;
        }

        public static Criteria<TEntity> True()
        {
            var entityType = typeof(TEntity);
            var criterium = new Criteria<TEntity>();
            var condition = new Condition();
            var conditionTree = new ConditionTree()
            {
                OperationType = Operator.None,
                NextLogicalOperator = LogicalOperator.And,
                Value = TrueFalse.True
            };
            condition.Tree = conditionTree;
            condition.EntityTypeName = entityType.Name;
            criterium.ConditionContainer = condition;
            criterium.entityType = entityType;
            var critaria = criterium;
            critaria.ConditionContainer.Id = idGenerator.GetId(critaria.ConditionContainer, out firstTime);
            critaria.ConditionContainer.Tree.Id = idGenerator.GetId(critaria.ConditionContainer.Tree, out firstTime);
            return critaria;
        }

        public Expression<Func<TDestination, bool>> TypedGetExpression<TDestination>()
            where TDestination : class
        {
            checkedIds = new List<double>();
            var entityType = typeof(TDestination);
            var parameterExpression = Expression.Parameter(entityType, "entity");
            var resultExpression = ConvertConditionToExpresion(ConditionContainer.Tree, entityType,
                parameterExpression);
            return Expression.Lambda<Func<TDestination, bool>>(resultExpression,
                new ParameterExpression[] { parameterExpression });
        }
    }
}