using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldItemToInteractWithVehicle")]
public class TutorialPointerDataWorldItemToInteractWithVehicle : TutorialPointerDataWorldItemToInteract
{
	public override bool ShouldBeEnabled()
	{
		if (base.ShouldBeEnabled())
		{
			return !PlayerHelper.IsUsingVehicle;
		}
		return false;
	}
}
