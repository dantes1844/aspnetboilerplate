using System;
using AutoMapper;

namespace Abp.AutoMapper
{
    /// <summary>
    /// 自动映射父类，定义了一个CreateMap，供每个子类来实现各自的自动映射操作
    /// </summary>
    public abstract class AutoMapAttributeBase : Attribute
    {
        /// <summary>
        /// 要映射到的目标类型
        /// </summary>
        public Type[] TargetTypes { get; private set; }

        protected AutoMapAttributeBase(params Type[] targetTypes)
        {
            TargetTypes = targetTypes;
        }

        public abstract void CreateMap(IMapperConfigurationExpression configuration, Type type);
    }
}