using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Tridion.ContentManager.AudienceManagement;

namespace Sdl.Web.Tridion
{
    /// <summary>
    /// Utility methods for working with Context Expression Target Groups.
    /// </summary>
    public static class ContextExpressionUtils
    {
        private static readonly Regex _titleRegex = new Regex(@"^[\p{L}_][\p{L}\p{N}_]*\.[\p{L}_][\p{L}\p{N}_]*$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Determines whether a given Target Group has a Context Expression
        /// </summary>
        /// <param name="targetGroup">The Target Group to test.</param>
        public static bool HasContextExpression(this TargetGroup targetGroup)
            => _titleRegex.IsMatch(targetGroup.Title);

        /// <summary>
        /// Gets the Context Expressions of a given set of Target Groups.
        /// </summary>
        /// <param name="targetGroups">The Target Groups to get the Context Expressions for.</param>
        public static string[] GetContextExpressions(IEnumerable<TargetGroup> targetGroups)
            => targetGroups.Where(HasContextExpression).Select(tg => tg.Title).ToArray();
    }
}
