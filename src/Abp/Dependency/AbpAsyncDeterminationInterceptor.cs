using Castle.DynamicProxy;

namespace Abp.Dependency
{
    /// <summary>
    /// 用来检测方法是同步还是异步的拦截器
    /// </summary>
    /// <typeparam name="TInterceptor"></typeparam>
    public class AbpAsyncDeterminationInterceptor<TInterceptor> : AsyncDeterminationInterceptor
        where TInterceptor : IAsyncInterceptor
    {
        public AbpAsyncDeterminationInterceptor(TInterceptor asyncInterceptor) : base(asyncInterceptor)
        {

        }
    }
}