using Sdl.Web.DataModel;

namespace Sdl.Web.Tridion.Templates.R2.Data.TargetGroups.Model
{
    public class KeywordCondition : Condition
    {
        public KeywordModelData KeywordModelData { get; set; }
        public ConditionOperator Operator { get; set; }
        public object Value { get; set; }
    }
}
