using System.Collections.Generic;
using BigAmbitions.Rivals;
using Buildings;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan;

public class HamptonsPurchaseBoxUI : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent estimatedValuationLocalizationComponent;

	[SerializeField]
	private TextLocalizationComponent buildingPriceLocalizationComponent;

	public TMP_InputField offerInputField;

	public void Show(BuildingRegistration registration)
	{
		base.gameObject.SetActive(value: true);
		if (registration.IsOnSale())
		{
			ShowOnSaleUI(registration);
		}
		else
		{
			ShowNotForSaleUI(registration);
		}
	}

	private void ShowNotForSaleUI(BuildingRegistration registration)
	{
		float marketValue = registration.BuildingCached.GetMarketValue();
		offerInputField.text = marketValue.ToString("F0");
		estimatedValuationLocalizationComponent.SetData("bizman_estimated_valuation".Localize(new
		{
			valuation = marketValue.ToShortCurrencyFormat()
		}));
		estimatedValuationLocalizationComponent.gameObject.SetActive(value: true);
		buildingPriceLocalizationComponent.gameObject.SetActive(value: false);
	}

	private void ShowOnSaleUI(BuildingRegistration registration)
	{
		float buildingSalePrice = registration.Address.GetBuildingSalePrice();
		offerInputField.text = buildingSalePrice.ToString("F0");
		buildingPriceLocalizationComponent.SetData("bizman_presentation_building_price".Localize(new
		{
			price = buildingSalePrice.ToShortCurrencyFormat()
		}));
		estimatedValuationLocalizationComponent.gameObject.SetActive(value: false);
		buildingPriceLocalizationComponent.gameObject.SetActive(value: true);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}

	private bool ShouldShow(Building building)
	{
		if (!building.IsHamptonsHouse())
		{
			return true;
		}
		if (building.IsHamptonsAIVilla())
		{
			return false;
		}
		IReadOnlyCollection<SpecialRival> specialRivals = RivalsHelper.GetSpecialRivals();
		BuildingRegistration registration = building.GetRegistration();
		foreach (SpecialRival item in specialRivals)
		{
			if (item.rivalData.id == registration.buildingOwnerRivalId)
			{
				return false;
			}
		}
		return true;
	}
}
