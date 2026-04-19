using HomeBroker.Application.Common;
using HomeBroker.Application.DTOs;
using HomeBroker.Application.ServiceInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HomeBroker.Infrastructure.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtConfiguration _jwtConfig;
        private readonly ILogger<JwtTokenService> _logger;

        public JwtTokenService(
            IOptions<JwtConfiguration> jwtConfig,
            ILogger<JwtTokenService> logger)
        {
            _jwtConfig = jwtConfig.Value;
            _logger = logger;
        }

        public string GenerateToken(UserDto user, IList<string> roles)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtConfig.JwtSecurityKey);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.UserName),
                };

                // Add roles as claims
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(24),
                    Issuer = _jwtConfig.Issuer,
                    Audience = _jwtConfig.Audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation($"JWT token generated for user: {user.Email}");

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while generating JWT token: {ex.Message}");
                throw;
            }
        }
    }
}
