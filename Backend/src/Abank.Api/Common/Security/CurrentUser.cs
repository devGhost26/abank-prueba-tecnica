using Abank.Application.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Abank.Api.Common.Security;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public int? Id =>
        int.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : null;
}
