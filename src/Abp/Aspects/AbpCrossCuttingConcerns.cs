﻿using System;
using Abp.Application.Services;
using Abp.Collections.Extensions;
using JetBrains.Annotations;

namespace Abp.Aspects
{
    internal static class AbpCrossCuttingConcerns
    {
        public const string Auditing = "AbpAuditing";
        public const string Validation = "AbpValidation";
        public const string UnitOfWork = "AbpUnitOfWork";
        public const string Authorization = "AbpAuthorization";

        public static void AddApplied(object obj, params string[] concerns)
        {
            if (concerns.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(concerns), $"{nameof(concerns)} should be provided!");
            }
            //给当前方法添加切面关注点
            (obj as IAvoidDuplicateCrossCuttingConcerns)?.AppliedCrossCuttingConcerns.AddRange(concerns);
        }

        public static void RemoveApplied(object obj, params string[] concerns)
        {
            if (concerns.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(concerns), $"{nameof(concerns)} should be provided!");
            }

            var crossCuttingEnabledObj = obj as IAvoidDuplicateCrossCuttingConcerns;
            if (crossCuttingEnabledObj == null)
            {
                return;
            }

            foreach (var concern in concerns)
            {
                crossCuttingEnabledObj.AppliedCrossCuttingConcerns.RemoveAll(c => c == concern);
            }
        }

        /// <summary>
        /// 这里的应用逻辑应该是：控制器方法会自动调用 AbpValidationActionFilter 中的校验方法
        /// 而业务类中则调用拦截器 ValidationInterceptor 的校验方法
        /// 二者不会同时出现，所以增加一个标记判断是否已经经过校验了
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="concern"></param>
        /// <returns></returns>
        public static bool IsApplied([NotNull] object obj, [NotNull] string concern)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (concern == null)
            {
                throw new ArgumentNullException(nameof(concern));
            }
            //判断当前方法是否已经添加了 concern 变量指定的切面关注点
            return (obj as IAvoidDuplicateCrossCuttingConcerns)?.AppliedCrossCuttingConcerns.Contains(concern) ?? false;
        }

        public static IDisposable Applying(object obj, params string[] concerns)
        {
            AddApplied(obj, concerns);
            return new DisposeAction(() =>
            {
                RemoveApplied(obj, concerns);
            });
        }

        public static string[] GetApplieds(object obj)
        {
            var crossCuttingEnabledObj = obj as IAvoidDuplicateCrossCuttingConcerns;
            if (crossCuttingEnabledObj == null)
            {
                return new string[0];
            }

            return crossCuttingEnabledObj.AppliedCrossCuttingConcerns.ToArray();
        }
    }
}