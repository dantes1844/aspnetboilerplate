using System.Collections.Generic;

namespace Abp.Application.Services
{
    /// <summary>
    /// 该接口用来标记服务类或者自动生成的api方法，记录当前类的方法已经应用了哪些切面关注点，避免重复调用
    /// </summary>
    public interface IAvoidDuplicateCrossCuttingConcerns
    {
        /// <summary>
        /// 已经应用的切面关注点
        /// </summary>
        List<string> AppliedCrossCuttingConcerns { get; }
    }
}