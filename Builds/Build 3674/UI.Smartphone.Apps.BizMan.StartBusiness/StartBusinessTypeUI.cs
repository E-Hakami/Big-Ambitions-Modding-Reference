using System;
using System.Collections.Generic;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.StartBusiness;

public class StartBusinessTypeUI : MonoBehaviour
{
	private const string ImporterProductSource = "ba:businessproductsource_importer";

	private const string FactoryProductSource = "ba:businessproductsource_factory";

	[SerializeField]
	private Button mainButton;

	[SerializeField]
	private Button selectButton;

	[SerializeField]
	private Image businessTypeIcon;

	[SerializeField]
	private TextLocalizationComponent localizedBusinessTypeName;

	[Header("Product Inventory Source")]
	[SerializeField]
	private GameObject[] productSourceSection;

	[SerializeField]
	private GameObject productSourcesContainer;

	[SerializeField]
	private StartBusinessInventorySourceEntry productSourceTemplate;

	[Header("Competitors")]
	[SerializeField]
	private TextLocalizationComponent competitorsLabel;

	[SerializeField]
	private TMP_Text competitorsAmountLabel;

	[Header("Primary Products List")]
	[SerializeField]
	private GameObject productsListContainer;

	[SerializeField]
	private TextLocalizationComponent primaryProductsTitle;

	[SerializeField]
	private TMP_Text productsListTemplate;

	[Header("Selected")]
	[SerializeField]
	private GameObject selectedOutline;

	private BusinessType _businessType;

	private bool ShowEmployeesInsteadOfProducts
	{
		get
		{
			if (!_businessType.IsHeadquarters)
			{
				string suitableBuildingType = _businessType.suitableBuildingType;
				return suitableBuildingType == "ba:buildingtype_warehouse" || suitableBuildingType == "ba:buildingtype_office";
			}
			return true;
		}
	}

	private string BusinessTypeName => _businessType.businessTypeName;

	public event Action<string> OnTypeSelected;

	private void Awake()
	{
		productsListTemplate.gameObject.SetActive(value: false);
		productSourceTemplate.gameObject.SetActive(value: false);
	}

	public void Initialize(BusinessType businessType, int competitorsAmount)
	{
		_businessType = businessType;
		base.gameObject.name = BusinessTypeName;
		selectButton.onClick.AddListener(SelectType);
		mainButton.onClick.AddListener(SelectType);
		businessTypeIcon.sprite = businessType.icon;
		localizedBusinessTypeName.Key = BusinessTypeName;
		SetProductSources();
		SetCompetitorsAmount(competitorsAmount);
		SetPrimaryProductsList();
		base.gameObject.SetActive(value: true);
	}

	private void SetProductSources()
	{
		string[] productSources = _businessType.productSources;
		if (ShowEmployeesInsteadOfProducts || productSources.Length == 0)
		{
			GameObject[] array = productSourceSection;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: false);
			}
			return;
		}
		string[] array2 = productSources;
		foreach (string productSource in array2)
		{
			StartBusinessInventorySourceEntry startBusinessInventorySourceEntry = UnityEngine.Object.Instantiate(productSourceTemplate, productSourcesContainer.transform);
			bool hasRequirements = HasRequiredSourceForBusiness(productSource);
			startBusinessInventorySourceEntry.Initialize(productSource, hasRequirements);
		}
		productSourcesContainer.SetActive(value: true);
	}

	private static bool HasRequiredSourceForBusiness(string productSource)
	{
		return productSource switch
		{
			"ba:businessproductsource_wholesaler" => true, 
			"ba:businessproductsource_importer" => HasPlayerBusiness("ba:businesstype_headquarters"), 
			"ba:businessproductsource_factory" => HasPlayerBusiness("ba:businesstype_factory"), 
			_ => true, 
		};
	}

	private static bool HasPlayerBusiness(string businessTypeName)
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && buildingRegistration.businessTypeName == businessTypeName)
			{
				return true;
			}
		}
		return false;
	}

	private void SetCompetitorsAmount(int competitors)
	{
		competitorsAmountLabel.text = (_businessType.IsHeadquarters ? "-" : competitors.ToString());
	}

	private void SetPrimaryProductsList()
	{
		ICollection<string> primaryProducts = GetPrimaryProducts();
		BasicTooltip component = productsListContainer.GetComponent<BasicTooltip>();
		if (ShowEmployeesInsteadOfProducts)
		{
			primaryProductsTitle.Key = "myemployees_title";
		}
		if (primaryProducts.Count == 0)
		{
			return;
		}
		component.enabled = primaryProducts.Count > 3;
		int num = Mathf.Min(primaryProducts.Count, 3);
		int num2 = 0;
		foreach (string item in primaryProducts)
		{
			if (num2 >= num)
			{
				break;
			}
			CreateNewProductEntry(item.GetLocalization());
			num2++;
		}
		if (primaryProducts.Count > 3)
		{
			CreateNewProductEntry("...");
		}
		List<string> values = primaryProducts.MapToList((string product) => product.GetLocalization());
		string value = string.Join("\n", values);
		component.localizationArguments = new { value };
	}

	private void CreateNewProductEntry(string label)
	{
		TMP_Text tMP_Text = UnityEngine.Object.Instantiate(productsListTemplate, productsListContainer.transform);
		tMP_Text.color = Color.white;
		tMP_Text.text = label;
		tMP_Text.gameObject.SetActive(value: true);
	}

	private ICollection<string> GetPrimaryProducts()
	{
		if (!ShowEmployeesInsteadOfProducts)
		{
			return _businessType.GetPrimaryProducts();
		}
		return _businessType.employeePrimarySkills;
	}

	private void SelectType()
	{
		if (HasMissingProductSourceRequirements())
		{
			HudConfirm.Show(null, "bizman_start_business_missing_requirements_popup", ConfirmSelectType, null, "common_confirm", null, allowConfirmationSkip: false);
		}
		else
		{
			ConfirmSelectType();
		}
	}

	private void ConfirmSelectType()
	{
		OnTypeSelected?.Invoke(BusinessTypeName);
	}

	private bool HasMissingProductSourceRequirements()
	{
		if (ShowEmployeesInsteadOfProducts)
		{
			return false;
		}
		string[] productSources = _businessType.productSources;
		for (int i = 0; i < productSources.Length; i++)
		{
			if (!HasRequiredSourceForBusiness(productSources[i]))
			{
				return true;
			}
		}
		return false;
	}

	public void ChangeSelectedState(bool isSelected)
	{
		selectedOutline.SetActive(isSelected);
		selectButton.interactable = !isSelected;
	}
}
