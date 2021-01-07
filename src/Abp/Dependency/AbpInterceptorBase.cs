using Castle.DynamicProxy;
using System.Threading.Tasks;

namespace Abp.Dependency
{

    /// <summary>
    /// 异步拦截器的父类
    /// </summary>
    public abstract class AbpInterceptorBase : IAsyncInterceptor
    {
        public virtual void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
        }

        public virtual void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
        }

        public abstract void InterceptSynchronous(IInvocation invocation);

        protected abstract Task InternalInterceptAsynchronous(IInvocation invocation);

        protected abstract Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation);
    }
}
