using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public abstract class IOverlay : MonoBehaviour
{
	[ReadOnly]
	public EntityController linkedController;

	[Header("InfoOverlay Settings")]
	[Tooltip("While the popup is visible, the overlay should refresh on these events")]
	[SerializeField]
	internal DynamicOverlayUpdateType dynamicUpdateType;

	[Tooltip("If a priority overlay is active, it prevents all other components in the overlay to show")]
	[SerializeField]
	internal bool isPriority;

	[SerializeField]
	internal bool isVisibleInBlueprintMode;

	[Tooltip("This list should contain all interactables that should be moved under the mouse when the overlay is active order by priority")]
	public List<Transform> interactablePriority;

	[SerializeField]
	internal GameObject splitterAbove;

	public abstract bool IsValid(EntityController entityController);

	public abstract bool ShouldShow(EntityController entityController);

	public virtual bool ShouldTakePriority(EntityController entityController)
	{
		return isPriority;
	}

	public abstract void UpdateOverlay(EntityController entityController);

	public Transform GetPrimaryInteractable()
	{
		for (int i = 0; i < interactablePriority.Count; i++)
		{
			if (interactablePriority[i].gameObject.activeSelf)
			{
				return interactablePriority[i];
			}
		}
		return null;
	}

	public void SetActive(bool active)
	{
		base.gameObject.SetActive(active);
		if ((bool)splitterAbove)
		{
			splitterAbove.SetActive(active);
		}
	}
}
