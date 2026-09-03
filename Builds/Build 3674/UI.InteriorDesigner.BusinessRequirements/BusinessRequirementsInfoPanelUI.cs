using System;
using System.Collections.Generic;
using BigAmbitions.Tags;
using Buildings;
using Buildings.BuildingTypes.Shared.BusinessRequirement;
using Buildings.Indoors.InteriorDesign;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using UnityEngine;

namespace UI.InteriorDesigner.BusinessRequirements;

public class BusinessRequirementsInfoPanelUI : FoldingInfoPanelUI
{
	[SerializeField]
	private BusinessRequirementTemplate entryTemplate;

	public override bool ShouldShow()
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			return BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.hasbusinessrequirements);
		}
		return false;
	}

	public override void OnEnterInteriorDesignerMode()
	{
		InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity = (Action)Delegate.Combine(InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity, new Action(UpdateBusinessRequirements));
		CoroutineUtility.RunAfterOneFrame(UpdateBusinessRequirements);
	}

	public override void OnExitInteriorDesignerMode()
	{
		InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity = (Action)Delegate.Remove(InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity, new Action(UpdateBusinessRequirements));
	}

	private void UpdateBusinessRequirements()
	{
		bool flag = ShouldShow();
		base.gameObject.SetActive(flag);
		if (!flag)
		{
			return;
		}
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		List<BusinessRequirement> businessRequirements = BusinessTypeHelper.GetData(buildingRegistration).businessRequirements;
		if (businessRequirements == null || businessRequirements.Count == 0)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		entryTemplate.transform.ResetTemplate();
		foreach (BusinessRequirement item in businessRequirements)
		{
			BusinessRequirementTemplate businessRequirementTemplate = UnityEngine.Object.Instantiate(entryTemplate, entryTemplate.transform.parent);
			businessRequirementTemplate.SetUp(item.GetLocalizeKey(), item.IsRequirementMet(buildingRegistration), item.GetHelpLink(buildingRegistration));
			businessRequirementTemplate.gameObject.SetActive(value: true);
		}
	}
}
