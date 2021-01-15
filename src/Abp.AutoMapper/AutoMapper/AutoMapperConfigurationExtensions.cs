using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Abp.Reflection.Extensions;
using AutoMapper;

namespace Abp.AutoMapper
{
    internal static class AutoMapperConfigurationExtensions
    {
        private static readonly object SyncObj = new object();

        /// <summary>
        /// 创建自动映射
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="type">被打标签的类</param>
        public static void CreateAutoAttributeMaps(this IMapperConfigurationExpression configuration, Type type)
        {
            //TODO 这里为什么需要锁？貌似没有并发操作？ 2021年1月14日 14:34:42
            lock (SyncObj)
            {
                //遍历所有的自动映射标签，将它们与目标类进行映射
                foreach (var autoMapAttribute in type.GetTypeInfo().GetCustomAttributes<AutoMapAttributeBase>())
                {
                    autoMapAttribute.CreateMap(configuration, type);
                }   
            }
        }

        /// <summary>
        /// 创建映射
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="type">原始类</param>
        /// <param name="targetTypes">目标类列表</param>
        /// <param name="memberList">成员类型，是Source还是Destination，用来验证字段已经全部映射完毕</param>
        public static void CreateAutoAttributeMaps(this IMapperConfigurationExpression configuration, Type type, Type[] targetTypes, MemberList memberList)
        {
            //Get all the properties in the source that have the AutoMapKeyAttribute
            var sourceKeysPropertyInfo = type.GetProperties()
                                             .Where(w => w.GetCustomAttribute<AutoMapKeyAttribute>() != null)
                                             .Select(s => s).ToList();

            foreach (var targetType in targetTypes)
            {
                //这里表示，当前类打了自动映射的标签，且类里面的字段都没有打映射主键标签，那么当前类就直接进行映射
                if (!sourceKeysPropertyInfo.Any())
                {
                    configuration.CreateMap(type, targetType, memberList);
                    continue;
                }

                BinaryExpression equalityComparer = null;

                //In a lambda expression represent the source example : (source) => ...
                ParameterExpression sourceParameterExpression = Expression.Parameter(type, "source");
                //In a lambda expression represent the target example : (target) => ...
                ParameterExpression targetParameterExpression = Expression.Parameter(targetType, "target");


                //We could use multiple AutoMapKey to compare the determine equality
                //遍历那些打了自动映射键的字段，根据属性的键进行匹配，匹配上了的才进行赋值
                foreach (PropertyInfo propertyInfo in sourceKeysPropertyInfo)
                {
                    //In a lambda expression represent a specific property of a parameter example : (source) => source.Id
                    MemberExpression sourcePropertyExpression = Expression.Property(sourceParameterExpression, propertyInfo);

                    //Find the target a property with the same name to compare with
                    //Example if we have in source the attribute AutoMapKey on the Property Id we want to get Id in the target to compare against
                    var targetPropertyInfo = targetType.GetProperty(sourcePropertyExpression.Member.Name);

                    //It happen if the property with AutoMapKeyAttribute does not exist in target
                    if (targetPropertyInfo is null)
                    {
                        continue;
                    }

                    //In a lambda expression represent a specific property of a parameter example : (target) => target.Id
                    MemberExpression targetPropertyExpression = Expression.Property(targetParameterExpression, targetPropertyInfo);

                    //Compare the property defined by AutoMapKey in the source against the same property in the target
                    //Example (source, target) => source.Id == target.Id
                    BinaryExpression equal = Expression.Equal(sourcePropertyExpression, targetPropertyExpression);

                    if (equalityComparer is null)
                    {
                        equalityComparer = equal;
                    }
                    else
                    {
                        //If we compare multiple key we want to make an and condition between
                        //Example : (source, target) => source.Email == target.Email && source.UserName == target.UserName
                        equalityComparer = Expression.And(equalityComparer, equal);
                    }
                }

                //If there is not match for AutoMapKey in the target
                //In this case we add the default mapping
                if (equalityComparer is null)
                {
                    configuration.CreateMap(type, targetType, memberList);
                    continue;
                }

                //We need to make a generic type of Func<SourceType, TargetType, bool> to invoke later Expression.Lambda
                var funcGenericType = typeof(Func<,,>).MakeGenericType(type, targetType, typeof(bool));

                //Make a method info of Expression.Lambda<Func<SourceType, TargetType, bool>> to call later
                var lambdaMethodInfo = typeof(Expression).GetMethod("Lambda", 2, 1).MakeGenericMethod(funcGenericType);

                //Make the call to Expression.Lambda
                var expressionLambdaResult = lambdaMethodInfo.Invoke(null, new object[] { equalityComparer, new ParameterExpression[] { sourceParameterExpression, targetParameterExpression } });

                //Get the method info of IMapperConfigurationExpression.CreateMap<Source, Target>
                var createMapMethodInfo = configuration.GetType().GetMethod("CreateMap", 1, 2).MakeGenericMethod(type, targetType);

                //Make the call to configuration.CreateMap<Source, Target>().
                var createMapResult = createMapMethodInfo.Invoke(configuration, new object[] { memberList });

                var autoMapperCollectionAssembly = Assembly.Load("AutoMapper.Collection");

                var autoMapperCollectionTypes = autoMapperCollectionAssembly.GetTypes();

                var equalityComparisonGenericMethodInfo = autoMapperCollectionTypes
                                         .Where(w => !w.IsGenericType && !w.IsNested)
                                         .SelectMany(s => s.GetMethods()).Where(w => w.Name == "EqualityComparison")
                                         .FirstOrDefault()
                                         .MakeGenericMethod(type, targetType);

                //Make the call to EqualityComparison
                //Exemple configuration.CreateMap<Source, Target>().EqualityComparison((source, target) => source.Id == target.Id)
                equalityComparisonGenericMethodInfo.Invoke(createMapResult, new object[] { createMapResult, expressionLambdaResult });
            }
        }
    }
}