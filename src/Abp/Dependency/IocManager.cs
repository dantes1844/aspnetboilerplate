using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Castle.Windsor.Proxy;

namespace Abp.Dependency
{
    /// <summary>
    /// This class is used to directly perform dependency injection tasks.
    /// </summary>
    public class IocManager : IIocManager
    {
        /// <summary>
        /// The Singleton instance.
        /// </summary>
        public static IocManager Instance { get; private set; }

        /// <summary>
        /// Singletone instance for Castle ProxyGenerator.
        /// From Castle.Core documentation it is highly recommended to use single instance of ProxyGenerator to avoid memoryleaks and performance issues
        /// Follow next links for more details:
        /// <a href="https://github.com/castleproject/Core/blob/master/docs/dynamicproxy.md">Castle.Core documentation</a>,
        /// <a href="http://kozmic.net/2009/07/05/castle-dynamic-proxy-tutorial-part-xii-caching/">Article</a>
        /// </summary>
        private static readonly ProxyGenerator ProxyGeneratorInstance = new ProxyGenerator();

        /// <summary>
        /// Reference to the Castle Windsor Container.
        /// </summary>
        public IWindsorContainer IocContainer { get; private set; }

        /// <summary>
        /// List of all registered conventional registrars.
        /// </summary>
        private readonly List<IConventionalDependencyRegistrar> _conventionalRegistrars;

        static IocManager()
        {
            Instance = new IocManager();
        }

        /// <summary>
        /// Creates a new <see cref="IocManager"/> object.
        /// Normally, you don't directly instantiate an <see cref="IocManager"/>.
        /// This may be useful for test purposes.
        /// </summary>
        public IocManager()
        {
            IocContainer = CreateContainer();
            _conventionalRegistrars = new List<IConventionalDependencyRegistrar>();

            //Register self!
            IocContainer.Register(
                Component
                    .For<IocManager, IIocManager, IIocRegistrar, IIocResolver>()
                    .Instance(this)
            );
        }

        protected virtual IWindsorContainer CreateContainer()
        {
            //用一个单例的代理生成器生成容器
            return new WindsorContainer(new DefaultProxyFactory(ProxyGeneratorInstance));
        }

        /// <summary>
        /// Adds a dependency registrar for conventional registration.
        /// </summary>
        /// <param name="registrar">dependency registrar</param>
        public void AddConventionalRegistrar(IConventionalDependencyRegistrar registrar)
        {
            _conventionalRegistrars.Add(registrar);
        }

        /// <summary>
        /// Registers types of given assembly by all conventional registrars. See <see cref="AddConventionalRegistrar"/> method.
        /// </summary>
        /// <param name="assembly">Assembly to register</param>
        public void RegisterAssemblyByConvention(Assembly assembly)
        {
            RegisterAssemblyByConvention(assembly, new ConventionalRegistrationConfig());
        }

        /// <summary>
        /// Registers types of given assembly by all conventional registrars. See <see cref="AddConventionalRegistrar"/> method.
        /// </summary>
        /// <param name="assembly">Assembly to register</param>
        /// <param name="config">Additional configuration</param>
        public void RegisterAssemblyByConvention(Assembly assembly, ConventionalRegistrationConfig config)
        {
            var context = new ConventionalRegistrationContext(assembly, this, config);

            foreach (var registerer in _conventionalRegistrars)
            {
                registerer.RegisterAssembly(context);
            }

            if (config.InstallInstallers)
            {
                IocContainer.Install(FromAssembly.Instance(assembly));
            }
        }

        /// <summary>
        /// Registers a type as self registration.
        /// <para>将当前类注册为自己的实例，默认单例</para>
        /// <para>参考连接 https://github.com/castleproject/Windsor/blob/master/docs/registering-components-one-by-one.md </para>
        /// </summary>
        /// <typeparam name="TType">Type of the class</typeparam>
        /// <param name="lifeStyle">Lifestyle of the objects of this type</param>
        public void Register<TType>(DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton) where TType : class
        {
            //注册的对象必须是IRegistration
            //最简单的方法就是使用Component.For方法,它返回IRegistration对象实例
            IocContainer.Register(ApplyLifestyle(Component.For<TType>(), lifeStyle));
        }

        /// <summary>
        /// Registers a type as self registration.
        /// </summary>
        /// <param name="type">Type of the class</param>
        /// <param name="lifeStyle">Lifestyle of the objects of this type</param>
        public void Register(Type type, DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton)
        {
            IocContainer.Register(ApplyLifestyle(Component.For(type), lifeStyle));
        }

        /// <summary>
        /// Registers a type with it's implementation.
        /// <para>注册实现类为接口的实例，默认单例</para>
        /// </summary>
        /// <typeparam name="TType">Registering type</typeparam>
        /// <typeparam name="TImpl">The type that implements <typeparamref name="TType"/></typeparam>
        /// <param name="lifeStyle">Lifestyle of the objects of this type</param>
        public void Register<TType, TImpl>(DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton)
            where TType : class
            where TImpl : class, TType
        {
            //IocContainer.Register(ApplyLifestyle(Component.For(typeof(TType)).ImplementedBy(typeof(TImpl)), lifeStyle));
            //以上注释掉的为非泛型版本
            //一个接口可以同时注册多个类，但是以第一次注册的为准（AutoFac以最后一次注入的为准）
            //可以通过在最后一次的注册上调用.IsDefault()方法将其设置为默认的实现。
            //如果想让多个注入同时存在，给他们一个唯一的名字即可 Component.For<IMyService>().Named("OtherServiceImpl").ImplementedBy<OtherServiceImpl>()
            IocContainer.Register(ApplyLifestyle(Component.For<TType, TImpl>().ImplementedBy<TImpl>(), lifeStyle));
        }

