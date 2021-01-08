using Castle.DynamicProxy;
using System.Threading.Tasks;

namespace Abp.Dependency
{

    /// <summary>
    /// 异步拦截器的父类
    /// </summary>
    public abstract class AbpInterceptorBase : IAsyncInterceptor
    {
        #region 这三个方法是异步拦截器实例必须要实现的
        
        public virtual void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
        }

        public virtual void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
        }

        public abstract void InterceptSynchronous(IInvocation invocation); 

        #endregion

        protected abstract Task InternalInterceptAsynchronous(IInvocation invocation);

        protected abstract Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation);
    }
}
