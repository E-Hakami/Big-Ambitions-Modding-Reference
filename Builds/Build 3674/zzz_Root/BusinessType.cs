using System;
using System.Collections.Generic;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.BusinessRequirement;
using Dialogs;
using Entities;
using HGAttributes;
using Helpers.BusinessSimulation;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "BusinessType", menuName = "BigAmbitions/BusinessType", order = 0)]
public class BusinessType : TaggedScriptableObject
{
	public string businessTypeName;

	[AutocompleteDropdown("BuildingTypes")]
	public string suitableBuildingType;

	public BusinessSimulator simulator;

	public bool spawnCustomers;

	public DiplomaName courseRequired;

	[ShowIf("spawnCustomers")]
	public float minutesToCheckBeforeTheSpawnTimeInAiBusinesses = 60f;

	[ShowIf("spawnCustomers")]
	public bool spawnCustomersOnlyOnEnter;

	[ShowIf("spawnCustomers")]
	public bool acceptCustomersWithoutOrderEntries;

	public bool hasEntranceFee;

	[ShowIf("hasEntranceFee")]
	[AutocompleteDropdown("Items")]
	public string defaultEntranceFee;

	[ShowIf("hasEntranceFee")]
	public bool hasWeekendOnlyEntranceFee;

	[ShowIf("hasWeekendOnlyEntranceFee")]
	[AutocompleteDropdown("Items")]
	public string weekendOnlyEntranceFee;

	public CallDialogType callDialogType;

	[Tooltip("Used in POIs and City Map filters")]
	public Sprite icon;

	public Color cityMapFilterColor;

	[Space]
	public BusinessProduct[] businessProducts;

	public string[] productSources;

	public string[] employeePrimarySkills;

	public AppearanceTag[] employeeDefaultAppearanceTags;

	public CustomerType customerType;

	[Range(1f, 5f)]
	public float maxAmountPerProduct = 1f;

	[Space]
	public List<BusinessRequirement> businessRequirements;

	public List<EmployeePreset> uniforms;

	public List<CustomerDemandSet> customerDemandSets;

	public List<string> logoShapes;

	public List<DayFactorMultiplier> dayFactorMultipliers;

	public List<HourlyFactorMultiplier> hourlyFactorMultipliers;

	[Space]
	public string[] aliases;

	[Space]
	[Header("Hangout zone properties")]
	public bool hasPedestriansOutside;

	[ShowIf("hasPedestriansOutside")]
	public BuildingOutsidePedestrianPool pedestrianPool;

	[Space]
	[Header("Radio properties")]
	public bool hasMusicOutside;

	[Range(0f, 1f)]
	public float radioVolume = 1f;

	public RadioStation radioStation;

	[NonSerialized]
	private HashSet<string> _cachedPrimaryProducts;

	[NonSerialized]
	private HashSet<string> _cachedAllProducts;

	[NonSerialized]
	private List<string> _cachedPrimaryRetailProducts;

	public bool CustomersNeedShoppingContainer => HasTag(TagRef.Businesstag.customersneedshoppingcontainer);

	public bool IsHeadquarters => businessTypeName == "ba:businesstype_headquarters";

	public HashSet<string> GetPrimaryProducts()
	{
		if (_cachedPrimaryProducts != null)
		{
			return _cachedPrimaryProducts;
		}
		_cachedPrimaryProducts = new HashSet<string>();
		BusinessProduct[] array = businessProducts;
		foreach (BusinessProduct businessProduct in array)
		{
			if (!(businessProduct.impact < 1f))
			{
				_cachedPrimaryProducts.Add(businessProduct.itemName);
			}
		}
		return _cachedPrimaryProducts;
	}

	public HashSet<string> GetAllProducts()
	{
		if (_cachedAllProducts != null)
		{
			return _cachedAllProducts;
		}
		_cachedAllProducts = new HashSet<string>();
		BusinessProduct[] array = businessProducts;
		foreach (BusinessProduct businessProduct in array)
		{
			_cachedAllProducts.Add(businessProduct.itemName);
		}
		return _cachedAllProducts;
	}

	public List<string> GetPrimaryRetailProducts()
	{
		if (_cachedPrimaryRetailProducts != null)
		{
			return _cachedPrimaryRetailProducts;
		}
		_cachedPrimaryRetailProducts = new List<string>();
		BusinessProduct[] array = businessProducts;
		foreach (BusinessProduct businessProduct in array)
		{
			Item byName = ItemsGetter.GetByName(businessProduct.itemName);
			if (businessProduct.impact >= 1f && byName.type.HasFlag(ItemType.RetailProduct))
			{
				_cachedPrimaryRetailProducts.Add(businessProduct.itemName);
			}
		}
		return _cachedPrimaryRetailProducts;
	}

	public float GetProductImpact(string itemName)
	{
		BusinessProduct[] array = businessProducts;
		foreach (BusinessProduct businessProduct in array)
		{
			if (!(businessProduct.itemName != itemName))
			{
				return businessProduct.impact;
			}
		}
		return 0.025f;
	}

	public bool IsPrimaryProduct(string itemName)
	{
		return GetPrimaryProducts().Contains(itemName);
	}
}
