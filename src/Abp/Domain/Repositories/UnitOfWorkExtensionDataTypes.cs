namespace Abp.Domain.Repositories
{
    internal class UnitOfWorkExtensionDataTypes
    {
        /// <summary>
        /// 真删除的标记键，在一个internal类里，只有当前程序集可以使用，在我们的业务系统中是无法访问到的
        /// </summary>
        public static string HardDelete { get; } = "HardDelete";
    }
}
