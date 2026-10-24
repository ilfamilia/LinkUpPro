using LinkUpPro.Core.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace LinkUpPro.Infrastructure.Identity.Services
{
    public class CurrentUserContext : ICurrentUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;

                return user?.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            }
        }
    }
}