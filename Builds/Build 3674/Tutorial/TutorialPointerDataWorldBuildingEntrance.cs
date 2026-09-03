using System.Linq;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldBuildingEntrance")]
public class TutorialPointerDataWorldBuildingEntrance : TutorialPointerData
{
	private const float TopOffset = 4.5f;

	[SerializeField]
	private QuestEntryTarget addressTarget;

	[SerializeField]
	private float zForwardOffset;

	protected override TutorialPointerType GetTutorialPointerType()
	{
		return TutorialPointerType.World;
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
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.cityBuildingControllers.FirstOrDefault((CityBuildingController x) => x.building.Address == addressTarget.GetAddress());
		if (cityBuildingController == null)
		{
			Debug.LogError($"No building controller found for {base.name} (address: {addressTarget.GetAddress()}");
			return;
		}
		Transform doorTransform = cityBuildingController.entranceDoors.First().doorTransform;
		tutorialPointer.transform.position = doorTransform.position + Vector3.up * 4.5f + doorTransform.forward * zForwardOffset;
	}
}
