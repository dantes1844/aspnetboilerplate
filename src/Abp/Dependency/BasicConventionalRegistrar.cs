using System.Reflection;
using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;

namespace Abp.Dependency
{
    /// <summary>
    /// This class is used to register basic dependency implementations such as <see cref="ITransientDependency"/> and <see cref="ISingletonDependency"/>.
    /// </summary>
    public class BasicConventionalRegistrar : IConventionalDependencyRegistrar
    {
        //全局的根据接口来进行注入操作的方法。
        public void RegisterAssembly(IConventionalRegistrationContext context)
        { 
            /*
            Windsor文档：
             https://github.com/castleproject/Windsor/blob/master/docs/registering-components-by-conventions.md
           
             * It will look at all the interfaces implemented by selected types,
             * and use as type's services these that have matching names. Matching names,
             * means that the implementing class contains in its name the name of the interface (without the I on the front).
             *
             * Transient，瞬时对象。
             * ApplicationService都是走这一步进行注入的，并且根据名称进行匹配，
             * 名称匹配的原则简单来说就是接口名称去掉I之后的部分必须完整的包含在实现类中。
            */
            context.IocManager.IocContainer.Register(
                Classes.FromAssembly(context.Assembly)
                    .IncludeNonPublicTypes()
                    .BasedOn<ITransientDependency>()
                    .If(type => !type.GetTypeInfo().IsGenericTypeDefinition)
                    .WithService.Self()
                    .WithService.DefaultInterfaces()
                    .LifestyleTransient()
                );

            //Singleton
            context.IocManager.IocContainer.Register(
                Classes.FromAssembly(context.Assembly)
                    .IncludeNonPublicTypes()
                    .BasedOn<ISingletonDependency>()
                    .If(type => !type.GetTypeInfo().IsGenericTypeDefinition)
                    .WithService.Self()
                    .WithService.DefaultInterfaces()
                    .LifestyleSingleton()
                );

            //Windsor Interceptors
            context.IocManager.IocContainer.Register(
                Classes.FromAssembly(context.Assembly)
                    .IncludeNonPublicTypes()
                    .BasedOn<IInterceptor>()
                    .If(type => !type.GetTypeInfo().IsGenericTypeDefinition)
                    .WithService.Self()
                    .LifestyleTransient()
                );
        }
    }
}