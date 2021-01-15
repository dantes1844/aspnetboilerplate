using System;
using Abp.Collections.Extensions;
using AutoMapper;

namespace Abp.AutoMapper
{
    public class AutoMapFromAttribute : AutoMapAttributeBase
    {
        public MemberList MemberList { get; set; } = MemberList.Destination;

        public AutoMapFromAttribute(params Type[] targetTypes)
            : base(targetTypes)
        {

        }

        /// <summary>
        /// 指定映射方向，按道理以类名足以区分使用了，这里再添加这个参数，貌似会产生歧义。
        /// <para>使用情况大概也就是：AutoMapFrom(MemberList.Source)这样正好二者是反的</para>
        /// </summary>
        /// <param name="memberList"></param>
        /// <param name="targetTypes"></param>
        public AutoMapFromAttribute(MemberList memberList, params Type[] targetTypes)
            : this(targetTypes)
        {
            MemberList = memberList;
        }

        public override void CreateMap(IMapperConfigurationExpression configuration, Type type)
        {
            if (TargetTypes.IsNullOrEmpty())
            {
                return;
            }

            foreach (var targetType in TargetTypes)
            {
                configuration.CreateAutoAttributeMaps(targetType, new[] { type }, MemberList);
            }
        }
    }
}