namespace Sdl.Web.Tridion.Templates.R2.Data.TargetGroups.Model
{
    public class TrackingKeyCondition : Condition
    {
        public string TrackingKeyTitle { get; set; }
        public ConditionOperator Operator { get; set; }
        public object Value { get; set; }
    }
}
