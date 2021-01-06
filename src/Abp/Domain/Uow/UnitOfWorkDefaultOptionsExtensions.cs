using System;
using System.Linq;
using System.Reflection;

namespace Abp.Domain.Uow
{
    internal static class UnitOfWorkDefaultOptionsExtensions
    {
        public static UnitOfWorkAttribute GetUnitOfWorkAttributeOrNull(this IUnitOfWorkDefaultOptions unitOfWorkDefaultOptions, MethodInfo methodInfo)
        {
            //方法上是否定义了UnitOfWorkAttribute，有则直接返回
            var attrs = methodInfo.GetCustomAttributes(true).OfType<UnitOfWorkAttribute>().ToArray();
            if (attrs.Length > 0)
            {
                return attrs[0];
            }
            //方法所属的类是否定义了UnitOfWorkAttribute，有则直接返回
            attrs = methodInfo.DeclaringType.GetTypeInfo().GetCustomAttributes(true).OfType<UnitOfWorkAttribute>().ToArray();
            if (attrs.Length > 0)
            {
                return attrs[0];
            }
            //用户是否通过配置给该类加上UnitOfWorkAttribute，有就返回一个实例
            if (unitOfWorkDefaultOptions.IsConventionalUowClass(methodInfo.DeclaringType))
            {
                return new UnitOfWorkAttribute(); //Default
            }
            //表明该方法没有定义UnitOfWorkAttribute
            return null;
        }
        /// <summary>
        /// 读取用户配置判断是否有设置UnitOfWorkAttribute
        /// </summary>
        /// <param name="unitOfWorkDefaultOptions"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsConventionalUowClass(this IUnitOfWorkDefaultOptions unitOfWorkDefaultOptions, Type type)
        {
            return unitOfWorkDefaultOptions.ConventionalUowSelectors.Any(selector => selector(type));
        }
    }
}
