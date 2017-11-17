namespace Sdl.Web.DataModel
{
    public interface ITargetGroupCondition : ICondition
    {
        ITargetGroup TargetGroup { get; }
    }
}
