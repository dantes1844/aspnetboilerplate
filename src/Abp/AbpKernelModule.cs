﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using Abp.Application.Features;
using Abp.Application.Navigation;
using Abp.Application.Services;
using Abp.Auditing;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Collections.Extensions;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.DynamicEntityParameters;
using Abp.EntityHistory;
using Abp.Events.Bus;
using Abp.Localization;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Modules;
using Abp.MultiTenancy;
using Abp.Net.Mail;
using Abp.Notifications;
using Abp.RealTime;
using Abp.Reflection.Extensions;
using Abp.Runtime;
using Abp.Runtime.Caching;
using Abp.Runtime.Remoting;
using Abp.Runtime.Validation.Interception;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Timing;
using Abp.Webhooks;
using Castle.MicroKernel.Registration;

namespace Abp
{
    /// <summary>
    /// Kernel (core) module of the ABP system.
    /// No need to depend on this, it's automatically the first module always.
    /// </summary>
    public sealed class AbpKernelModule : AbpModule
    {
        public override void PreInitialize()
        {
            IocManager.AddConventionalRegistrar(new BasicConventionalRegistrar());

            IocManager.Register<IScopedIocResolver, ScopedIocResolver>(DependencyLifeStyle.Transient);
            IocManager.Register(typeof(IAmbientScopeProvider<>), typeof(DataContextAmbientScopeProvider<>), DependencyLifeStyle.Transient);

            AddAuditingSelectors();
            AddLocalizationSources();//加载本地化资源
            AddSettingProviders();
            AddUnitOfWorkFilters();
            ConfigureCaches();
            AddIgnoredTypes();
            AddMethodParameterValidators();
            AddDefaultNotificationDistributor();
        }

        public override void Initialize()
        {
            foreach (var replaceAction in ((AbpStartupConfiguration)Configuration).ServiceReplaceActions.Values)
            {
                replaceAction();
            }

            IocManager.IocContainer.Install(new EventBusInstaller(IocManager));

            IocManager.Register(typeof(IOnlineClientManager<>), typeof(OnlineClientManager<>), DependencyLifeStyle.Singleton);
            IocManager.Register(typeof(IOnlineClientStore<>), typeof(InMemoryOnlineClientStore<>), DependencyLifeStyle.Singleton);

            IocManager.Register(typeof(EventTriggerAsyncBackgroundJob<>), DependencyLifeStyle.Transient);

            IocManager.RegisterAssemblyByConvention(typeof(AbpKernelModule).GetAssembly(),
                new ConventionalRegistrationConfig
                {
                    InstallInstallers = false
                });

            RegisterInterceptors();
        }

        private void RegisterInterceptors()
        {
            //这是将拦截器类进行注册，它应该是由Castle自己使用的。在Abp中并没有使用它的实例
            IocManager.Register(typeof(AbpAsyncDeterminationInterceptor<UnitOfWorkInterceptor>), DependencyLifeStyle.Transient);
            IocManager.Register(typeof(AbpAsyncDeterminationInterceptor<AuditingInterceptor>), DependencyLifeStyle.Transient);
            IocManager.Register(typeof(AbpAsyncDeterminationInterceptor<AuthorizationInterceptor>), DependencyLifeStyle.Transient);
            IocManager.Register(typeof(AbpAsyncDeterminationInterceptor<ValidationInterceptor>), DependencyLifeStyle.Transient);
            IocManager.Register(typeof(AbpAsyncDeterminationInterceptor<EntityHistoryInterceptor>), DependencyLifeStyle.Transient);
        }

        public override void PostInitialize()
        {
            RegisterMissingComponents();

            IocManager.Resolve<SettingDefinitionManager>().Initialize();
            IocManager.Resolve<FeatureManager>().Initialize();
            IocManager.Resolve<PermissionManager>().Initialize();
            IocManager.Resolve<LocalizationManager>().Initialize();
            IocManager.Resolve<NotificationDefinitionManager>().Initialize();
            IocManager.Resolve<NavigationManager>().Initialize();
            IocManager.Resolve<WebhookDefinitionManager>().Initialize();
            IocManager.Resolve<DynamicEntityParameterDefinitionManager>().Initialize();

            if (Configuration.BackgroundJobs.IsJobExecutionEnabled)
            {
                var workerManager = IocManager.Resolve<IBackgroundWorkerManager>();
                workerManager.Start();
                workerManager.Add(IocManager.Resolve<IBackgroundJobManager>());
            }
        }

        public override void Shutdown()
        {
            if (Configuration.BackgroundJobs.IsJobExecutionEnabled)
            {
                IocManager.Resolve<IBackgroundWorkerManager>().StopAndWaitToStop();
            }
        }

