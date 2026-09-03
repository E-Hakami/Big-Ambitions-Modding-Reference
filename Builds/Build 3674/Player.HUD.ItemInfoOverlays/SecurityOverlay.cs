using BigAmbitions.Tags;
using Localizor;
using TMPro;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class SecurityOverlay : IOverlay
{
	[SerializeField]
	private TMP_Text securityField;

	public override bool IsValid(EntityController entityController)
	{
		if (entityController is ShelfController shelfController && !(shelfController is StorageShelfController) && !(shelfController is PalletController))
		{
			return !(shelfController is DeliverySpot);
		}
		return false;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && InstanceBehavior<BuildingManager>.Instance.businessType.HasTag(TagRef.Businesstag.allowtheft))
		{
			return IsValid(entityController);
		}
		return false;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		ShelfController shelfController = (ShelfController)entityController;
		securityField.text = (shelfController.ItemInstance.isSecured ? "itemoverlay_security_info_protected".Localize().ToString() : "itemoverlay_security_info_not_protected".Localize().ToString());
		securityField.color = (shelfController.ItemInstance.isSecured ? InstanceBehavior<GlobalReferences>.Instance.colors.blue : InstanceBehavior<GlobalReferences>.Instance.colors.red);
	}
}
