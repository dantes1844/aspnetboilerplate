﻿using System;
using System.Threading.Tasks;
using Abp.Web.Security.AntiForgery;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Abp.AspNetCore.Mvc.Antiforgery
{
    public class AbpValidateAntiforgeryTokenAuthorizationFilter : IAsyncAuthorizationFilter, IAntiforgeryPolicy
    {
        private IAntiforgery _antiforgery;
        private readonly AntiforgeryOptions _antiforgeryOptions;
        private readonly IOptionsSnapshot<CookieAuthenticationOptions> _namedOptionsAccessor;
        private readonly IAbpAntiForgeryConfiguration _antiForgeryConfiguration;
        private readonly ILogger _logger;

        public AbpValidateAntiforgeryTokenAuthorizationFilter(
            IAntiforgery antiforgery,
            IOptions<AntiforgeryOptions> antiforgeryOptions,
            IAbpAntiForgeryConfiguration antiForgeryConfiguration,
            IOptionsSnapshot<CookieAuthenticationOptions> namedOptionsAccessor,
            ILogger logger)
        {
            _antiforgery = antiforgery;
            _antiforgeryOptions = antiforgeryOptions.Value;
            _namedOptionsAccessor = namedOptionsAccessor;
            _logger = logger;
            _antiForgeryConfiguration = antiForgeryConfiguration;
        }

        /// <summary>
        /// 这个方法最终在<see cref="Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker"/>中被调用 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            //有其他优先级更高的过滤器则不执行当前这个
            if (!context.IsEffectivePolicy<IAntiforgeryPolicy>(this))
            {
                _logger.Info("Skipping the execution of current filter as its not the most effective filter implementing the policy " + typeof(IAntiforgeryPolicy));
                return;
            }

            if (ShouldValidate(context))
            {
                try
                {
                    //asp.net core里是由运行时自己调用这个方法来确定防伪是否被篡改。
                    //mvc5里是根据传入的参数进行判断，在Abp.Web.MVC项目里
                    await _antiforgery.ValidateRequestAsync(context.HttpContext);
                }
                catch (AntiforgeryValidationException exception)
                {
                    _logger.Error(exception.Message, exception);
                    //给Result赋值，ResourceInvoker会短路当前所有过滤器，直接返回结果
                    /*
                     
                    case State.AuthorizationAsyncEnd:
                    {
                        Debug.Assert(state != null);
                        Debug.Assert(_authorizationContext != null);

                        var filter = (IAsyncAuthorizationFilter)state;
                        var authorizationContext = _authorizationContext;

                        _diagnosticListener.AfterOnAuthorizationAsync(authorizationContext, filter);
                        _logger.AfterExecutingMethodOnFilter(
                            FilterTypeConstants.AuthorizationFilter,
                            nameof(IAsyncAuthorizationFilter.OnAuthorizationAsync),
                            filter);

                        if (authorizationContext.Result != null)
                        {
                            goto case State.AuthorizationShortCircuit;
                        }

                        goto case State.AuthorizationNext;
                    }
                     *
                     *
                     */
                    context.Result = new AntiforgeryValidationFailedResult();
                }
            }
        }

        protected virtual bool ShouldValidate(AuthorizationFilterContext context)
        {
            if (!ShouldValidateInternal(context))
            {
                return false;
            }

            var cookieAuthenticationOptions = _namedOptionsAccessor.Get(_antiForgeryConfiguration.AuthorizationCookieName);

            //Always perform antiforgery validation when request contains authentication cookie
            if (cookieAuthenticationOptions?.Cookie.Name != null &&
                context.HttpContext.Request.Cookies.ContainsKey(cookieAuthenticationOptions.Cookie.Name))
            {
                return true;
            }

            //No need to validate if antiforgery cookie is not sent.
            //That means the request is sent from a non-browser client.
            //See https://github.com/aspnet/Antiforgery/issues/115
            if (!context.HttpContext.Request.Cookies.ContainsKey(_antiforgeryOptions.Cookie.Name))
            {
                return false;
            }

            // Anything else requires a token.
            return true;
        }

        private static bool ShouldValidateInternal(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return true;
        }
    }
}
