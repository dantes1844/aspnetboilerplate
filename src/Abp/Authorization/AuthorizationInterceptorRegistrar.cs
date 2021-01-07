using System;
using System.Linq;
using System.Reflection;
using Abp.Application.Features;
using Abp.Dependency;
using Castle.Core;
using Castle.MicroKernel;

namespace Abp.Authorization
{
    /// <summary>
    /// This class is used to register interceptors on the Application Layer.
    /// </summary>
    internal static class AuthorizationInterceptorRegistrar
    {
        public static void Initialize(IIocManager iocManager)
        {
            iocManager.IocContainer.Kernel.ComponentRegistered += Kernel_ComponentRegistered;
        }

        private static void Kernel_ComponentRegistered(string key, IHandler handler)
        {
            if (ShouldIntercept(handler.ComponentModel.Implementation))
            {
                //这里与AbpKernelModule中的 RegisterInterceptors有什么不一样呢
                handler.ComponentModel.Interceptors.Add(new InterceptorReference(typeof(AbpAsyncDeterminationInterceptor<AuthorizationInterceptor>)));
            }
        }

        private static bool ShouldIntercept(Type type)
        {
            if (SelfOrMethodsDefinesAttribute<AbpAuthorizeAttribute>(type))
            {
                return true;
            }

            if (SelfOrMethodsDefinesAttribute<RequiresFeatureAttribute>(type))
            {
                return true;
            }

            return false;
        }

        private static bool SelfOrMethodsDefinesAttribute<TAttr>(Type type)
        {
            //如果自己本身就定义了该特性，直接返回
            if (type.GetTypeInfo().IsDefined(typeof(TAttr), true))
            {
                return true;
            }

            //判断类中的任何属性或者公开、私有的方法是否定义了相关的特性
            return type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(m => m.IsDefined(typeof(TAttr), true));
        }
    }
}