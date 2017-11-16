using System.Collections.Generic;
using Sdl.Web.DataModel.Condition;

namespace Sdl.Web.DataModel.TargetGroups
{
    public class TargetGroup : ITargetGroup
    {
        public string Description { get; set; }

        public IList<ICondition> Conditions { get; set; }
    }
}
