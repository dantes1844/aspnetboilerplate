using System;
using Abp.Collections.Extensions;
using AutoMapper;

namespace Abp.AutoMapper
{
    /// <summary>
    /// 双向映射操作，当前类和目标类需要双向的映射
    /// </summary>
    public class AutoMapAttribute : AutoMapAttributeBase
    {
        public AutoMapAttribute(params Type[] targetTypes)
            : base(targetTypes)
        {

        }

        /// <summary>
        /// 创建映射
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="type">打标签的类</param>
        public override void CreateMap(IMapperConfigurationExpression configuration, Type type)
        {
            /*
             *
             * [AutoMap(TypeOf(Person),TypeOf(Chinese))]
             * public class Student{}
             *
             * 这里参数的type就是Student，targetTypes就是 Person,Chinese等
             */
            if (TargetTypes.IsNullOrEmpty())
            {
                return;
            }
            //当前类映射到模板类上，当前类是source
            configuration.CreateAutoAttributeMaps(type, TargetTypes, MemberList.Source);

            //遍历所有的目标类，映射到当前类上，当前类是Destination
            foreach (var targetType in TargetTypes)
            {
                configuration.CreateAutoAttributeMaps(targetType, new[] { type }, MemberList.Destination);
            }
        }
    }
}