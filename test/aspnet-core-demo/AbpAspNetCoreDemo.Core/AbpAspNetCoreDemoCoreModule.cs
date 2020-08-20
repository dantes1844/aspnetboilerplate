using System.Diagnostics;
using Abp;
using Abp.AutoMapper;
using Abp.Localization;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Json;
using Abp.Modules;
using Abp.Reflection.Extensions;

namespace AbpAspNetCoreDemo.Core
{
    [DependsOn(typeof(AbpAutoMapperModule))]
    public class AbpAspNetCoreDemoCoreModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Auditing.IsEnabledForAnonymousUsers = true;

            Configuration.Localization.Languages.Add(new LanguageInfo("en", "English", isDefault: true));
            Configuration.Localization.Languages.Add(new LanguageInfo("tr", "Türkçe"));

            AbpDebug.WriteLine($"{nameof(AbpAspNetCoreDemoCoreModule)}添加AbpAspNetCoreDemoModule的本地化资源配置项");
            Configuration.Localization.Sources.Add(
                new DictionaryBasedLocalizationSource("AbpAspNetCoreDemoModule",
                    new JsonEmbeddedFileLocalizationDictionaryProvider(
                        typeof(AbpAspNetCoreDemoCoreModule).GetAssembly(),
                        "AbpAspNetCoreDemo.Core.Localization.SourceFiles"
                    )
                )
            );
            Configuration.Localization.WrapGivenTextIfNotFound = false;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AbpAspNetCoreDemoCoreModule).GetAssembly());
        }
    }
}