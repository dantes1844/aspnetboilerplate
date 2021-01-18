using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Runtime.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Abp.Authorization
{
    public class AbpUserClaimsPrincipalFactory<TUser, TRole> : UserClaimsPrincipalFactory<TUser, TRole>, ITransientDependency
        where TRole : AbpRole<TUser>, new()
        where TUser : AbpUser<TUser>
    {
        public AbpUserClaimsPrincipalFactory(
            AbpUserManager<TRole, TUser> userManager,
            AbpRoleManager<TRole, TUser> roleManager,
            IOptions<IdentityOptions> optionsAccessor
            ) : base(userManager, roleManager, optionsAccessor)
        {

        }

        /// <summary>
        /// 通过AspNetCore.Identity的方法来创建Principal，并增加一个租户的Claim
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [UnitOfWork]
        public override async Task<ClaimsPrincipal> CreateAsync(TUser user)
        {
            var principal = await base.CreateAsync(user);

            if (user.TenantId.HasValue)
            {
                principal.Identities.First().AddClaim(new Claim(AbpClaimTypes.TenantId, user.TenantId.ToString()));
            }

            return principal;
        }
    }
}