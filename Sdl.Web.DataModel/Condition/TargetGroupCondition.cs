using Sdl.Web.DataModel.TargetGroups;

namespace Sdl.Web.DataModel.Condition
{
    public class TargetGroupCondition : Condition, ITargetGroupCondition
    {
        public TargetGroup TargetGroup { get; set; }

        ITargetGroup ITargetGroupCondition.TargetGroup => TargetGroup as ITargetGroup;
    }
}