        /// <summary>
        /// Registers a type with it's implementation.
        /// </summary>
        /// <param name="type">Type of the class</param>
        /// <param name="impl">The type that implements <paramref name="type"/></param>
        /// <param name="lifeStyle">Lifestyle of the objects of this type</param>
        public void Register(Type type, Type impl, DependencyLifeStyle lifeStyle = DependencyLifeStyle.Singleton)
        {
            IocContainer.Register(ApplyLifestyle(Component.For(type, impl).ImplementedBy(impl), lifeStyle));
        }

        /// <summary>
        /// Checks whether given type is registered before.
        /// </summary>
        /// <param name="type">Type to check</param>
        public bool IsRegistered(Type type)
        {
            return IocContainer.Kernel.HasComponent(type);
        }

        /// <summary>
        /// Checks whether given type is registered before.
        /// </summary>
        /// <typeparam name="TType">Type to check</typeparam>
        public bool IsRegistered<TType>()
        {
            return IocContainer.Kernel.HasComponent(typeof(TType));
        }

        /// <summary>
        /// Gets an object from IOC container.
        /// Returning object must be Released (see <see cref="IIocResolver.Release"/>) after usage.
        /// </summary> 
        /// <typeparam name="T">Type of the object to get</typeparam>
        /// <returns>The instance object</returns>
        public T Resolve<T>()
        {
            return IocContainer.Resolve<T>();
        }

        /// <summary>
        /// Gets an object from IOC container.
        /// Returning object must be Released (see <see cref="Release"/>) after usage.
        /// </summary> 
        /// <typeparam name="T">Type of the object to cast</typeparam>
        /// <param name="type">Type of the object to resolve</param>
        /// <returns>The object instance</returns>
        public T Resolve<T>(Type type)
        {
            return (T)IocContainer.Resolve(type);
        }

        /// <summary>
        /// Gets an object from IOC container.
        /// Returning object must be Released (see <see cref="IIocResolver.Release"/>) after usage.
        /// </summary> 
        /// <typeparam name="T">Type of the object to get</typeparam>
        /// <param name="argumentsAsAnonymousType">Constructor arguments</param>
        /// <returns>The instance object</returns>
        public T Resolve<T>(object argumentsAsAnonymousType)
        {
            return IocContainer.Resolve<T>(Arguments.FromProperties(argumentsAsAnonymousType));
        }

        /// <summary>
        /// Gets an object from IOC container.
        /// Returning object must be Released (see <see cref="IIocResolver.Release"/>) after usage.
        /// </summary> 
        /// <param name="type">Type of the object to get</param>
        /// <returns>The instance object</returns>
        public object Resolve(Type type)
        {
            return IocContainer.Resolve(type);
        }

        /// <summary>
        /// Gets an object from IOC container.
        /// Returning object must be Released (see <see cref="IIocResolver.Release"/>) after usage.
        /// </summary> 
        /// <param name="type">Type of the object to get</param>
        /// <param name="argumentsAsAnonymousType">Constructor arguments</param>
        /// <returns>The instance object</returns>
        public object Resolve(Type type, object argumentsAsAnonymousType)
        {
            return IocContainer.Resolve(type, Arguments.FromProperties(argumentsAsAnonymousType));
        }

        ///<inheritdoc/>
        public T[] ResolveAll<T>()
        {
            return IocContainer.ResolveAll<T>();
        }

        ///<inheritdoc/>
        public T[] ResolveAll<T>(object argumentsAsAnonymousType)
        {
            return IocContainer.ResolveAll<T>(Arguments.FromProperties(argumentsAsAnonymousType));
        }

        ///<inheritdoc/>
        public object[] ResolveAll(Type type)
        {
            return IocContainer.ResolveAll(type).Cast<object>().ToArray();
        }

        ///<inheritdoc/>
        public object[] ResolveAll(Type type, object argumentsAsAnonymousType)
        {
            return IocContainer.ResolveAll(type, Arguments.FromProperties(argumentsAsAnonymousType)).Cast<object>().ToArray();
        }

        /// <summary>
        /// Releases a pre-resolved object. See Resolve methods.
        /// </summary>
        /// <param name="obj">Object to be released</param>
        public void Release(object obj)
        {
            IocContainer.Release(obj);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            IocContainer.Dispose();
        }

        private static ComponentRegistration<T> ApplyLifestyle<T>(ComponentRegistration<T> registration, DependencyLifeStyle lifeStyle)
            where T : class
        {
            switch (lifeStyle)
            {
                case DependencyLifeStyle.Transient:
                    return registration.LifestyleTransient();
                case DependencyLifeStyle.Singleton:
                    return registration.LifestyleSingleton();
                default:
                    return registration;
            }
        }
    }
}