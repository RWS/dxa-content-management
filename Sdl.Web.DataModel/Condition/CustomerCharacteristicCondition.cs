namespace Sdl.Web.DataModel.Condition
{
    public class CustomerCharacteristicCondition : Condition
    {
        public string Name { get; set; }
        public ConditionOperator Operator { get; set; }
        public object Value { get; set; }
    }
}
