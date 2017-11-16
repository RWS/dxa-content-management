using System.Collections.Generic;
using Sdl.Web.DataModel;
using Tridion.ContentManager.CommunicationManagement;
using Dxa = Sdl.Web.DataModel.Condition;
using AM = Tridion.ContentManager.AudienceManagement;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    public class AddTargetGroupsModelBuilder : DataModelBuilder, IPageModelDataBuilder
    {
        public AddTargetGroupsModelBuilder(DataModelBuilderPipeline pipeline) : base(pipeline)
        {
        }

        public void BuildPageModel(ref PageModelData pageModelData, Page page)
        {
            foreach (var componentPresentation in page.ComponentPresentations)
            {
                if (componentPresentation.Conditions != null && componentPresentation.Conditions.Count > 0)
                {
                    pageModelData.Conditions = (List<Dxa.ICondition>)MapTargetGroupConditions(
                        componentPresentation.Conditions);
                }               
            }
        }

        public DataModel.TargetGroups.TargetGroup BuildTargetGroup(AM.TargetGroup targetGroup)
        {
            var tg = new DataModel.TargetGroups.TargetGroup
            {
                Conditions = MapConditions(targetGroup.Conditions),
                Description = targetGroup.Description,
               // Id = targetGroup.Id,
               // OwningPublication = PublicationBuilder.BuildPublication(targetGroup.OwningRepository),
               // Publication = PublicationBuilder.BuildPublication(targetGroup.ContextRepository),
               // PublicationId = targetGroup.ContextRepository.Id,
               // Title = targetGroup.Title
            };
            return tg;
        }

        public IList<Dxa.ICondition> MapTargetGroupConditions(IList<AM.TargetGroupCondition> componentPresentationConditions)
        {
            var mappedConditions = new List<Dxa.ICondition>();
            foreach (var componentPresentationCondition in componentPresentationConditions)
            {
                mappedConditions.AddRange(
                    MapConditions(componentPresentationCondition.TargetGroup.Conditions));
            }
            return mappedConditions;
        }

        private IList<Dxa.ICondition> MapConditions(IList<AM.Condition> conditions)
        {
            var mappedConditions = new List<Dxa.ICondition>();
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
                    //log.Warning("Condition of type: " + condition.GetType().FullName + " was not supported by the mapping code.");
                }
            }
            return mappedConditions;
        }

        private Dxa.CustomerCharacteristicCondition MapCustomerCharacteristicCondition(AM.CustomerCharacteristicCondition condition)
        {
            var newCondition = new Dxa.CustomerCharacteristicCondition()
            {
                Value = condition.Value,
                Operator = (Dxa.ConditionOperator)condition.Operator,
                Name = condition.Name,
                Negate = condition.Negate
            };
            return newCondition;
        }

        private Dxa.TargetGroupCondition MapTargetGroupCondition(AM.TargetGroupCondition targetGroupCondition)
        {
            var newCondition = new Dxa.TargetGroupCondition()
            {
                TargetGroup = BuildTargetGroup(targetGroupCondition.TargetGroup),
                Negate = targetGroupCondition.Negate
            };
            return newCondition;
        }

        private Dxa.KeywordCondition MapTrackingKeyCondition(AM.TrackingKeyCondition trackingKeyCondition)
        {
            var newCondition = new Dxa.KeywordCondition
            {
                KeywordModelData = Pipeline.CreateKeywordModel(trackingKeyCondition.Keyword, Pipeline.Settings.ExpandLinkDepth),
                Operator = (Dxa.ConditionOperator)trackingKeyCondition.Operator,
                Negate = true,
                Value = trackingKeyCondition.Value
            };
            return newCondition;
        }
    }
}
