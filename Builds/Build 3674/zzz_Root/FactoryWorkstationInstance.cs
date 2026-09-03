using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Factories.Recipes;
using BigAmbitions.Factories.Workstations;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.Indoors.InteriorDesign;
using Helpers;

[Serializable]
public class FactoryWorkstationInstance : ItemInstance
{
	private const string ReasonClosed = "bizman_factory_inactive_reason_closed";

	private const string ReasonNotEnoughSpace = "bizman_factory_inactive_reason_no_space";

	private const string ReasonInvalidWorkstation = "bizman_factory_inactive_reason_invalid_workstation";

	private const string ReasonNotEnoughIngredients = "bizman_factory_inactive_reason_not_enough_ingredients";

	private const string ReasonNoEmployeeAssigned = "bizman_factory_inactive_reason_no_employee";

	private const string ReasonReachedProductionLimit = "bizman_factory_inactive_reason_production_limit";

	public string selectedRecipeId;

	public int priority;

	public bool produceUpTo;

	public int produceUpToValue;

	public string workstationType;

	[NonSerialized]
	private Recipe _selectedRecipe;

	[NonSerialized]
	private List<string> _invalidReasons = new List<string>(12);

	[NonSerialized]
	private List<ItemInstance> _palletShelves = new List<ItemInstance>();

	public Recipe SelectedRecipe
	{
		get
		{
			if (string.IsNullOrEmpty(selectedRecipeId))
			{
				return null;
			}
			Recipe selectedRecipe = _selectedRecipe;
			if (selectedRecipe != null && selectedRecipe.id == selectedRecipeId)
			{
				return _selectedRecipe;
			}
			_selectedRecipe = Workstation.supportedRecipes.FirstOrDefault((Recipe x) => x.id == selectedRecipeId);
			return _selectedRecipe;
		}
	}

	public FactoryWorkstation Workstation => FactoryWorkstationHelper.GetWorkstation(workstationType);

	public string CreatedItemName => SelectedRecipe.output.item;

	public FactoryWorkstationInstance(string itemName)
		: base(itemName)
	{
		List<FactoryWorkstation> workstationsByAssemblyMachine = FactoryWorkstationHelper.GetWorkstationsByAssemblyMachine(itemName);
		if (workstationsByAssemblyMachine.Count > 0)
		{
			FactoryWorkstation factoryWorkstation = workstationsByAssemblyMachine[0];
			workstationType = factoryWorkstation.workstationType;
			selectedRecipeId = factoryWorkstation.supportedRecipes[0].id;
		}
	}

	public bool IsWorkstationValid()
	{
		FactoryWorkstation workstation = FactoryWorkstationHelper.GetWorkstation(workstationType);
		if (workstation == null || workstation.requiredAssemblyMachine != itemName)
		{
			return false;
		}
		HashSet<string> hashSet = new HashSet<string>();
		int i = 0;
		for (int count = stackedItems.Count; i < count; i++)
		{
			hashSet.Add(stackedItems[i].childItemName);
		}
		List<string> requiredProductionMachines = workstation.requiredProductionMachines;
		int j = 0;
		for (int count2 = requiredProductionMachines.Count; j < count2; j++)
		{
			if (!hashSet.Contains(requiredProductionMachines[j]))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsWorkstationActive(BuildingRegistration registration)
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			return false;
		}
		if (!BusinessHelper.IsBusinessOpen(registration))
		{
			return false;
		}
		if (!IsWorkstationValid())
		{
			return false;
		}
		if (!EmployeeHelper.IsEmployeeStationEmployedAtHour(registration, id, SaveGameManager.Current.Hour))
		{
			return false;
		}
		UpdatePalletShelvesList(registration);
		if (!IsSpaceOnPalletShelvesInternal(_palletShelves))
		{
			return false;
		}
		if (!HasRecipeIngredients(registration, SelectedRecipe, _palletShelves))
		{
			return false;
		}
		if (HasReachedProductionLimit(registration, _palletShelves))
		{
			return false;
		}
		return true;
	}

