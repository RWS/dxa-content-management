using Sdl.Web.DataModel.TargetGroups;

namespace Sdl.Web.DataModel.Condition
{
    public interface ITargetGroupCondition : ICondition
    {
        ITargetGroup TargetGroup { get; }
    }
}
