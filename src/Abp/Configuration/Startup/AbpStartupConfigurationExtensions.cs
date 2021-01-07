using System;
using Abp.Dependency;

namespace Abp.Configuration.Startup
{
    /// <summary>
    /// Extension methods for <see cref="IAbpStartupConfiguration"/>.
    /// </summary>
    public static class AbpStartupConfigurationExtensions
    {
        /// <summary>
        /// Used to replace a service type.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="type">Type.</param>
        /// <param name="impl">Implementation.</param>
        /// <param name="lifeStyle">Life style.</param>
        public static void ReplaceService(this IAbpStartupConfiguration configuration, Type type, Type impl, DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton)
        {
            configuration.ReplaceService(type, () =>
            {
                configuration.IocManager.Register(type, impl, lifeStyle);
            });
        }

        /// <summary>
        /// Used to replace a service type.
        /// </summary>
        /// <typeparam name="TType">Type of the service.</typeparam>
        /// <typeparam name="TImpl">Type of the implementation.</typeparam>
        /// <param name="configuration">The configuration.</param>
        /// <param name="lifeStyle">Life style.</param>
        public static void ReplaceService<TType, TImpl>(this IAbpStartupConfiguration configuration, DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton)
            where TType : class
            where TImpl : class, TType
        {
            configuration.ReplaceService(typeof(TType), () =>
            {
                //官网说的是先注册的作为实际绑定。那么这里再次注册为什么能替换掉之前的呢？也没有调用用IsDefault()
                configuration.IocManager.Register<TType, TImpl>(lifeStyle);
            });
        }


        /// <summary>
        /// Used to replace a service type.
        /// </summary>
        /// <typeparam name="TType">Type of the service.</typeparam>
        /// <param name="configuration">The configuration.</param>
        /// <param name="replaceAction">Replace action.</param>
        public static void ReplaceService<TType>(this IAbpStartupConfiguration configuration, Action replaceAction)
            where TType : class
        {
            configuration.ReplaceService(typeof(TType), replaceAction);
        }
    }
}