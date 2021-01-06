using Abp.Web.Configuration;

namespace Abp.Configuration.Startup
{
    /// <summary>
    /// Defines extension methods to <see cref="IModuleConfigurations"/> to allow to configure ABP Web module.
    /// </summary>
    public static class AbpWebConfigurationExtensions
    {
        /// <summary>
        /// Used to configure ABP Web Common module.
        /// <para>将模块的配置项以方法的形式暴露出去，供其他模块进行设置用</para>
        /// </summary>
        public static IAbpWebCommonModuleConfiguration AbpWebCommon(this IModuleConfigurations configurations)
        {
            return configurations.AbpConfiguration.Get<IAbpWebCommonModuleConfiguration>();
        }
    }
}