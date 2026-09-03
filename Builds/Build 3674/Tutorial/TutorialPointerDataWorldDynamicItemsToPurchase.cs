using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldItemsToSetUpBusiness")]
public class TutorialPointerDataWorldDynamicItemsToPurchase : TutorialPointerDataWorldItem
{
	[SerializeField]
	private HasPurchasedDynamicItems questRequirement;

	[SerializeField]
	private bool requiresVehicle;

	[NonSerialized]
	private string _nextItemToBuy;

	[NonSerialized]
	private TutorialDynamicItems _dynamicItems;

	private readonly List<string> _itemsToBuy = new List<string>();

	private readonly Dictionary<string, int> _itemCount = new Dictionary<string, int>();

	public override bool ShouldBeEnabled()
	{
		if (base.ShouldBeEnabled() && (!requiresVehicle || PlayerHelper.IsUsingVehicle))
		{
			return !HasGrabbedItems();
		}
		return false;
	}

	public override void Relocate(TutorialPointer tutorialPointer)
	{
		if (_nextItemToBuy != GetNextItemToBuy())
		{
			OnShow(tutorialPointer);
		}
		base.Relocate(tutorialPointer);
	}

	public override void FindEntityController()
	{
		Vector3 playerPosition = PlayerHelper.GetPosition();
		_nextItemToBuy = GetNextItemToBuy();
		if (string.IsNullOrEmpty(_nextItemToBuy))
		{
			entityControllerTarget = null;
			return;
		}
		ItemController itemController = (from x in InstanceBehavior<BuildingManager>.Instance.allItemControllers
			where x.playerItemPurchaserSettings.enabled && x.GetProducedItemName() == _nextItemToBuy
			orderby Vector3.SqrMagnitude(x.transform.position - playerPosition)
			select x).FirstOrDefault();
		if (!(itemController == null))
		{
			entityControllerTarget = itemController;
			while (itemController.parentItemController != null)
			{
				itemController = (ItemController)(entityControllerTarget = itemController.parentItemController);
			}
		}
	}

	private string GetNextItemToBuy()
	{
		TutorialDynamicItems dynamicItems = GetDynamicItems();
		if (dynamicItems.invalid)
		{
			return null;
		}
		_itemsToBuy.Clear();
		foreach (string[] dynamicItem in dynamicItems.dynamicItems)
		{
			_itemsToBuy.Add(dynamicItem[0]);
		}
		GroupItemsByItemName();
		ICargoHolder currentCargoHolder = PlayerHelper.GetCurrentCargoHolder();
		if (currentCargoHolder == null)
		{
			return _itemsToBuy.FirstOrDefault();
		}
		foreach (CargoInstance cargoInstance in currentCargoHolder.GetCargoInstances())
		{
			if (_itemsToBuy.Contains(cargoInstance.itemName))
			{
				_itemsToBuy.Remove(cargoInstance.itemName);
			}
			foreach (NestedCargoInstance nestedCargoInstance in cargoInstance.nestedCargoInstances)
			{
				if (_itemsToBuy.Contains(nestedCargoInstance.itemName))
				{
					_itemsToBuy.Remove(nestedCargoInstance.itemName);
				}
			}
		}
		return _itemsToBuy.FirstOrDefault();
	}

	private void GroupItemsByItemName()
	{
		_itemCount.Clear();
		foreach (string item in _itemsToBuy)
		{
			if (!_itemCount.TryAdd(item, 1))
			{
				_itemCount[item]++;
			}
		}
		_itemsToBuy.Clear();
		foreach (KeyValuePair<string, int> item2 in _itemCount)
		{
			for (int i = 0; i < item2.Value; i++)
			{
				_itemsToBuy.Add(item2.Key);
			}
		}
	}

	private bool HasGrabbedItems()
	{
		ICargoHolder currentCargoHolder = PlayerHelper.GetCurrentCargoHolder();
		if (currentCargoHolder == null)
		{
			return false;
		}
		TutorialDynamicItems dynamicItems = GetDynamicItems();
		if (dynamicItems.invalid)
		{
			return false;
		}
		dynamicItems.ResetFulfilled();
		foreach (CargoInstance cargoInstance in currentCargoHolder.GetCargoInstances())
		{
			dynamicItems.CheckItem(cargoInstance.itemName);
			foreach (NestedCargoInstance nestedCargoInstance in cargoInstance.nestedCargoInstances)
			{
				dynamicItems.CheckItem(nestedCargoInstance.itemName);
			}
		}
		return dynamicItems.NoItemsRemaining();
	}

	private TutorialDynamicItems GetDynamicItems()
	{
		if (_dynamicItems != null)
		{
			return _dynamicItems;
		}
		_dynamicItems = questRequirement.GetDynamicItemsForTutorialPointers();
		return _dynamicItems;
	}
}
