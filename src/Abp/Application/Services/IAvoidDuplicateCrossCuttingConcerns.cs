using System.Collections.Generic;

namespace Abp.Application.Services
{
    /// <summary>
    /// 该接口用来标记服务类或者自动生成的api方法避免添加多个重复的切面关注点
    /// </summary>
    public interface IAvoidDuplicateCrossCuttingConcerns
    {
        /// <summary>
        /// 已经添加的切面关注点
        /// </summary>
        List<string> AppliedCrossCuttingConcerns { get; }
    }
}