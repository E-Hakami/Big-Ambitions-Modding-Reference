using System.Collections.Generic;
using BigAmbitions.Items;
using HGAttributes;
using Helpers;
using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldItemToInteract")]
public class TutorialPointerDataWorldItemToInteract : TutorialPointerDataWorldItem
{
	[SerializeField]
	[AutocompleteDropdown("Items")]
	protected string[] itemNameToInteractWith;

	[SerializeField]
	protected string[] itemTagsToInteractWith;

	[SerializeField]
	private bool chooseClosestToPlayer = true;

	[Tooltip("If multiple items match, this will be the index of the item the pointer will point to")]
	[HideIf("chooseClosestToPlayer")]
	[SerializeField]
	private int itemIndex;

	public override void FindEntityController()
	{
		ItemController itemController3;
		if (chooseClosestToPlayer)
		{
			List<ItemController> allItemControllers = InstanceBehavior<BuildingManager>.Instance.allItemControllers;
			ItemController itemController = null;
			float num = float.MaxValue;
			Vector3 position = PlayerHelper.GetPosition();
			int i = 0;
			for (int count = allItemControllers.Count; i < count; i++)
			{
				ItemController itemController2 = allItemControllers[i];
				if (!itemController2.playerItemPurchaserSettings.enabled && Matches(itemController2))
				{
					float sqrMagnitude = (position - itemController2.transform.position).sqrMagnitude;
					if (!(sqrMagnitude >= num))
					{
						num = sqrMagnitude;
						itemController = itemController2;
					}
				}
			}
			itemController3 = itemController;
		}
		else
		{
			List<ItemController> list = new List<ItemController>();
			foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
			{
				if (Matches(allItemController) && !allItemController.playerItemPurchaserSettings.enabled)
				{
					list.Add(allItemController);
				}
			}
			itemController3 = list[(itemIndex <= list.Count) ? itemIndex : 0];
		}
		if (!(itemController3 == null))
		{
			entityControllerTarget = itemController3;
			while (itemController3.parentItemController != null)
			{
				itemController3 = (ItemController)(entityControllerTarget = itemController3.parentItemController);
			}
		}
	}

	private bool Matches(ItemController controller)
	{
		for (int i = 0; i < itemNameToInteractWith.Length; i++)
		{
			if (itemNameToInteractWith[i] == controller.itemName)
			{
				return true;
			}
		}
		if (itemTagsToInteractWith == null || itemTagsToInteractWith.Length == 0)
		{
			return false;
		}
		Item item = controller.Item;
		if (item == null)
		{
			return false;
		}
		for (int j = 0; j < itemTagsToInteractWith.Length; j++)
		{
			if (item.HasTag(itemTagsToInteractWith[j]))
			{
				return true;
			}
		}
		return false;
	}
}
