using System.ComponentModel.DataAnnotations;

namespace HomeBroker.Application.DTOs
{
    /// <summary>
    /// Registration request — used by both house seekers and brokers.
    /// The Role field controls which Identity role is assigned.
    /// </summary>
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        /// <summary>
        /// "Broker" or "HouseSeeker" — validated server-side.
        /// </summary>
        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public IEnumerable<string>? Errors { get; set; }
    }
}
