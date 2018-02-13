namespace Sdl.Web.Tridion.Templates.R2.Data.TargetGroups.Model
{
    public class CustomerCharacteristicCondition : Condition
    {
        public string Name { get; set; }
        public ConditionOperator Operator { get; set; }
        public object Value { get; set; }
    }
}
