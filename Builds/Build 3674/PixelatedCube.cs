using System;
using UnityEngine;

public class PixelatedCube : MonoBehaviour
{
	[SerializeField]
	private MeshRenderer meshRenderer;

	private bool _enabled;

	private void OnEnable()
	{
		GlobalEvents.onCurrentHeightChanged = (Action)Delegate.Combine(GlobalEvents.onCurrentHeightChanged, new Action(OnCurrentHeightChanged));
		Disable();
	}

	private void OnDisable()
	{
		GlobalEvents.onCurrentHeightChanged = (Action)Delegate.Remove(GlobalEvents.onCurrentHeightChanged, new Action(OnCurrentHeightChanged));
	}

	private void OnCurrentHeightChanged()
	{
		if (_enabled)
		{
			MultipleHeightsBuildingController multipleHeightsBuildingController = InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController;
			if (!(multipleHeightsBuildingController == null))
			{
				bool positionVisible = multipleHeightsBuildingController.GetPositionVisible(base.transform.position);
				Toggle(positionVisible);
			}
		}
	}

	public void Enable()
	{
		_enabled = true;
		MultipleHeightsBuildingController multipleHeightsBuildingController = InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController;
		if (multipleHeightsBuildingController == null)
		{
			Toggle(toggle: true);
		}
		else if (multipleHeightsBuildingController.GetPositionVisible(base.transform.position))
		{
			Toggle(toggle: true);
		}
	}

	public void Disable()
	{
		_enabled = false;
		Toggle(toggle: false);
	}

	public void Toggle(bool toggle)
	{
		meshRenderer.enabled = toggle;
	}
}
