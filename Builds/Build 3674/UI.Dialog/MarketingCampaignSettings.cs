using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialog;

public class MarketingCampaignSettings : MonoBehaviour
{
	[SerializeField]
	private UI.Elements.Dropdown businessDropdown;

	[SerializeField]
	private Transform marketingTypeTemplate;

	[SerializeField]
	private TextMeshProUGUI pricePerDayLabel;

	[HideInInspector]
	public List<MarketingTypeName> marketingTypeNames;

	[HideInInspector]
	public List<MarketingTypeName> marketingTypeNamesSelected;

	[HideInInspector]
	public BuildingRegistration selectedBusiness;

	private static Address QueuedSelectedBusinessAddress;

	private List<BuildingRegistration> _buildingRegistrations;

	public static void QueueSelectedBusiness(BuildingRegistration buildingRegistration)
	{
		QueuedSelectedBusinessAddress = buildingRegistration?.Address;
	}

	private void OnEnable()
	{
		MarketingAgencySettings marketingAgencySettings = BuildingHelper.GetBuilding(DialogController.current.contact.Address).SpecialService.settings as MarketingAgencySettings;
		if (marketingAgencySettings == null)
		{
			throw new Exception("Missing MarketingAgencySettings");
		}
		selectedBusiness = null;
		SetMarketingTypes(marketingAgencySettings.marketingTypesAvailable);
		SelectQueuedBusiness();
		SetPrice();
	}

	private void SetMarketingTypes(List<MarketingTypeName> marketingTypes)
	{
		marketingTypeNames = marketingTypes;
		selectedBusiness = null;
		_buildingRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter);
		marketingTypeNamesSelected = new List<MarketingTypeName>();
		businessDropdown.SetPlaceholder("dialog_select_business");
		businessDropdown.SetOptions(_buildingRegistrations.Select((BuildingRegistration x) => x.GetDisplayName()).ToList(), localize: false);
		businessDropdown.onOptionSelected.AddListener(SelectBusiness);
		SetMarketingTypeNames();
	}

	private void SelectQueuedBusiness()
	{
		if (!(QueuedSelectedBusinessAddress == null))
		{
			int num = _buildingRegistrations.FindIndex((BuildingRegistration x) => x.Address == QueuedSelectedBusinessAddress);
			QueuedSelectedBusinessAddress = null;
			if (num >= 0)
			{
				businessDropdown.SelectOption(num);
			}
		}
	}

	private void SetMarketingTypeNames()
	{
		marketingTypeTemplate.ResetTemplate();
		foreach (MarketingTypeName marketingTypeName in marketingTypeNames)
		{
			MarketingTypeSettings marketingTypeSettings = MarketingTypeSettings.Get(marketingTypeName);
			Transform obj = UnityEngine.Object.Instantiate(marketingTypeTemplate, marketingTypeTemplate.parent);
			obj.GetLanguageChangeEventByName("MarketingTypeName").SetData(GetMarketingPercentageLabel(marketingTypeSettings));
			Toggle component = obj.Find("Enabled").GetComponent<Toggle>();
			component.isOn = marketingTypeNamesSelected.Contains(marketingTypeName);
			component.onValueChanged.AddListener(delegate(bool isEnabled)
			{
				UpdateMarketingTypeEnabled(isEnabled, marketingTypeName);
			});
			obj.gameObject.SetActive(value: true);
		}
	}

	private LanguageChangeEventDataHolder GetMarketingPercentageLabel(MarketingTypeSettings marketingTypeSettings)
	{
		if (selectedBusiness == null)
		{
			return (marketingTypeSettings.marketingTypeName.GetLocalizeKey() + "_info_no_percentage").Localize(new
			{
				pricePerDay = marketingTypeSettings.pricePerDay.ToShortCurrencyFormat()
			});
		}
		int squareMeters = BuildingSizeHelper.GetData(selectedBusiness).squareMeters;
		float marketingReachMultiplier = BuildingTypeHelper.GetData(selectedBusiness).marketingReachMultiplier;
		int marketingReachPercentage = Mathf.RoundToInt(Mathf.Min(100f * (float)marketingTypeSettings.sqmReach * marketingReachMultiplier / (float)squareMeters, 100f));
		return (marketingTypeSettings.marketingTypeName.GetLocalizeKey() + "_info").Localize(new
		{
			marketingReachPercentage = marketingReachPercentage,
			pricePerDay = marketingTypeSettings.pricePerDay.ToShortCurrencyFormat()
		});
	}

	private void UpdateMarketingTypeEnabled(bool isEnabled, MarketingTypeName marketingTypeName)
	{
		if (isEnabled)
		{
			marketingTypeNamesSelected.Add(marketingTypeName);
		}
		else
		{
			marketingTypeNamesSelected.Remove(marketingTypeName);
		}
		SetPrice();
	}

	private static bool PlayerBuildingFilter(BuildingRegistration buildingRegistration)
	{
		return BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.generatesrevenue);
	}

	private void SetPrice()
	{
		if (marketingTypeNamesSelected.Count == 0)
		{
			pricePerDayLabel.text = "-";
			return;
		}
		float val = marketingTypeNamesSelected.Sum((MarketingTypeName x) => MarketingTypeSettings.Get(x).pricePerDay);
		pricePerDayLabel.text = val.ToCurrencyFormat();
	}

	private void SelectBusiness(int businessIndex)
	{
		selectedBusiness = _buildingRegistrations[businessIndex];
		if (selectedBusiness == null)
		{
			marketingTypeNamesSelected.Clear();
		}
		else
		{
			marketingTypeNamesSelected = (from x in selectedBusiness.marketingCampaigns
				where x.agencyAddress == DialogController.current.contact.Address && x.enabled
				select x.marketingTypeName).ToList();
		}
		SetPrice();
		SetMarketingTypeNames();
	}
}
