using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Tridion.ContentManager.AudienceManagement;

namespace Sdl.Web.Tridion.Templates.DD4T
{
    internal static class ContextExpressionManager
    {
        private static readonly Regex _titleRegex = new Regex(@"^[\p{L}_][\p{L}\p{N}_]*\.[\p{L}_][\p{L}\p{N}_]*$", RegexOptions.Compiled | RegexOptions.Singleline);

        internal static bool HasContextExpression(TargetGroup targetGroup)
        {
            // TODO: Also check for CE App Data (?)
            return _titleRegex.IsMatch(targetGroup.Title);
        }

        internal static string[] GetContextExpressions(IEnumerable<TargetGroup> targetGroups)
        {
            return targetGroups.Where(HasContextExpression).Select(tg => tg.Title).ToArray();
        }
    }
}
