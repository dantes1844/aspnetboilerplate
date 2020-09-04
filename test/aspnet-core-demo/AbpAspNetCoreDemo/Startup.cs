using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using Abp;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Mvc.Antiforgery;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.Castle.Logging.Log4Net;
using Abp.Dependency;
using Abp.Json;
using Abp.PlugIns;
using AbpAspNetCoreDemo.Controllers;
using AbpAspNetCoreDemo.Laobai;
using Castle.Core.Logging;
using Castle.Facilities.Logging;
using Castle.MicroKernel.ModelBuilder.Inspectors;
using Castle.MicroKernel.SubSystems.Conversion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;

namespace AbpAspNetCoreDemo
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;

        //https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1?view=netcore-3.1
        //AsyncLocal<T> 实例可以用来跨线程存储数据，因为容器本身需要全局公用
        public static readonly AsyncLocal<IocManager> IocManager = new AsyncLocal<IocManager>();

        public Startup(IWebHostEnvironment env)
        {
            //建造者模式，生成ConfigurationBuilder对象， 将不同的配置文件加入到Configuration实例中
            _env = env;
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)//设置文件的根目录
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            var connStr = Configuration.GetConnectionString("Default");
            AbpDebug.WriteLine($"connStr={connStr}");
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //读取所有的配置项后，将其设置为单例的依赖注入。方便其他页面使用
            services.AddSingleton(Configuration);

            #region 测试filter的 2020年8月21日

            services.Configure<PositionOptions>(Configuration.GetSection("Position"));
            //services.AddScoped<GlobalRegisteredActionFilterAttribute>();//配合ServiceFilter的，不注册的话，FilterService会报错

            #endregion

            #region 测试注入的，无用

            ////Some test classes
            //services.AddTransient<MyTransientClass1>();
            //services.AddTransient<MyTransientClass2>();
            //services.AddScoped<MyScopedClass>(); 

            #endregion

            //Add framework services
            services.AddMvc(options =>
            {
                //添加自动表单防伪，由ASP.Net Core的过滤器机制来实现
                //options.Filters.Add(new AbpAutoValidateAntiforgeryTokenAttribute());//实例方法注入Filter，所有的请求公用这个Filter变量

                ////这里可以全局注册过滤器：对所有的控制器和action起作用，所以这时候在控制器上再加有可能会出现错误。Header里不允许添加重复的键
                options.Filters.Add(typeof(GlobalRegisteredActionFilterAttribute));//类注入Filter，将会激活类，所有的构造函数注入的Filter都将实例化
                options.Filters.Add(typeof(LaobaiResultActionFilter));
            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new AbpMvcContractResolver(IocManager.Value)
                {
                    //策略模式，使用不同的方法转化json的格式，这里使用的是驼峰法
                    NamingStrategy = new CamelCaseNamingStrategy()
                };
            });

            #region OData，官方调用注释

            // Waiting for OData .NET Core 3.0 support, see https://github.com/OData/WebApi/issues/1748
            // services.AddOData();

            // Workaround: https://github.com/OData/WebApi/issues/1177
            // Waiting for OData .NET Core 3.0 support, see https://github.com/OData/WebApi/issues/1748
            //services.AddMvcCore(options =>
            //{
            //    foreach (var outputFormatter in options.OutputFormatters.OfType<ODataOutputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
            //    {
            //        outputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
            //    }

            //    foreach (var inputFormatter in options.InputFormatters.OfType<ODataInputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
            //    {
            //        inputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
            //    }
            //}); 

            #endregion

            //Configure Abp and Dependency Injection. Should be called last.
            return services.AddAbp<AbpAspNetCoreDemoModule>(options =>
            {
                options.IocManager = IocManager.Value ?? new IocManager();

                string plugDllInPath = "";
#if DEBUG
                plugDllInPath = Path.Combine(_env.ContentRootPath,
                    @"..\AbpAspNetCoreDemo.PlugIn\bin\Debug\netcoreapp3.1\AbpAspNetCoreDemo.PlugIn.dll");
#else
                plugDllInPath = Path.Combine(_env.ContentRootPath,
                    @"..\AbpAspNetCoreDemo.PlugIn\bin\Release\netcoreapp3.1\AbpAspNetCoreDemo.PlugIn.dll");
#endif
                if (!File.Exists(plugDllInPath))
                {
                    throw new FileNotFoundException("There is no plugin dll file in the given path.", plugDllInPath);
                }

                options.PlugInSources.Add(new AssemblyFileListPlugInSource(plugDllInPath));

                //Configure Log4Net logging
                options.IocManager.IocContainer.AddFacility<LoggingFacility>(
                    f => f.UseAbpLog4Net().WithConfig("log4net.config")
                );

                var propInjector = options.IocManager.IocContainer.Kernel.ComponentModelBuilder
                    .Contributors
                    .OfType<PropertiesDependenciesModelInspector>()
                    .Single();

                options.IocManager.IocContainer.Kernel.ComponentModelBuilder.RemoveContributor(propInjector);
                options.IocManager.IocContainer.Kernel.ComponentModelBuilder.AddContributor(new AbpPropertiesDependenciesModelInspector(new DefaultConversionManager()));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseAbp(); //Initializes ABP framework. Should be called first.

            #region OData，官方调用注释

            // Waiting for OData .NET Core 3.0 support, see https://github.com/OData/WebApi/issues/1748
            // app.UseOData(builder =>
            // {
            //     builder.EntitySet<Product>("Products").EntityType.Expand().Filter().OrderBy().Page().Select();
            // });

            // Return IQueryable from controllers
            //app.UseUnitOfWork(options =>
            //{
            //    options.Filter = httpContext => httpContext.Request.Path.Value.StartsWith("/odata");
            //}); 

            #endregion

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseEmbeddedFiles(); //Allows to expose embedded files to the web!

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("defaultWithArea", "{area}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute("default", "{controller=TagHelper}/{action=Index}/{id?}");
                endpoints.MapRazorPages();

                app.ApplicationServices.GetRequiredService<IAbpAspNetCoreConfiguration>().EndpointConfiguration.ConfigureAllEndpoints(endpoints);

                //TODO@3.0 related: https://github.com/OData/WebApi/issues/1707
                //routes.MapODataServiceRoute(app); ???
            });
        }
    }
}
