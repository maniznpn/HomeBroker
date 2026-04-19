using HomeBroker.Application.IServiceInterfaces.IUserMeta;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace HomeBroker.Infrastructure.Services.UserMeta
{
    public class UserMeta : IUserMeta
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserMeta(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long GetUserId()
        {
            var token = GetTokenFromHttpContext();
            var claimValue = GetClaimValue(token, "nameid");
            return long.TryParse(claimValue, out var userId) ? userId : 0;
        }

        public string? GetEmail()
        {
            var token = GetTokenFromHttpContext();
            return GetClaimValue(token, "email");
        }

        public string? GetUserName()
        {
            var token = GetTokenFromHttpContext();
            return GetClaimValue(token, "userName");
        }

        public string? GetRole()
        {
            var token = GetTokenFromHttpContext();
            return GetClaimValue(token, "role");
        }

        private string GetTokenFromHttpContext()
        {
            var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                throw new InvalidOperationException("Authorization header is missing or invalid.");
            }

            return authorizationHeader.Replace("Bearer ", string.Empty);
        }

        private string? GetClaimValue(string token, string claimType)
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                throw new ArgumentException("Invalid token format.");
            }

            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }
    }
}
