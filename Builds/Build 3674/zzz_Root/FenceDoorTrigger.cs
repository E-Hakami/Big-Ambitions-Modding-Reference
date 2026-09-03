using Helpers;
using NaughtyAttributes;
using UnityEngine;

public class FenceDoorTrigger : MonoBehaviour
{
	[Required(null)]
	public FenceDoor fenceDoor;

	[SerializeField]
	private bool ignoreLockedCondition;

	[Button(null, EButtonEnableMode.Always)]
	private void AutoSetup()
	{
		fenceDoor = GetComponentInParent<FenceDoor>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!ShouldIgnore(other))
		{
			fenceDoor.HandleTriggerEnter(other, ignoreLockedCondition);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		fenceDoor.HandleTriggerExit(other);
	}

	private bool ShouldIgnore(Collider other)
	{
		if (fenceDoor.IsForSale())
		{
			return true;
		}
		if (fenceDoor.acceptsVehicles)
		{
			return false;
		}
		int layer = other.gameObject.layer;
		if (layer != LayerHelper.PlayerLayerIndex && layer != LayerHelper.HumanLayerIndex)
		{
			return true;
		}
		if (layer == LayerHelper.PlayerLayerIndex && InstanceBehavior<GameManager>.Instance != null)
		{
			return InstanceBehavior<GameManager>.Instance.selectedVehicle != null;
		}
		return false;
	}
}
