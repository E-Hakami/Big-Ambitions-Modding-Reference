using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldEducationDoor")]
public class TutorialPointerDataWorldEducationDoor : TutorialPointerDataWorldItem
{
	[SerializeField]
	[SearchableEnum]
	private DiplomaName diplomaName;

	[SerializeField]
	private Vector3 localOffset;

	public override void FindEntityController()
	{
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (allItemController is EducationDoorController educationDoorController && educationDoorController.StudyDiploma.diplomaName == diplomaName)
			{
				entityControllerTarget = educationDoorController;
				break;
			}
		}
	}

	public override void OnShow(TutorialPointer tutorialPointer)
	{
		FindEntityController();
		if (entityControllerTarget == null)
		{
			Debug.LogError("No item found for " + base.name);
		}
		else
		{
			tutorialPointer.transform.position = entityControllerTarget.transform.TransformPoint(localOffset);
		}
	}
}
