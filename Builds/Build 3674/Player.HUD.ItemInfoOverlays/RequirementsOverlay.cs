using System.Collections.Generic;
using BigAmbitions.Items;
using Controllers;
using Extensions;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class RequirementsOverlay : IOverlay
{
	[Header("Requirements")]
	[SerializeField]
	private RequirementOverlayEntry requirementTemplate;

	[SerializeField]
	private TextLocalizationComponent requirementHeader;

	[SerializeField]
	private List<FurnitureRequirement> hiddenBrowsingRequirements = new List<FurnitureRequirement>();

	public override bool IsValid(EntityController entityController)
	{
		if (!(entityController is ItemController itemController))
		{
			return false;
		}
		return itemController.Item.furnitureRequirements.Count > 0;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (!(entityController is ItemController itemController))
		{
			return false;
		}
		if (CanPlayerBuyItem(itemController))
		{
			return HasVisibleBrowsingRequirements(itemController);
		}
		return IsPlayerOwnedAndMissingRequirements(itemController);
	}

	private bool HasVisibleBrowsingRequirements(ItemController itemController)
	{
		return itemController.Item.furnitureRequirements.Exists((FurnitureRequirement requirement) => !hiddenBrowsingRequirements.Exists((FurnitureRequirement hidden) => hidden.name == requirement.name));
	}

	public override bool ShouldTakePriority(EntityController entityController)
	{
		if (isPriority && entityController is ItemController itemController)
		{
			return IsPlayerOwnedAndMissingRequirements(itemController);
		}
		return false;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		if (entityController is ItemController itemController)
		{
			requirementTemplate.transform.ResetTemplate();
			if (CanPlayerBuyItem(itemController))
			{
				ShowBrowsingItemRequirement(itemController);
			}
			else
			{
				ShowPurchasedItemRequirements(itemController);
			}
		}
	}

	private static bool CanPlayerBuyItem(ItemController itemController)
	{
		PlayerItemPurchaserSettings playerItemPurchaserSettings = itemController.playerItemPurchaserSettings;
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && playerItemPurchaserSettings != null && playerItemPurchaserSettings.enabled && playerItemPurchaserSettings.itemName == itemController.itemName)
		{
			PlayerItemPurchaser playerItemPurchaser = itemController.PlayerItemPurchaser;
			if (playerItemPurchaser != null)
			{
				return playerItemPurchaser.TotalPrice > 0f;
			}
			return false;
		}
		return false;
	}

	private static bool IsPlayerOwnedAndMissingRequirements(ItemController itemController)
	{
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return ItemHelper.HasAnyMissingRequirements(itemController.ItemInstance);
		}
		return false;
	}

	private void ShowPurchasedItemRequirements(ItemController itemController)
	{
		List<FurnitureRequirement> missingRequirements = ItemHelper.GetMissingRequirements(itemController.ItemInstance);
		foreach (FurnitureRequirement requirement in itemController.Item.furnitureRequirements)
		{
			if (!requirement.IsRequirementMet(itemController.ItemInstance) || requirement.showIfMet)
			{
				RequirementOverlayEntry requirementOverlayEntry = InitializeEntry(requirement, showToggle: true);
				requirementOverlayEntry.Toggle.gameObject.SetActive(value: true);
				if (!missingRequirements.Exists((FurnitureRequirement x) => x.name == requirement.name))
				{
					requirementOverlayEntry.Toggle.isOn = true;
				}
			}
		}
	}

	private void ShowBrowsingItemRequirement(ItemController itemController)
	{
		requirementHeader.Key = "common_requirements";
		foreach (FurnitureRequirement requirement in itemController.Item.furnitureRequirements)
		{
			if (!hiddenBrowsingRequirements.Exists((FurnitureRequirement hidden) => hidden.name == requirement.name))
			{
				InitializeEntry(requirement, showToggle: false);
			}
		}
	}

	private RequirementOverlayEntry InitializeEntry(FurnitureRequirement requirement, bool showToggle)
	{
		RequirementOverlayEntry requirementOverlayEntry = Object.Instantiate(requirementTemplate, requirementTemplate.transform.parent);
		requirementOverlayEntry.Initialize(requirement, showToggle);
		return requirementOverlayEntry;
	}
}
