using Microsoft.AspNetCore.Identity;

namespace HomeBroker.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<long>
    {
        public UserRole UserRole { get; set; }
        public string FullName { get; set; }
    }

    public enum UserRole
    {
        HouseSeeker,
        Broker,
        Admin
    }
}
