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

            foreach (var componentPresentation in page.ComponentPresentations)
            {
                if (componentPresentation.Conditions != null && componentPresentation.Conditions.Count > 0)
                {
                    pageModelData.Conditions = (List<ICondition>)MapTargetGroupConditions(
                        componentPresentation.Conditions);
                }               
            }
        }

        public TargetGroup BuildTargetGroup(AM.TargetGroup targetGroup) 
            => new TargetGroup
        {
            Conditions = MapConditions(targetGroup.Conditions),
            Description = targetGroup.Description,
            Id = targetGroup.Id,
            Title = targetGroup.Title
            // OwningPublication = PublicationBuilder.BuildPublication(targetGroup.OwningRepository),
            // Publication = PublicationBuilder.BuildPublication(targetGroup.ContextRepository),
            // PublicationId = targetGroup.ContextRepository.Id,
        };

        public IList<ICondition> MapTargetGroupConditions(IList<AM.TargetGroupCondition> componentPresentationConditions)
        {
            var mappedConditions = new List<ICondition>();
            foreach (var componentPresentationCondition in componentPresentationConditions)
            {
                mappedConditions.AddRange(
                    MapConditions(componentPresentationCondition.TargetGroup.Conditions));
            }
            return mappedConditions;
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

        private CustomerCharacteristicCondition MapCustomerCharacteristicCondition(AM.CustomerCharacteristicCondition condition)
        {
            var newCondition = new CustomerCharacteristicCondition()
            {
                Value = condition.Value,
                Operator = (ConditionOperator)condition.Operator,
                Name = condition.Name,
                Negate = condition.Negate
            };
            return newCondition;
        }

        private TargetGroupCondition MapTargetGroupCondition(AM.TargetGroupCondition targetGroupCondition)
        {
            var newCondition = new TargetGroupCondition()
            {
                TargetGroup = BuildTargetGroup(targetGroupCondition.TargetGroup),
                Negate = targetGroupCondition.Negate
            };
            return newCondition;
        }

        private KeywordCondition MapTrackingKeyCondition(AM.TrackingKeyCondition trackingKeyCondition)
        {
            var newCondition = new KeywordCondition
            {
                KeywordModelData = Pipeline.CreateKeywordModel(trackingKeyCondition.Keyword, Pipeline.Settings.ExpandLinkDepth),
                Operator = (ConditionOperator)trackingKeyCondition.Operator,
                Negate = true,
                Value = trackingKeyCondition.Value
            };
            return newCondition;
        }
    }
}
