using System;
using System.Reflection;
using Abp.Reflection.Extensions;

namespace Abp.Application.Services
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
    public class RemoteServiceAttribute : Attribute
    {
        /// <summary>
        /// Default: true.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// �Ƿ�����Ԫ���ݣ�Ĭ��ֵ����
        /// </summary>
        public bool IsMetadataEnabled { get; set; }

        public RemoteServiceAttribute(bool isEnabled = true)
        {
            IsEnabled = isEnabled;
            IsMetadataEnabled = true;
        }

        /// <summary>
        /// �ǶԶ�ĳ������Ч��Ĭ��ֵ����
        /// �鷽����������������д
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual bool IsEnabledFor(Type type)
        {
            return IsEnabled;
        }

        /// <summary>
        /// �Ƿ��ĳ��������Ч��Ĭ��ֵ����
        /// �鷽����������������д
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public virtual bool IsEnabledFor(MethodInfo method)
        {
            return IsEnabled;
        }

        /// <summary>
        /// Ԫ�����Ƿ��ĳ�������ã�Ĭ��ֵ����
        /// �鷽����������������д
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual bool IsMetadataEnabledFor(Type type)
        {
            return IsMetadataEnabled;
        }
        /// <summary>
        /// Ԫ�����Ƿ��ĳ���������ã�Ĭ��ֵ����
        /// �鷽����������������д
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual bool IsMetadataEnabledFor(MethodInfo method)
        {
            return IsMetadataEnabled;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsExplicitlyEnabledFor(Type type)
        {
            var remoteServiceAttr = type.GetTypeInfo().GetSingleAttributeOrNull<RemoteServiceAttribute>();
            return remoteServiceAttr != null && remoteServiceAttr.IsEnabledFor(type);
        }

        public static bool IsExplicitlyDisabledFor(Type type)
        {
            var remoteServiceAttr = type.GetTypeInfo().GetSingleAttributeOrNull<RemoteServiceAttribute>();
            return remoteServiceAttr != null && !remoteServiceAttr.IsEnabledFor(type);
        }

        public static bool IsMetadataExplicitlyEnabledFor(Type type)
        {
            var remoteServiceAttr = type.GetTypeInfo().GetSingleAttributeOrNull<RemoteServiceAttribute>();
            return remoteServiceAttr != null && remoteServiceAttr.IsMetadataEnabledFor(type);
        }

        public static bool IsMetadataExplicitlyDisabledFor(Type type)
        {
            var remoteServiceAttr = type.GetTypeInfo().GetSingleAttributeOrNull<RemoteServiceAttribute>();
            return remoteServiceAttr != null && !remoteServiceAttr.IsMetadataEnabledFor(type);
        }

        public static bool IsMetadataExplicitlyDisabledFor(MethodInfo method)
        {
            var remoteServiceAttr = method.GetSingleAttributeOrNull<RemoteServiceAttribute>();
            return remoteServiceAttr != null && !remoteServiceAttr.IsMetadataEnabledFor(method);
        }

        public static bool IsMetadataExplicitlyEnabledFor(MethodInfo method)
        {
            var remoteServiceAttr = method.GetSingleAttributeOrNull<RemoteServiceAttribute>();
            return remoteServiceAttr != null && remoteServiceAttr.IsMetadataEnabledFor(method);
        }
    }
}