namespace Sdl.Web.Tridion.Templates.R2.Data.TargetGroups.Model
{
    public class TargetGroupCondition : Condition, ITargetGroupCondition
    {
        public TargetGroup TargetGroup { get; set; }

        ITargetGroup ITargetGroupCondition.TargetGroup => TargetGroup as ITargetGroup;
    }
}