        private void AddUnitOfWorkFilters()
        {
            Configuration.UnitOfWork.RegisterFilter(AbpDataFilters.SoftDelete, true);
            Configuration.UnitOfWork.RegisterFilter(AbpDataFilters.MustHaveTenant, true);
            Configuration.UnitOfWork.RegisterFilter(AbpDataFilters.MayHaveTenant, true);
        }

        private void AddSettingProviders()
        {
            Configuration.Settings.Providers.Add<LocalizationSettingProvider>();
            Configuration.Settings.Providers.Add<EmailSettingProvider>();
            Configuration.Settings.Providers.Add<NotificationSettingProvider>();
            Configuration.Settings.Providers.Add<TimingSettingProvider>();
        }

        private void AddAuditingSelectors()
        {
            Configuration.Auditing.Selectors.Add(
                new NamedTypeSelector(
                    "Abp.ApplicationServices",
                    type => typeof(IApplicationService).IsAssignableFrom(type)
                )
            );
        }

        private void AddLocalizationSources()
        {
            //添加Abp开头的本地化资源配置项：abp核心模块的本地化资源
            AbpDebug.WriteLine("添加Abp开头的本地化资源配置项");

            //要读取本地化资源的程序集
            var assembly = typeof(AbpKernelModule).GetAssembly();
            //资源文件除文件名外的完整命名空间
            var rootNameSpaces = "Abp.Localization.Sources.AbpXmlSource";
            //资源解析器
            var provider = new XmlEmbeddedFileLocalizationDictionaryProvider(assembly, rootNameSpaces);
            //资源
            var source = new DictionaryBasedLocalizationSource(AbpConsts.LocalizationSourceName, provider);
            Configuration.Localization.Sources.Add(source);
        }

        private void ConfigureCaches()
        {
            Configuration.Caching.Configure(AbpCacheNames.ApplicationSettings, cache =>
            {
                cache.DefaultSlidingExpireTime = TimeSpan.FromHours(8);
            });

            Configuration.Caching.Configure(AbpCacheNames.TenantSettings, cache =>
            {
                cache.DefaultSlidingExpireTime = TimeSpan.FromMinutes(60);
            });

            Configuration.Caching.Configure(AbpCacheNames.UserSettings, cache =>
            {
                cache.DefaultSlidingExpireTime = TimeSpan.FromMinutes(20);
            });
        }

        private void AddIgnoredTypes()
        {
            var commonIgnoredTypes = new[]
            {
                typeof(Stream),
                typeof(Expression)
            };

            foreach (var ignoredType in commonIgnoredTypes)
            {
                Configuration.Auditing.IgnoredTypes.AddIfNotContains(ignoredType);
                Configuration.Validation.IgnoredTypes.AddIfNotContains(ignoredType);
            }

            var validationIgnoredTypes = new[] { typeof(Type) };
            foreach (var ignoredType in validationIgnoredTypes)
            {
                Configuration.Validation.IgnoredTypes.AddIfNotContains(ignoredType);
            }
        }

        private void AddMethodParameterValidators()
        {
            Configuration.Validation.Validators.Add<DataAnnotationsValidator>();
            Configuration.Validation.Validators.Add<ValidatableObjectValidator>();
            Configuration.Validation.Validators.Add<CustomValidator>();
        }

        private void AddDefaultNotificationDistributor()
        {
            Configuration.Notifications.Distributers.Add<DefaultNotificationDistributer>();
        }

        private void RegisterMissingComponents()
        {
            if (!IocManager.IsRegistered<IGuidGenerator>())
            {
                IocManager.IocContainer.Register(
                    Component
                        .For<IGuidGenerator, SequentialGuidGenerator>()
                        .Instance(SequentialGuidGenerator.Instance)
                );
            }

            IocManager.RegisterIfNot<IUnitOfWork, NullUnitOfWork>(DependencyLifeStyle.Transient);
            IocManager.RegisterIfNot<IAuditingStore, SimpleLogAuditingStore>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<IPermissionChecker, NullPermissionChecker>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<IRealTimeNotifier, NullRealTimeNotifier>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<INotificationStore, NullNotificationStore>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<IUnitOfWorkFilterExecuter, NullUnitOfWorkFilterExecuter>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<IClientInfoProvider, NullClientInfoProvider>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<ITenantStore, NullTenantStore>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<ITenantResolverCache, NullTenantResolverCache>(DependencyLifeStyle.Singleton);
            IocManager.RegisterIfNot<IEntityHistoryStore, NullEntityHistoryStore>(DependencyLifeStyle.Singleton);

            if (Configuration.BackgroundJobs.IsJobExecutionEnabled)
            {
                IocManager.RegisterIfNot<IBackgroundJobStore, InMemoryBackgroundJobStore>(DependencyLifeStyle.Singleton);
            }
            else
            {
                IocManager.RegisterIfNot<IBackgroundJobStore, NullBackgroundJobStore>(DependencyLifeStyle.Singleton);
            }
        }
    }
}
