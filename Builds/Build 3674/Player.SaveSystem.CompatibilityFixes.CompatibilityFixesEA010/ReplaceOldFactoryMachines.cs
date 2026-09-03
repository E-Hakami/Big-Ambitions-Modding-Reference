using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Factories;
using BigAmbitions.Factories.Recipes;
using BigAmbitions.Factories.Workstations;
using BigAmbitions.Items;
using Helpers;
using Items.SpecialItems;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class ReplaceOldFactoryMachines : ICompatibilityFix
{
	private const string OldBurgerRecipeId = "82frJfvDqUqVIpRQqeTNVw==";

	private const string OldKabobRecipeId = "hlTbu2A7JEio52ijzfpz6A==";

	private const string OldCupcakeRecipeId = "DlSF1C2pp0CQu2DYHBFsQ==";

	private const string OldDonutRecipeId = "xoCv3UUlV0eh8mX8Hgi+9w==";

	private const string OldHotdogRecipeId = "Pu7BqWLBt0m6Pn+OksdTw==";

	private const string OldSaladRecipeId = "CZdxPgnFjU+JEpjEy7rLQA==";

	private const string OldFrenchFriesRecipeId = "fh8Q6hMaU0yGIPJy1yY2kg==";

	private const string OldCroissantRecipeId = "2QraMix6H0m1we1tPrB+g==";

	private const string OldPizzaRecipeId = "QcZHGnrrPEWD762NhTGVA==";

	private const string OldBeerRecipeId = "LxJhdKa1HkajvIN2ylaGQ==";

	private const string OldCupOfCoffeeRecipeId = "ZzOaksxUl0uWLGHHwqkg==";

	private const string OldMargaritaRecipeId = "87cFeR00kEGEkVzJhyQ1A==";

	private const string OldMartiniRecipeId = "S+kkcZL40WSMU1jpwPdgg==";

	private const string OldSodaCanRecipeId = "MzRGnzmpz0esItaJQ91OmQ==";

	private const string OldWhiskyRecipeId = "oCGCjIScdU63ixZb8BmcQw==";

	private const string OldBottleOfWineRecipeId = "4LpcysqO0WiDpdd8Mu9mA==";

	private const string OldCigarRecipeId = "nTBvp68HCku2TrVPaDLWnw==";

	private const string OldCigaretteRecipeId = "LU7Dr2K5EisSRsNvnvwxQ==";

	private const string OldCheapGiftRecipeId = "aeJxQFmOuU+BHhkEtT1QlA==";

	private const string OldExpensiveGiftRecipeId = "Y8u8fJNWoUCk3coxCeJucw==";

	private const string OldClassicCheapMaleClothingRecipeId = "aBa0rxR+zECnmcL3NQATRA==";

	private const string OldClassicCheapFemaleClothingRecipeId = "ibo2K0Y6DUOX0DI5ORGbNQ==";

	private const string OldModernCheapMaleClothingRecipeId = "ZmVLmqB4ekeA7gt6y11I9w==";

	private const string OldModernCheapFemaleClothingRecipeId = "nMYxuKg7SkSvnIKIsUm7mQ==";

	private const string OldClassicExpensiveMaleClothingRecipeId = "ctMY3qMgWUaOg9+FEzZrVA==";

	private const string OldClassicExpensiveFemaleClothingRecipeId = "SkYrQo9z5Ey8KU9w8TduQ==";

	private const string OldModernExpensiveMaleClothingRecipeId = "KwhGz4RerEq1P1NPuQyTtA==";

	private const string OldModernExpensiveFemaleClothingRecipeId = "w7AEWT9lnUKovE9xnfNpxA==";

	private const string OldCheapJewelryRecipeId = "avT2zE4OP02UUK54OKgbYg==";

	private const string OldExpensiveJewelryRecipeId = "lFUmxTw98kGucA6etqF0Q==";

	private const string OldSmartphone1RecipeId = "mY7Ob+54xEmcMhJAUXKsqA==";

	private const string OldSmartphone2RecipeId = "DalE9gxVj02FJitYtzfcLw==";

	private const string OldSmartwatch1RecipeId = "hTMXOy+nRkiSZVJlp6vfg==";

	private const string OldSmartwatch2RecipeId = "jE+Lsz8D6EOYDomzbqvdYA==";

	private const string OldHeadphones01RecipeId = "ukDJovjXmEObV6abwA8dAw==";

	private const string OldEarbuds01RecipeId = "Rkx+oTrxlkOd0+5Hp15xAg==";

	private const string OldIceCreamRecipeId = "nst8ew8IJUiV34sgepNiUg==";

	private readonly HashSet<MachineWithRecipe> _allAvailableMachinesWithRecipeSet = new HashSet<MachineWithRecipe>();

	private readonly List<MachineWithRecipe> _allAvailableMachinesWithRecipeList = new List<MachineWithRecipe>();

	public bool Priority => true;

	public void Apply(GameInstance gameInstance)
	{
		ReplaceOutdatedMachineInstances(gameInstance);
	}

	private void ReplaceOutdatedMachineInstances(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.businessTypeName != "ba:businesstype_factory")
			{
				continue;
			}
			FillMachineCollections(buildingRegistration);
			foreach (MachineWithRecipe allAvailableMachinesWithRecipe in _allAvailableMachinesWithRecipeList)
			{
				(FactoryWorkstation, Recipe) newWorkstationAndRecipeToReplaceOldMachine = GetNewWorkstationAndRecipeToReplaceOldMachine(allAvailableMachinesWithRecipe);
				var (factoryWorkstation, recipe) = newWorkstationAndRecipeToReplaceOldMachine;
				if (!(factoryWorkstation == null) || !(recipe == null))
				{
					FactoryWorkstation item = newWorkstationAndRecipeToReplaceOldMachine.Item1;
					Recipe item2 = newWorkstationAndRecipeToReplaceOldMachine.Item2;
					FactoryWorkstationInstance assemblyMachineInstance = CreateAssemblyMachine(item, allAvailableMachinesWithRecipe, item2, buildingRegistration);
					CreateProductionMachines(assemblyMachineInstance, allAvailableMachinesWithRecipe, item, buildingRegistration);
				}
			}
		}
	}

	private void FillMachineCollections(BuildingRegistration registration)
	{
		_allAvailableMachinesWithRecipeSet.Clear();
		_allAvailableMachinesWithRecipeList.Clear();
		foreach (ItemInstance item2 in registration.itemInstances.Values.ToList())
		{
			if (item2 is MachineWithRecipe item && string.IsNullOrEmpty(item2.parentId))
			{
				_allAvailableMachinesWithRecipeSet.Add(item);
				_allAvailableMachinesWithRecipeList.Add(item);
			}
		}
	}

	private FactoryWorkstationInstance CreateAssemblyMachine(FactoryWorkstation factoryWorkstation, MachineWithRecipe machineWithRecipe, Recipe recipe, BuildingRegistration registration)
	{
		FactoryWorkstationInstance factoryWorkstationInstance = (FactoryWorkstationInstance)ItemHelper.InitializeNewInstance(factoryWorkstation.requiredAssemblyMachine);
		factoryWorkstationInstance.position = machineWithRecipe.position;
		factoryWorkstationInstance.rotation = machineWithRecipe.rotation;
		factoryWorkstationInstance.yRotation = machineWithRecipe.yRotation;
		factoryWorkstationInstance.selectedRecipeId = recipe.id;
		factoryWorkstationInstance.workstationType = factoryWorkstation.workstationType;
		factoryWorkstationInstance.streetName = machineWithRecipe.streetName;
		factoryWorkstationInstance.streetNumber = machineWithRecipe.streetNumber;
		ReplaceOldMachineWithNewInstance(factoryWorkstation.requiredAssemblyMachine, machineWithRecipe, registration, factoryWorkstationInstance);
		return factoryWorkstationInstance;
	}

	private void ReplaceOldMachineWithNewInstance(string newMachineItemName, MachineWithRecipe machineWithRecipe, BuildingRegistration registration, FactoryWorkstationInstance newMachineInstance)
	{
		if (newMachineItemName != machineWithRecipe.itemName)
		{
			foreach (MachineWithRecipe item in _allAvailableMachinesWithRecipeSet)
			{
				if (item.itemName == newMachineItemName)
				{
					var (factoryWorkstation, recipe) = GetNewWorkstationAndRecipeToReplaceOldMachine(item);
					if (factoryWorkstation == null && recipe == null)
					{
						newMachineInstance.id = item.id;
						registration.itemInstances[item.id] = newMachineInstance;
						_allAvailableMachinesWithRecipeSet.Remove(item);
						return;
					}
				}
			}
			registration.AddItemInstanceToBuilding(newMachineInstance);
		}
		else
		{
			newMachineInstance.id = machineWithRecipe.id;
			registration.itemInstances[machineWithRecipe.id] = newMachineInstance;
			_allAvailableMachinesWithRecipeSet.Remove(machineWithRecipe);
		}
	}

	private void CreateProductionMachines(FactoryWorkstationInstance assemblyMachineInstance, MachineWithRecipe machineWithRecipe, FactoryWorkstation factoryWorkstation, BuildingRegistration registration)
	{
		FactoryAssemblyMachineController factoryAssemblyMachineController = (FactoryAssemblyMachineController)PrefabHelper.LoadItemControllerFromPrefab(assemblyMachineInstance.itemName);
		factoryAssemblyMachineController.transform.position = machineWithRecipe.position;
		factoryAssemblyMachineController.transform.rotation = machineWithRecipe.rotation;
		for (int i = 0; i < factoryWorkstation.requiredProductionMachines.Count; i++)
		{
			string text = factoryWorkstation.requiredProductionMachines[i];
			FactoryWorkstationInstance factoryWorkstationInstance = (FactoryWorkstationInstance)ItemHelper.InitializeNewInstance(text);
			ReplaceOldMachineWithNewInstance(text, machineWithRecipe, registration, factoryWorkstationInstance);
			factoryWorkstationInstance.parentId = assemblyMachineInstance.id;
			AddProductionMachineAsChild(assemblyMachineInstance, factoryWorkstationInstance, i);
			AlignAndPositionProductionMachine(machineWithRecipe, factoryAssemblyMachineController, i, factoryWorkstationInstance);
		}
		factoryAssemblyMachineController.transform.position = Vector3.zero;
		factoryAssemblyMachineController.transform.rotation = Quaternion.identity;
	}

	private static void AlignAndPositionProductionMachine(MachineWithRecipe machineWithRecipe, FactoryAssemblyMachineController assemblyMachineController, int index, FactoryWorkstationInstance productionMachineInstance)
	{
		FactoryMachineAttachmentPoint factoryMachineAttachmentPoint = (FactoryMachineAttachmentPoint)assemblyMachineController.GetAttachmentPoints()[index];
		FactoryProductionMachineController obj = (FactoryProductionMachineController)PrefabHelper.LoadItemControllerFromPrefab(productionMachineInstance.itemName);
		Transform alignmentTransform = factoryMachineAttachmentPoint.AlignmentTransform;
		Transform alignmentTransform2 = obj.GetAlignmentTransform();
		Quaternion quaternion = alignmentTransform.rotation * Quaternion.Inverse(alignmentTransform2.localRotation);
		Vector3 vector = alignmentTransform.position - quaternion * alignmentTransform2.localPosition;
		vector.y = machineWithRecipe.position.y;
		productionMachineInstance.position = vector;
		productionMachineInstance.rotation = quaternion;
		productionMachineInstance.yRotation = quaternion.eulerAngles.y;
	}

	private static void AddProductionMachineAsChild(FactoryWorkstationInstance assemblyMachineInstance, FactoryWorkstationInstance productionMachineInstance, int index)
	{
		assemblyMachineInstance.stackedItems.Add(new AttachableChild
		{
			childId = productionMachineInstance.id,
			childItemName = productionMachineInstance.itemName,
			attachmentIndex = index
		});
	}

	private (FactoryWorkstation, Recipe) GetNewWorkstationAndRecipeToReplaceOldMachine(MachineWithRecipe oldMachine)
	{
		string text = oldMachine.selectedRecipeId switch
		{
			"82frJfvDqUqVIpRQqeTNVw==" => "ba:itemname_burger", 
			"hlTbu2A7JEio52ijzfpz6A==" => "ba:itemname_kabob", 
			"DlSF1C2pp0CQu2DYHBFsQ==" => "ba:itemname_cupcake", 
			"xoCv3UUlV0eh8mX8Hgi+9w==" => "ba:itemname_donut", 
			"Pu7BqWLBt0m6Pn+OksdTw==" => "ba:itemname_hotdog", 
			"CZdxPgnFjU+JEpjEy7rLQA==" => "ba:itemname_salad", 
			"fh8Q6hMaU0yGIPJy1yY2kg==" => "ba:itemname_frenchfries", 
			"2QraMix6H0m1we1tPrB+g==" => "ba:itemname_croissant", 
			"QcZHGnrrPEWD762NhTGVA==" => "ba:itemname_pizza", 
			"LxJhdKa1HkajvIN2ylaGQ==" => "ba:itemname_beer", 
			"ZzOaksxUl0uWLGHHwqkg==" => "ba:itemname_cupofcoffee", 
			"87cFeR00kEGEkVzJhyQ1A==" => "ba:itemname_margarita", 
			"S+kkcZL40WSMU1jpwPdgg==" => "ba:itemname_martini", 
			"MzRGnzmpz0esItaJQ91OmQ==" => "ba:itemname_sodacan", 
			"oCGCjIScdU63ixZb8BmcQw==" => "ba:itemname_whisky", 
			"4LpcysqO0WiDpdd8Mu9mA==" => "ba:itemname_bottleofwine", 
			"nTBvp68HCku2TrVPaDLWnw==" => "ba:itemname_cigar", 
			"LU7Dr2K5EisSRsNvnvwxQ==" => "ba:itemname_cigarette", 
			"aeJxQFmOuU+BHhkEtT1QlA==" => "ba:itemname_cheapgift", 
			"aBa0rxR+zECnmcL3NQATRA==" => "ba:itemname_classiccheapmaleclothing", 
			"ibo2K0Y6DUOX0DI5ORGbNQ==" => "ba:itemname_classiccheapfemaleclothing", 
			"ZmVLmqB4ekeA7gt6y11I9w==" => "ba:itemname_moderncheapmaleclothing", 
			"nMYxuKg7SkSvnIKIsUm7mQ==" => "ba:itemname_moderncheapfemaleclothing", 
			"ctMY3qMgWUaOg9+FEzZrVA==" => "ba:itemname_classicexpensivemaleclothing", 
			"SkYrQo9z5Ey8KU9w8TduQ==" => "ba:itemname_classicexpensivefemaleclothing", 
			"KwhGz4RerEq1P1NPuQyTtA==" => "ba:itemname_modernexpensivemaleclothing", 
			"w7AEWT9lnUKovE9xnfNpxA==" => "ba:itemname_modernexpensivefemaleclothing", 
			"Y8u8fJNWoUCk3coxCeJucw==" => "ba:itemname_expensivegift", 
			"avT2zE4OP02UUK54OKgbYg==" => "ba:itemname_cheapjewelry", 
			"lFUmxTw98kGucA6etqF0Q==" => "ba:itemname_expensivejewelry", 
			"mY7Ob+54xEmcMhJAUXKsqA==" => "ba:itemname_smartphone1", 
			"DalE9gxVj02FJitYtzfcLw==" => "ba:itemname_smartphone2", 
			"hTMXOy+nRkiSZVJlp6vfg==" => "ba:itemname_smartwatch1", 
			"jE+Lsz8D6EOYDomzbqvdYA==" => "ba:itemname_smartwatch2", 
			"ukDJovjXmEObV6abwA8dAw==" => "ba:itemname_headphones01", 
			"Rkx+oTrxlkOd0+5Hp15xAg==" => "ba:itemname_earbuds01", 
			"nst8ew8IJUiV34sgepNiUg==" => "ba:itemname_icecream", 
			_ => string.Empty, 
		};
		if (!string.IsNullOrEmpty(text))
		{
			return FactoryWorkstationHelper.GetWorkstationAndRecipeByOutputItem(text);
		}
		return default((FactoryWorkstation, Recipe));
	}
}
