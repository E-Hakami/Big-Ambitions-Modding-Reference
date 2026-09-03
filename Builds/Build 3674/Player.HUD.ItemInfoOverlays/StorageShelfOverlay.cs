using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class StorageShelfOverlay : IOverlay
{
	[Header("Storage Shelf")]
	[SerializeField]
	private Button manageStorageButton;

	[SerializeField]
	private Button addItemsToStorageButton;

	private void Start()
	{
		manageStorageButton.onClick.AddListener(delegate
		{
			((StorageShelfController)linkedController).ManageStorage();
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		});
		addItemsToStorageButton.onClick.AddListener(delegate
		{
			((StorageShelfController)linkedController).AddItemsToStorage();
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		});
	}

	public override bool IsValid(EntityController entityController)
	{
		return entityController is StorageShelfController;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		CtaManager.UpdateCta(entityController);
		return CtaManager.ctaAction == null;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
	}
}
