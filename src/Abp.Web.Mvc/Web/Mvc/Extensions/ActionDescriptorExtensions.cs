using System.Reflection;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using Abp.Extensions;

namespace Abp.Web.Mvc.Extensions
{
    public static class ActionDescriptorExtensions
    {
        /// <summary>
        /// 常规方法，async结尾方法，返回值是Task或泛型的方法
        /// </summary>
        /// <param name="actionDescriptor"></param>
        /// <returns></returns>
        public static MethodInfo GetMethodInfoOrNull(this ActionDescriptor actionDescriptor)
        {
            //常规action方法，返回其反射类型
            if (actionDescriptor is ReflectedActionDescriptor)
            {
                return actionDescriptor.As<ReflectedActionDescriptor>().MethodInfo;
            }
            //异步action方法(方法名以async结尾)返回，其反射类型
            if (actionDescriptor is ReflectedAsyncActionDescriptor)
            {
                return actionDescriptor.As<ReflectedAsyncActionDescriptor>().MethodInfo;
            }
            //返回值类型是Task或者Task<T>类型的action
            if (actionDescriptor is TaskAsyncActionDescriptor)
            {
                return actionDescriptor.As<TaskAsyncActionDescriptor>().MethodInfo;
            }

            return null;
        }
    }
}