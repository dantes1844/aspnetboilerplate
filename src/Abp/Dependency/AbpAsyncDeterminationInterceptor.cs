using Castle.DynamicProxy;

namespace Abp.Dependency
{
    /// <summary>
    /// ������ⷽ����ͬ�������첽��������
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