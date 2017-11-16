using System.Collections.Generic;
using Sdl.Web.DataModel.Condition;

namespace Sdl.Web.DataModel.TargetGroups
{
    public interface ITargetGroup
    {
        string Description { get; set; }
        IList<ICondition> Conditions { get; }
    }
}
