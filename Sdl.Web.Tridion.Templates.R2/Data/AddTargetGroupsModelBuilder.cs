using System.Collections.Generic;
using Sdl.Web.DataModel;
using Tridion.ContentManager.CommunicationManagement;
using AM = Tridion.ContentManager.AudienceManagement;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    public class AddTargetGroupsModelBuilder : DataModelBuilder, IPageModelDataBuilder
    {
        public AddTargetGroupsModelBuilder(DataModelBuilderPipeline pipeline) : base(pipeline)
        {
            Logger.Debug("AddTargetGroupsModelBuilder initialized.");
        }

        public void BuildPageModel(ref PageModelData pageModelData, Page page)
        {
            Logger.Debug("Adding target groups to page model data.");
            foreach (var cp in page.ComponentPresentations)
            {
                if (cp.Conditions == null || cp.Conditions.Count <= 0) continue;
                List<ICondition> conditions = new List<ICondition>();
                foreach (var condition in cp.Conditions)
                {
                    var mapped = MapConditions(condition.TargetGroup.Conditions);
                    if (mapped == null || mapped.Count <= 0) continue;
                    conditions.AddRange(mapped);
                }
                if (conditions.Count <= 0) continue;
                pageModelData.Conditions = conditions;
            }
        }
      
        private IList<ICondition> MapConditions(IList<AM.Condition> conditions)
        {
            var mappedConditions = new List<ICondition>();
            foreach (var condition in conditions)
            {
                if (condition is AM.TrackingKeyCondition)
                {
                    mappedConditions.Add(MapTrackingKeyCondition((AM.TrackingKeyCondition)condition));
                }
                else if (condition is AM.TargetGroupCondition)
                {
                    mappedConditions.Add(MapTargetGroupCondition((AM.TargetGroupCondition)condition));
                }
                else if (condition is AM.CustomerCharacteristicCondition)
                {
                    mappedConditions.Add(MapCustomerCharacteristicCondition((AM.CustomerCharacteristicCondition)condition));
                }
                else
                {
                    Logger.Warning("Condition of type: " + condition.GetType().FullName + " was not supported by the mapping code.");
                }
            }
            return mappedConditions;
        }

        private CustomerCharacteristicCondition MapCustomerCharacteristicCondition(AM.CustomerCharacteristicCondition condition) => new CustomerCharacteristicCondition()
        {
            Value = condition.Value,
            Operator = (ConditionOperator)condition.Operator,
            Name = condition.Name,
            Negate = condition.Negate
        };

        private TargetGroupCondition MapTargetGroupCondition(AM.TargetGroupCondition targetGroupCondition) => new TargetGroupCondition()
        {
            TargetGroup = MapTargetGroup(targetGroupCondition.TargetGroup),
            Negate = targetGroupCondition.Negate
        };

        private KeywordCondition MapTrackingKeyCondition(AM.TrackingKeyCondition trackingKeyCondition) => new KeywordCondition
        {
            KeywordModelData = Pipeline.CreateKeywordModel(trackingKeyCondition.Keyword, Pipeline.Settings.ExpandLinkDepth),
            Operator = (ConditionOperator)trackingKeyCondition.Operator,
            Negate = true,
            Value = trackingKeyCondition.Value
        };

        public TargetGroup MapTargetGroup(AM.TargetGroup targetGroup) => new TargetGroup
        {
            Conditions = MapConditions(targetGroup.Conditions),
            Description = targetGroup.Description,
            Id = targetGroup.Id,
            Title = targetGroup.Title
            // OwningPublication = PublicationBuilder.BuildPublication(targetGroup.OwningRepository),
            // Publication = PublicationBuilder.BuildPublication(targetGroup.ContextRepository),
            // PublicationId = targetGroup.ContextRepository.Id,
        };
    }
}
