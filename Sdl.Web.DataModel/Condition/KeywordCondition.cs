namespace Sdl.Web.DataModel
{
    public class KeywordCondition : Condition
    {
        public KeywordModelData KeywordModelData { get; set; }
        public ConditionOperator Operator { get; set; }
        public object Value { get; set; }
    }
}
