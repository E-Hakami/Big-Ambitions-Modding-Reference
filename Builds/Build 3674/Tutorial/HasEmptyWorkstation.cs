using System.Linq;
using BigAmbitions.Items;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasEmptyWorkstation")]
public class HasEmptyWorkstation : QuestRequirement
{
	[SerializeField]
	private QuestEntryTarget questEntryTarget;

	[SerializeField]
	private ItemType workStationType;

	public override bool CheckIfCompleted()
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(questEntryTarget.GetAddress());
		if (buildingRegistration == null)
		{
			return false;
		}
		return buildingRegistration.itemInstances.Values.Any((ItemInstance x) => (x.ItemCached.type & workStationType) != 0 && !ItemHelper.HasAnyMissingRequirements(x) && buildingRegistration.scheduleDays.All((ScheduleDay y) => y.workShifts.All((WorkShift z) => z.itemInstanceId != x.id)));
	}
}
