using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldPosition")]
public class TutorialPointerDataWorldPosition : TutorialPointerData
{
	private const float TopOffset = 1.5f;

	[SerializeField]
	private QuestEntryTarget addressTarget;

	[SerializeField]
	private Vector3 position;

	protected override TutorialPointerType GetTutorialPointerType()
	{
		return TutorialPointerType.World;
	}

	public override bool ShouldBeEnabled()
	{
		if (!(addressTarget == null))
		{
			if (BuildingManager.IsInsideBuilding)
			{
				return InstanceBehavior<BuildingManager>.Instance.building.Address == addressTarget.GetAddress();
			}
			return false;
		}
		return true;
	}

	public override void Relocate(TutorialPointer tutorialPointer)
	{
		Transform transform = tutorialPointer.transform;
		Quaternion rotation = Quaternion.LookRotation(GameManager.GetMainCamera().transform.position - transform.position);
		rotation.eulerAngles = new Vector3(0f, rotation.eulerAngles.y, rotation.eulerAngles.z);
		transform.rotation = rotation;
	}

	public override void OnShow(TutorialPointer tutorialPointer)
	{
		tutorialPointer.transform.position = position + Vector3.up * 1.5f;
	}
}
