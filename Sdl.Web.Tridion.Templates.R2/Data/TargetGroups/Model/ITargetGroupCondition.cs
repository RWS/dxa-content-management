namespace Sdl.Web.Tridion.Templates.R2.Data.TargetGroups.Model
{
    public interface ITargetGroupCondition : ICondition
    {
        ITargetGroup TargetGroup { get; }
    }
}
