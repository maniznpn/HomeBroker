using HomeBroker.Application.DTOs;

namespace HomeBroker.Application.ServiceInterfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    }
}
