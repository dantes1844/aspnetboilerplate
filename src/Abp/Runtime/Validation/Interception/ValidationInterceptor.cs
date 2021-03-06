﻿using System.Threading.Tasks;
using Abp.Aspects;
using Abp.Dependency;
using Castle.DynamicProxy;

namespace Abp.Runtime.Validation.Interception
{
    /// <summary>
    /// This interceptor is used intercept method calls for classes which's methods must be validated.
    /// </summary>
    public class ValidationInterceptor : AbpInterceptorBase, ITransientDependency
    {
        private readonly IIocResolver _iocResolver;

        public ValidationInterceptor(IIocResolver iocResolver)
        {
            _iocResolver = iocResolver;
        }

        /// <summary>
        /// 被Castle.Windsor.DynamicProxy调用的
        /// </summary>
        /// <param name="invocation"></param>
        public override void InterceptSynchronous(IInvocation invocation)
        {
            // 如果已经调用过验证拦截器，则直接执行下一个拦截器。
            if (AbpCrossCuttingConcerns.IsApplied(invocation.InvocationTarget, AbpCrossCuttingConcerns.Validation))
            {
                invocation.Proceed();
                return;
            }
            //没有应用验证拦截器，手动调用校验，并调用下一个拦截器
            using (var validator = _iocResolver.ResolveAsDisposable<MethodInvocationValidator>())
            {
                validator.Object.Initialize(invocation.MethodInvocationTarget, invocation.Arguments);
                validator.Object.Validate();
            }

            invocation.Proceed();
        }


        protected override async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            var proceedInfo = invocation.CaptureProceedInfo();

            if (AbpCrossCuttingConcerns.IsApplied(invocation.InvocationTarget, AbpCrossCuttingConcerns.Validation))
            {
                proceedInfo.Invoke();
                await ((Task)invocation.ReturnValue).ConfigureAwait(false);
                return;
            }

            using (var validator = _iocResolver.ResolveAsDisposable<MethodInvocationValidator>())
            {
                validator.Object.Initialize(invocation.MethodInvocationTarget, invocation.Arguments);
                validator.Object.Validate();
            }

            proceedInfo.Invoke();
            await ((Task)invocation.ReturnValue).ConfigureAwait(false);
        }

        protected override async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            var proceedInfo = invocation.CaptureProceedInfo();

            if (AbpCrossCuttingConcerns.IsApplied(invocation.InvocationTarget, AbpCrossCuttingConcerns.Validation))
            {
                proceedInfo.Invoke();
                return await ((Task<TResult>)invocation.ReturnValue).ConfigureAwait(false);
            }

            using (var validator = _iocResolver.ResolveAsDisposable<MethodInvocationValidator>())
            {
                validator.Object.Initialize(invocation.MethodInvocationTarget, invocation.Arguments);
                validator.Object.Validate();
            }

            proceedInfo.Invoke();
            return await ((Task<TResult>)invocation.ReturnValue).ConfigureAwait(false);
        }
    }
}
