﻿using System.Collections.Generic;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Templates.R2.Data.TargetGroups.Model;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using AM = Tridion.ContentManager.AudienceManagement;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    public class AddTargetGroupsModelBuilder : DataModelBuilder, IEntityModelDataBuilder
    {
        public AddTargetGroupsModelBuilder(DataModelBuilderPipeline pipeline) : base(pipeline)
        {
            Logger.Debug("AddTargetGroupsModelBuilder initialized.");
        }

        public void BuildEntityModel(ref EntityModelData entityModelData, ComponentPresentation cp)
        {
            Logger.Debug("Adding target groups to entity model data.");
            if (cp.Conditions == null || cp.Conditions.Count <= 0) return;
            List<Condition> conditions = new List<Condition>();
            foreach (var condition in cp.Conditions)
            {
                var mapped = MapTargetGroupCondition(condition);
                if (mapped == null) continue;
                conditions.Add(mapped);
            }
            if (conditions.Count > 0)
            {
                entityModelData.SetExtensionData("TargetGroupConditions", conditions.ToArray());
            }
        }

        public void BuildEntityModel(ref EntityModelData entityModelData, Component component, ComponentTemplate ct,
            bool includeComponentTemplateDetails, int expandLinkDepth)
        {

        }

        private IList<Condition> MapConditions(IList<AM.Condition> conditions)
        {
            var mappedConditions = new List<Condition>();
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

        private TrackingKeyCondition MapTrackingKeyCondition(AM.TrackingKeyCondition trackingKeyCondition)
        {
            KeywordModelData kwd = Pipeline.CreateKeywordModel(trackingKeyCondition.Keyword, Pipeline.Settings.ExpandLinkDepth);
            return new TrackingKeyCondition
            {
                Operator = (ConditionOperator) trackingKeyCondition.Operator,
                Negate = true,
                Value = trackingKeyCondition.Value,
                TrackingKeyTitle = kwd.Title
            };
        }

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
