using HomeBroker.Application.DTOs;

namespace HomeBroker.Application.ServiceInterfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken( UserDto user, IList<string> roles);
    }
}
