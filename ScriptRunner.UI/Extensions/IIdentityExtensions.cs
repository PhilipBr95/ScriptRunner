using System.Collections.Generic;
using System.Security.Principal;

namespace ScriptRunner.UI.Extensions
{
	public static class IIdentityExtensions
	{
		private static List<string> _groups = null;
		public static IEnumerable<string> Groups(this IIdentity identity)
		{
			if(_groups != null) return _groups.ToArray();

			_groups = new List<string>();//save all your groups' name
			var wi = (WindowsIdentity)identity;
			if (wi?.Groups != null)
			{
				foreach (var group in wi.Groups)
				{
					try
					{
						_groups.Add(group.Translate(typeof(NTAccount)).ToString());
					}
					catch (Exception e)
					{
						// ignored
					}
				}

				return _groups;
			}

			return Array.Empty<string>();
		}

	}
}
