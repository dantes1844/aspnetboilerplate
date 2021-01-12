using Abp.Dependency;
using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.Net.Mail;
using Abp.Reflection.Extensions;

namespace Abp.MailKit
{
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpMailKitModule : AbpModule
    {
        public override void PreInitialize()
        {
            IocManager.Register<IAbpMailKitConfiguration, AbpMailKitConfiguration>();

            /*
             * 2021年1月8日 11:05:29
             * IEmailSender 的几个实现，NullEmailSender，SmtpEmailSender，MailKitEmailSender。
             * 其中 SmtpEmailSender 继承自 ITransientDependency 接口，按道理应该是默认的实现，因为在PreInitialize中进行快捷注册的
             * 然后再在这里进行ReplaceService，而Replace是在Initialize方法中执行，那么这里的替换应该是没有生效的？
             *
             * 2021年1月8日 11:17:07
             * 验证结果：确实没有替换，得到的实例是SmtpEmailSender类型
             *
             */
            Configuration.ReplaceService<IEmailSender, MailKitEmailSender>(DependencyLifeStyle.Transient);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(AbpMailKitModule).GetAssembly());
        }
    }
}
