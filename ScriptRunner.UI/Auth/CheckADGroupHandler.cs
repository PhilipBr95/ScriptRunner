using Microsoft.AspNetCore.Authorization;
using System.Security.Principal;

namespace ScriptRunner.UI.Auth
{
    public class CheckADGroupHandler : AuthorizationHandler<CheckADGroupRequirement>
    {
        private ILogger _logger;
        public CheckADGroupHandler(ILoggerFactory loggerFactory) => _logger = loggerFactory.CreateLogger(GetType().FullName);

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                       CheckADGroupRequirement requirement)
        {
            //The 1st time round they might be auth'd
            if (!context.User.Identity.IsAuthenticated)
            {
                _logger?.LogDebug($"Unauthenticated(Early) user...");
                return Task.CompletedTask;
            }

            //var isAuthorized = context.User.IsInRole(requirement.GroupName);

            var groups = new List<string>();//save all your groups' name
            var wi = (WindowsIdentity)context?.User?.Identity;
            if (wi?.Groups != null)
            {
                foreach (var group in wi.Groups)
                {
                    try
                    {
                        groups.Add(group.Translate(typeof(NTAccount)).ToString());
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }
                }

                if (groups.Contains(requirement.GroupName))//do the check
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            _logger?.LogWarning($"Unauthenticated user [{context?.User?.Identity?.Name}]...");
            return Task.CompletedTask;
        }
    }
}