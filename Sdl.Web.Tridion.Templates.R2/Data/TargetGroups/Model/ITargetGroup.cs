using System.Collections.Generic;

namespace Sdl.Web.Tridion.Templates.R2.Data.TargetGroups.Model
{
    public interface ITargetGroup
    {
        string Id { get; set; }
        string Title { get; set; }
        string Description { get; set; }
        IList<ICondition> Conditions { get; }
    }
}
