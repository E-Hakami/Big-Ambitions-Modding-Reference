using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InteriorDesigner.InteriorElements;
using Helpers;
using InteriorDesign;
using UI.InteriorDesigner;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/InteriorScoreIsValue")]
public class TutorialPointerHideConditionInteriorScoreIsValue : TutorialPointerHideCondition
{
	[SerializeField]
	private CustomBuildingTarget playerStoreTarget;

	[SerializeField]
	private int score;

	[SerializeField]
	private bool isGreaterThan;

	protected override bool ConditionMetInternal()
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(playerStoreTarget.GetAddress());
		if (buildingRegistration == null)
		{
			return false;
		}
		List<SerializedInteriorDesign> designs = buildingRegistration.interiorDesigns;
		if (InteriorDesignerUI.IsOpen && InstanceBehavior<BuildingManager>.Instance.buildingRegistration == buildingRegistration)
		{
			designs = InteriorElementsHelper.InteriorElementsCache.Select((KeyValuePair<string, InteriorElement> x) => x.Value.Serialize()).ToList();
		}
		int interiorScorePercentage = InteriorScoreCalculator.GetInteriorScorePercentage(designs);
		if (isGreaterThan)
		{
			return interiorScorePercentage >= score;
		}
		return interiorScorePercentage < score;
	}
}
