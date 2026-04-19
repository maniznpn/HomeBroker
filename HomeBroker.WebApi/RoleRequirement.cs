using HomeBroker.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace HomeBroker.WebApi.Authorization
{
    public class RoleRequirement : IAuthorizationRequirement
    {
        public string[] AllowedRoles { get; }

        public RoleRequirement(params string[] roles)
        {
            AllowedRoles = roles;
        }
    }

    public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated ?? false)
            {
                throw new UnauthorizedException("User is not authenticated.");
            }

            var userRoles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role);

            if (!userRoles.Any())
            {
                throw new ForbiddenAccessException($"User does not have access to this resource.");
            }

            if (userRoles.Any(r => requirement.AllowedRoles.Contains(r.Value)))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            throw new ForbiddenAccessException(
                $"User role is not allowed. Required roles: {string.Join(", ", requirement.AllowedRoles)}");
        }
    }
}