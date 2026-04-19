namespace HomeBroker.Application.IServiceInterfaces.IUserMeta
{
    public interface IUserMeta
    {
        /// <summary>
        /// Extracts the user ID from the token in the HTTP context.
        /// </summary>
        /// <returns>The user ID as a long.</returns>
        long GetUserId();

        /// <summary>
        /// Extracts the email from the token in the HTTP context.
        /// </summary>
        /// <returns>The email as a string.</returns>
        string? GetEmail();

        /// <summary>
        /// Extracts the username from the token in the HTTP context.
        /// </summary>
        /// <returns>The username as a string.</returns>
        string? GetUserName();

        /// <summary>
        /// Extracts the role from the token in the HTTP context.
        /// </summary>
        /// <returns>The role as a string.</returns>
        string? GetRole();

    }

    public class UserMetaDto
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
