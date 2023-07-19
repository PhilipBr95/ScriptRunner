using Microsoft.AspNetCore.Authorization;

namespace ScriptRunner.UI.Auth
{
    public class CheckADGroupRequirement : IAuthorizationRequirement
    {
        public string GroupName { get; private set; }

        public CheckADGroupRequirement(string groupName)
        {
            GroupName = groupName;
        }
    }
}