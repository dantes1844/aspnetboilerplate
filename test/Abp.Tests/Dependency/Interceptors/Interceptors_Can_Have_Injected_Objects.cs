using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;
using Shouldly;
using Xunit;

namespace Abp.Tests.Dependency.Interceptors
{
    public class Interceptors_Tests : TestBaseWithLocalIocManager
    {
        [Fact]
        public void Interceptors_Should_Work()
        {
            LocalIocManager.IocContainer.Register(
                Component.For<BracketInterceptor>().LifestyleTransient(),
                Component.For<MyGreetingClass>().Interceptors<BracketInterceptor>().LifestyleTransient()
                );

            var greetingObj = LocalIocManager.Resolve<MyGreetingClass>();

            greetingObj.SayHello("Halil").ShouldBe("(Hello Halil)");
        }

        public class MyGreetingClass
        {
            public virtual string SayHello(string name)
            {
                return "Hello " + name;
            }
        }

        /// <summary>
        /// 实现 拦截器的方法
        /// </summary>
        public class BracketInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                //本身的方法调用
                invocation.Proceed();
                //对方法返回值进行包装处理
                invocation.ReturnValue = "(" + invocation.ReturnValue + ")";
            }
        }
    }
}
