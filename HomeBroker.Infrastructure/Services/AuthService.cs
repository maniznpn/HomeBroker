using HomeBroker.Application.DTOs;
using HomeBroker.Application.ServiceInterfaces;
using HomeBroker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HomeBroker.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private static readonly HashSet<string> AllowedRoles =
            new(StringComparer.OrdinalIgnoreCase) { "Broker", "HouseSeeker" };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwtTokenService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user as either a Broker or HouseSeeker.
        /// </summary>
        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            // Validate role input early
            if (!AllowedRoles.Contains(request.Role))
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Invalid role. Must be 'Broker', 'HouseSeeker'",
                    Errors = new[] { $"Role '{request.Role}' is not valid." }
                };
            }

            // Check for duplicate email
            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing != null)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "A user with this email already exists.",
                    Errors = new[] { "Email is already taken." }
                };
            }

            var role = Enum.TryParse<UserRole>(request.Role, true, out var parsedRole)
                ? parsedRole
                : UserRole.HouseSeeker;

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpperInvariant(),
                NormalizedUserName = request.Email.ToUpperInvariant(),
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true, // MVP: skip email verification
                UserRole = role
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Registration failed.",
                    Errors = result.Errors.Select(e => e.Description)
                };
            }

            // Assign the Identity role
            await _userManager.AddToRoleAsync(user, request.Role);

            _logger.LogInformation("User {Email} registered as {Role}", request.Email, request.Role);

            return new RegisterResponse
            {
                Success = true,
                Message = $"Registration successful. You are registered as a {request.Role}."
            };
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                    return new LoginResponse { Success = false, Message = "Email and password are required." };

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: user {Email} not found", request.Email);
                    return new LoginResponse { Success = false, Message = "Invalid email or password." };
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Login failed: bad password for {Email}", request.Email);
                    return new LoginResponse { Success = false, Message = "Invalid email or password." };
                }

                var roles = await _userManager.GetRolesAsync(user);
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = request.Email,
                    FullName = user.FullName,
                    UserName = user.UserName!
                };
                var token = _jwtTokenService.GenerateToken(userDto, roles);

                _logger.LogInformation("User {Email} logged in", request.Email);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        UserName = user.UserName!,
                        FullName = user.FullName
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return new LoginResponse { Success = false, Message = "An error occurred during login." };
            }
        }
    }
}
