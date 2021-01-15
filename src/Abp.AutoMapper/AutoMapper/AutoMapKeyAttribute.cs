using System;
using Abp.Collections.Extensions;
using AutoMapper;

namespace Abp.AutoMapper
{
    /// <summary>
    /// 映射键：根据这个标记来确定两个类实例是否进行映射，例如实例1的ID=实例2.ID，这个时候才是需要映射的，否则不映射
    /// </summary>
    public class AutoMapKeyAttribute : Attribute
    {

    }
}