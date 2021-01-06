using System.Web.Http;
using Abp.Dependency;
using Castle.MicroKernel.Registration;
using System.Reflection;

namespace Abp.WebApi.Controllers
{
    /// <summary>
    /// Registers all Web API Controllers derived from <see cref="ApiController"/>.
    /// </summary>
    public class ApiControllerConventionalRegistrar : IConventionalDependencyRegistrar
    {
        /// <summary>
        /// 所有的api控制器注入，生命周期为瞬时
        /// </summary>
        /// <param name="context"></param>
        public void RegisterAssembly(IConventionalRegistrationContext context)
        {
            context.IocManager.IocContainer.Register(
                Classes.FromAssembly(context.Assembly)
                    .BasedOn<ApiController>()
                    .If(type => !type.GetTypeInfo().IsGenericTypeDefinition)
                    .LifestyleTransient()
                );
        }
    }
}
