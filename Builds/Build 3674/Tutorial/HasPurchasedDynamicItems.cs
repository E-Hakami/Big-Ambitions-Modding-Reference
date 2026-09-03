using System.Collections.Generic;
using BigAmbitions.Items;
using Furniture.Requirements;
using Helpers;

namespace Tutorial;

public class HasPurchasedDynamicItems : QuestRequirement
{
	public CustomBuildingTarget customBuildingTarget;

	protected TutorialDynamicItems dynamicItems = new TutorialDynamicItems();

	protected TutorialDynamicItems dynamicItemsToFulfillItemRequirements = new TutorialDynamicItems();

	protected TutorialDynamicItems dynamicItemsForTutorialPointers = new TutorialDynamicItems();

	public override bool CheckIfCompleted()
	{
		SetDynamicItems();
		if (!CheckIfDynamicItemsFulfilled(dynamicItems))
		{
			return false;
		}
		SetDynamicItemsToFulfillItemRequirements();
		return CheckIfDynamicItemsFulfilled(dynamicItemsToFulfillItemRequirements);
	}

	private bool CheckIfDynamicItemsFulfilled(TutorialDynamicItems dynamicItemsToCheck)
	{
		if (dynamicItemsToCheck.invalid)
		{
			return false;
		}
		if (dynamicItemsToCheck.NoItemsRemaining())
		{
			return true;
		}
		CheckItemsInPlayerHands(dynamicItemsToCheck);
		if (dynamicItemsToCheck.NoItemsRemaining())
		{
			return true;
		}
		CheckItemsInVehicle(dynamicItemsToCheck);
		if (dynamicItemsToCheck.NoItemsRemaining())
		{
			return true;
		}
		CheckItemsInBuilding(dynamicItemsToCheck);
		return dynamicItemsToCheck.NoItemsRemaining();
	}

	protected virtual void CheckItemsInBuilding(TutorialDynamicItems dynamicItemsToCheck)
	{
		foreach (ItemInstance value in BuildingHelper.GetBuildingRegistration(customBuildingTarget.GetAddress()).itemInstances.Values)
		{
			dynamicItemsToCheck.CheckItem(value.itemName);
			CheckDynamicItemsInCargoInstances(value.cargoInstances, dynamicItemsToCheck);
			if (dynamicItemsToCheck.NoItemsRemaining())
			{
				break;
			}
		}
	}

	private void CheckItemsInVehicle(TutorialDynamicItems dynamicItemsToCheck)
	{
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (string.IsNullOrEmpty(vehicleInstance.Address.streetName) || !(vehicleInstance.Address != InstanceBehavior<BuildingManager>.Instance.buildingRegistration?.Address))
			{
				CheckDynamicItemsInCargoInstances(vehicleInstance.cargoInstances, dynamicItemsToCheck);
			}
		}
	}

	private void CheckItemsInPlayerHands(TutorialDynamicItems dynamicItemsToCheck)
	{
		if (PlayerHelper.IsHoldingItem)
		{
			dynamicItemsToCheck.CheckItem(PlayerHelper.ItemInstanceInHands.itemName);
			CheckDynamicItemsInCargoInstances(PlayerHelper.ItemInstanceInHands.cargoInstances, dynamicItemsToCheck);
		}
	}

	protected void CheckDynamicItemsInCargoInstances(List<CargoInstance> cargoInstances, TutorialDynamicItems dynamicItemsToCheck)
	{
		foreach (CargoInstance cargoInstance in cargoInstances)
		{
			if (cargoInstance.paid)
			{
				dynamicItemsToCheck.CheckItem(cargoInstance.itemName, cargoInstance.amount);
			}
		}
	}

	protected virtual void SetDynamicItems()
	{
	}

	protected virtual void SetDynamicItemsForTutorialPointers()
	{
	}

	protected void SetDynamicItemsToFulfillItemRequirements()
	{
		dynamicItemsToFulfillItemRequirements.Reset();
		foreach (string item in dynamicItems.GetDynamicItemsFulfilled())
		{
			foreach (FurnitureRequirement furnitureRequirement in ItemsGetter.GetByName(item).furnitureRequirements)
			{
				if (furnitureRequirement is HasItemTypeAttached hasItemTypeAttached)
				{
					dynamicItemsToFulfillItemRequirements.AddCollection(hasItemTypeAttached.GetAllItemsFittingRequirement(), hasItemTypeAttached.itemType == ItemType.AttachableWorkSurface);
					break;
				}
			}
		}
	}

	public TutorialDynamicItems GetDynamicItems()
	{
		SetDynamicItems();
		return dynamicItems;
	}

	public TutorialDynamicItems GetDynamicItemsToFulfillItemRequirements()
	{
		SetDynamicItemsToFulfillItemRequirements();
		return dynamicItemsToFulfillItemRequirements;
	}

	public TutorialDynamicItems GetDynamicItemsForTutorialPointers()
	{
		SetDynamicItemsForTutorialPointers();
		return dynamicItemsForTutorialPointers;
	}
}