	public List<string> GetInactiveReasonKeys(BuildingRegistration registration)
	{
		if (_invalidReasons == null)
		{
			_invalidReasons = new List<string>(12);
		}
		_invalidReasons.Clear();
		if (!IsWorkstationValid())
		{
			_invalidReasons.Add("bizman_factory_inactive_reason_invalid_workstation");
		}
		if (!BusinessHelper.IsBusinessOpen(registration))
		{
			_invalidReasons.Add("bizman_factory_inactive_reason_closed");
		}
		if (!EmployeeHelper.IsEmployeeStationEmployedAtHour(registration, id, SaveGameManager.Current.Hour))
		{
			_invalidReasons.Add("bizman_factory_inactive_reason_no_employee");
		}
		UpdatePalletShelvesList(registration);
		if (!IsSpaceOnPalletShelvesInternal(_palletShelves))
		{
			_invalidReasons.Add("bizman_factory_inactive_reason_no_space");
		}
		if (!HasRecipeIngredients(registration, SelectedRecipe, _palletShelves))
		{
			_invalidReasons.Add("bizman_factory_inactive_reason_not_enough_ingredients");
		}
		if (HasReachedProductionLimit(registration, _palletShelves))
		{
			_invalidReasons.Add("bizman_factory_inactive_reason_production_limit");
		}
		return _invalidReasons;
	}

	public bool HasProductionMachine(string machineName)
	{
		foreach (AttachableChild stackedItem in stackedItems)
		{
			if (stackedItem.childItemName == machineName && ItemHelper.GetItemControllerByID(stackedItem.childId).gameObject.activeInHierarchy)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsSpaceOnPalletShelves(List<ItemInstance> allItemInstances)
	{
		UpdatePalletShelvesList(allItemInstances);
		return IsSpaceOnPalletShelvesInternal(_palletShelves);
	}

	private static bool IsSpaceOnPalletShelvesInternal(List<ItemInstance> palletShelfInstances)
	{
		foreach (ItemInstance palletShelfInstance in palletShelfInstances)
		{
			if (palletShelfInstance.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage) && palletShelfInstance.cargoInstances.Count < palletShelfInstance.ItemCached.cargoCapacity)
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasRecipeIngredients(BuildingRegistration registration, Recipe recipe, List<ItemInstance> palletShelfInstances)
	{
		List<RecipeItem> ingredients = recipe.ingredients;
		if (ingredients == null || ingredients.Count == 0)
		{
			return true;
		}
		HashSet<string> hashSet = new HashSet<string>(ingredients.Count);
		int i = 0;
		for (int count = ingredients.Count; i < count; i++)
		{
			hashSet.Add(ingredients[i].item);
		}
		foreach (ItemInstance item in palletShelfInstances ?? registration.itemInstances.Values.ToList())
		{
			if (!item.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage))
			{
				continue;
			}
			List<CargoInstance> list = item.cargoInstances;
			int j = 0;
			for (int count2 = list.Count; j < count2; j++)
			{
				hashSet.Remove(list[j].itemName);
				if (hashSet.Count == 0)
				{
					return true;
				}
			}
		}
		return hashSet.Count == 0;
	}

	public bool HasReachedProductionLimit(BuildingRegistration registration, List<ItemInstance> palletShelfInstances)
	{
		if (!produceUpTo)
		{
			return false;
		}
		string createdItemName = CreatedItemName;
		int num = 0;
		foreach (ItemInstance item in palletShelfInstances ?? registration.itemInstances.Values.ToList())
		{
			if (!item.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage))
			{
				continue;
			}
			List<CargoInstance> list = item.cargoInstances;
			int i = 0;
			for (int count = list.Count; i < count; i++)
			{
				CargoInstance cargoInstance = list[i];
				if (!(cargoInstance.itemName != createdItemName))
				{
					num += cargoInstance.amount;
					if (num >= produceUpToValue)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private void UpdatePalletShelvesList(BuildingRegistration registration)
	{
		UpdatePalletShelvesList(registration.itemInstances.Values.ToList());
	}

	private void UpdatePalletShelvesList(List<ItemInstance> allItemInstances)
	{
		if (_palletShelves == null)
		{
			_palletShelves = new List<ItemInstance>();
		}
		_palletShelves.Clear();
		foreach (ItemInstance allItemInstance in allItemInstances)
		{
			if (allItemInstance.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage))
			{
				_palletShelves.Add(allItemInstance);
			}
		}
	}
}
