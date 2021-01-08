using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Abp.Application.Features;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Localization;
using Abp.Reflection;
using Abp.Runtime.Session;

namespace Abp.Authorization
{
    /// <summary>
    /// 实际的权限校验类,继承了ITransientDependency，被自动注入成瞬时对象
    /// </summary>
    public class AuthorizationHelper : IAuthorizationHelper, ITransientDependency
    {
        public IAbpSession AbpSession { get; set; }
        public IPermissionChecker PermissionChecker { get; set; }
        public ILocalizationManager LocalizationManager { get; set; }

        private readonly IFeatureChecker _featureChecker;
        private readonly IAuthorizationConfiguration _authConfiguration;

        public AuthorizationHelper(IFeatureChecker featureChecker, IAuthorizationConfiguration authConfiguration)
        {
            _featureChecker = featureChecker;
            _authConfiguration = authConfiguration;
            AbpSession = NullAbpSession.Instance;
            PermissionChecker = NullPermissionChecker.Instance;
            LocalizationManager = NullLocalizationManager.Instance;
        }

        public virtual async Task AuthorizeAsync(IEnumerable<IAbpAuthorizeAttribute> authorizeAttributes)
        {
            if (!_authConfiguration.IsEnabled)
            {
                return;
            }

            //没有用户信息的直接没有权限
            if (!AbpSession.UserId.HasValue)
            {
                throw new AbpAuthorizationException(
                    LocalizationManager.GetString(AbpConsts.LocalizationSourceName, "CurrentUserDidNotLoginToTheApplication")
                    );
            }
            //遍历每个权限进行验证
            foreach (var authorizeAttribute in authorizeAttributes)
            {
                await PermissionChecker.AuthorizeAsync(authorizeAttribute.RequireAllPermissions, authorizeAttribute.Permissions);
            }
        }

        public virtual void Authorize(IEnumerable<IAbpAuthorizeAttribute> authorizeAttributes)
        {
            if (!_authConfiguration.IsEnabled)
            {
                return;
            }

            if (!AbpSession.UserId.HasValue)
            {
                throw new AbpAuthorizationException(
                    LocalizationManager.GetString(AbpConsts.LocalizationSourceName, "CurrentUserDidNotLoginToTheApplication")
                    );
            }

            foreach (var authorizeAttribute in authorizeAttributes)
            {
                PermissionChecker.Authorize(authorizeAttribute.RequireAllPermissions, authorizeAttribute.Permissions);
            }
        }

        public virtual async Task AuthorizeAsync(MethodInfo methodInfo, Type type)
        {
            await CheckFeaturesAsync(methodInfo, type);
            await CheckPermissionsAsync(methodInfo, type);
        }

        public virtual void Authorize(MethodInfo methodInfo, Type type)
        {
            CheckFeatures(methodInfo, type);
            CheckPermissions(methodInfo, type);
        }

        /// <summary>
        /// 多租户相关的内容。暂时不考虑
        /// <para>参考连接 https://aspnetboilerplate.com/Pages/Documents/Feature-Management </para>
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual async Task CheckFeaturesAsync(MethodInfo methodInfo, Type type)
        {
            var featureAttributes = ReflectionHelper.GetAttributesOfMemberAndType<RequiresFeatureAttribute>(methodInfo, type);

            if (featureAttributes.Count <= 0)
            {
                return;
            }

            foreach (var featureAttribute in featureAttributes)
            {
                await _featureChecker.CheckEnabledAsync(featureAttribute.RequiresAll, featureAttribute.Features);
            }
        }

        protected virtual void CheckFeatures(MethodInfo methodInfo, Type type)
        {
            var featureAttributes = ReflectionHelper.GetAttributesOfMemberAndType<RequiresFeatureAttribute>(methodInfo, type);

            if (featureAttributes.Count <= 0)
            {
                return;
            }

            foreach (var featureAttribute in featureAttributes)
            {
                _featureChecker.CheckEnabled(featureAttribute.RequiresAll, featureAttribute.Features);
            }
        }

        protected virtual async Task CheckPermissionsAsync(MethodInfo methodInfo, Type type)
        {
            //配置将权限校验关闭的直接返回
            if (!_authConfiguration.IsEnabled)
            {
                return;
            }

            //允许匿名访问的方法直接返回
            if (AllowAnonymous(methodInfo, type))
            {
                return;
            }

            //属性生成的方法直接返回
            if (ReflectionHelper.IsPropertyGetterSetterMethod(methodInfo, type))
            {
                return;
            }

            //非public方法且未定义AbpAuthorizeAttribute标签的直接返回
            if (!methodInfo.IsPublic && !methodInfo.GetCustomAttributes().OfType<IAbpAuthorizeAttribute>().Any())
            {
                return;
            }

            var authorizeAttributes =
                ReflectionHelper
                    .GetAttributesOfMemberAndType(methodInfo, type)
                    .OfType<IAbpAuthorizeAttribute>()
                    .ToArray();


            if (!authorizeAttributes.Any())
            {
                return;
            }

            //包含四种权限校验标签中的任何一个，进行校验
            await AuthorizeAsync(authorizeAttributes);
        }

        protected virtual void CheckPermissions(MethodInfo methodInfo, Type type)
        {
            if (!_authConfiguration.IsEnabled)
            {
                return;
            }

            if (AllowAnonymous(methodInfo, type))
            {
                return;
            }

            if (ReflectionHelper.IsPropertyGetterSetterMethod(methodInfo, type))
            {
                return;
            }

            if (!methodInfo.IsPublic && !methodInfo.GetCustomAttributes().OfType<IAbpAuthorizeAttribute>().Any())
            {
                return;
            }

            var authorizeAttributes =
                ReflectionHelper
                    .GetAttributesOfMemberAndType(methodInfo, type)
                    .OfType<IAbpAuthorizeAttribute>()
                    .ToArray();

            if (!authorizeAttributes.Any())
            {
                return;
            }

            Authorize(authorizeAttributes);
        }

        private static bool AllowAnonymous(MemberInfo memberInfo, Type type)
        {
            return ReflectionHelper
                .GetAttributesOfMemberAndType(memberInfo, type)
                .OfType<IAbpAllowAnonymousAttribute>()
                .Any();
        }
    }
}