using UnityEngine;

namespace Tutorial;

public class TutorialPointerDataWorldItem : TutorialPointerData
{
	protected const float TopOffset = 1.5f;

	[SerializeField]
	protected QuestEntryTarget addressTarget;

	[SerializeField]
	private bool setPositionEveryFrame;

	protected EntityController entityControllerTarget;

	private float _itemControllerMeshExtentY;

	protected override TutorialPointerType GetTutorialPointerType()
	{
		return TutorialPointerType.World;
	}

	public override bool ShouldBeEnabled()
	{
		if (base.ShouldBeEnabled() && BuildingManager.IsInsideBuilding)
		{
			return InstanceBehavior<BuildingManager>.Instance.building.Address == addressTarget.GetAddress();
		}
		return false;
	}

	public override void Relocate(TutorialPointer tutorialPointer)
	{
		if (setPositionEveryFrame)
		{
			SetPosition(tutorialPointer);
		}
		Transform transform = tutorialPointer.transform;
		Vector3 forward = GameManager.GetMainCamera().transform.position - transform.position;
		forward.y = 0f;
		Quaternion rotation = Quaternion.LookRotation(forward);
		transform.rotation = rotation;
	}

	public override void OnShow(TutorialPointer tutorialPointer)
	{
		FindEntityController();
		if (entityControllerTarget == null)
		{
			Debug.LogError("No item found for " + base.name);
			return;
		}
		MeshFilter component = entityControllerTarget.GetComponent<MeshFilter>();
		if ((bool)component)
		{
			_itemControllerMeshExtentY = component.mesh.bounds.extents.y;
		}
		else
		{
			_itemControllerMeshExtentY = 0f;
		}
		SetPosition(tutorialPointer);
	}

	public virtual void FindEntityController()
	{
	}

	private void SetPosition(TutorialPointer tutorialPointer)
	{
		if (!(entityControllerTarget == null))
		{
			tutorialPointer.transform.position = entityControllerTarget.transform.position + Vector3.up * (_itemControllerMeshExtentY + 1.5f);
		}
	}
}
