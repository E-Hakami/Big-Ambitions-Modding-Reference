using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;
using Vehicles;

namespace UI.Dialog;

public class AutoTowServiceSettings : MonoBehaviour
{
	[SerializeField]
	private Transform optionTemplate;

	public string optionSelected;

	private void Start()
	{
		optionTemplate.ResetTemplate();
		if (IsCurrentVehicleBroken())
		{
			optionSelected = "ba:towdestination_autorepairshop";
		}
		Toggle toggle = null;
		foreach (TowDestinationData towDestination in TowDestinationHelper.TowDestinations)
		{
			Transform obj = Object.Instantiate(optionTemplate, optionTemplate.parent);
			TextLocalizationComponent languageChangeEventByName = obj.GetLanguageChangeEventByName("Value");
			languageChangeEventByName.SetData(LanguageChangeEventDataHolder.Create(towDestination.towType));
			languageChangeEventByName.Suffix = " (" + towDestination.servicePrice.ToShortCurrencyFormat() + ")";
			Toggle component = obj.GetComponent<Toggle>();
			component.onValueChanged.AddListener(delegate(bool isOn)
			{
				if (isOn)
				{
					optionSelected = towDestination.towType;
				}
			});
			if (optionSelected == towDestination.towType)
			{
				toggle = component;
			}
			obj.gameObject.SetActive(value: true);
		}
		toggle?.SetIsOnWithoutNotify(value: true);
	}

	private static bool IsCurrentVehicleBroken()
	{
		VehicleController currentVehicleBase = VehicleHelper.GetCurrentVehicleBase();
		if (currentVehicleBase != null)
		{
			return Mathf.Approximately(currentVehicleBase.GetCurrentCondition(), 1f);
		}
		return false;
	}
}
