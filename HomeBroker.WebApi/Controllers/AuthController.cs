using HomeBroker.Application.DTOs;
using HomeBroker.Application.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeBroker.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// User login endpoint
        /// </summary>
        /// <param name="request">Login request with email and password</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid request. Email and password are required."
                });
            }

            try
            {
                var result = await _authService.LoginAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning($"Login failed for email: {request.Email}");
                    return Unauthorized(result);
                }

                _logger.LogInformation($"User {request.Email} logged in successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred during login: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new LoginResponse
                    {
                        Success = false,
                        Message = "An error occurred during login. Please try again."
                    });
            }
        }

        /// <summary>
        /// Register a new user as either a Broker or HouseSeeker.
        /// </summary>
        /// <remarks>
        /// POST /api/auth/register
        ///
        ///     {
        ///       "email": "jane@example.com",
        ///       "password": "Passw0rd!",
        ///       "fullName": "Jane Doe",
        ///       "phoneNumber": "+977-9801234567",
        ///       "role": "Broker"
        ///     }
        ///
        /// Role must be "Broker" or "HouseSeeker".
        /// </remarks>
        [HttpPost("register")]
        [Authorize(Policy ="AdminPolicy")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new RegisterResponse
                {
                    Success = false,
                    Message = "Validation failed.",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                _logger.LogWarning("Registration failed for {Email}: {Message}", request.Email, result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
