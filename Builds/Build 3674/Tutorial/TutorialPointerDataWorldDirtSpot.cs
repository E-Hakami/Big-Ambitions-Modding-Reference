using System.Linq;
using Entities;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldDirtSpot")]
public class TutorialPointerDataWorldDirtSpot : TutorialPointerDataWorldItem
{
	public override void Relocate(TutorialPointer tutorialPointer)
	{
		DirtSpot dirtSpot = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.dirtSpots.OrderByDescending((DirtSpot x) => x.dirtiness).FirstOrDefault();
		if (dirtSpot != null && !(dirtSpot.dirtiness <= 0f))
		{
			tutorialPointer.transform.position = new Vector3(dirtSpot.x, 1.5f, dirtSpot.z);
			base.Relocate(tutorialPointer);
		}
	}

	public override void OnShow(TutorialPointer tutorialPointer)
	{
	}
}
