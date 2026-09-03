using System.Collections.Generic;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UnityEngine;

namespace Buildings.BuildingTypes.Special.PrivateDriverService;

public class PrivateDriverContractSettings : MonoBehaviour
{
	[SerializeField]
	private Dropdown contractDropdown;

	[SerializeField]
	private TextLocalizationComponent descriptionText;

	[SerializeField]
	private TextLocalizationComponent maxCarsText;

	[SerializeField]
	private TextMeshProUGUI costPerDayText;

	[SerializeField]
	private CanvasGroup canvasGroup;

	private Dictionary<string, PrivateDriverContract> _contracts;

	private void Start()
	{
		_contracts = PrivateDriverHelpers.GetContracts();
		List<string> list = new List<string>(_contracts.Keys);
		contractDropdown.SetOptions(list);
		if (list.Count > 0)
		{
			PrivateDriverContract activeContract = PrivateDriverHelpers.GetActiveContract();
			int b = (activeContract ? list.IndexOf(activeContract.key) : 0);
			contractDropdown.SelectOption(Mathf.Max(0, b));
		}
		contractDropdown.onOptionSelected.AddListener(OnContractSelected);
		UpdateValues();
	}

	public void Disable()
	{
		canvasGroup.interactable = false;
	}

	private void OnContractSelected(int index)
	{
		if (index >= 0 && index < _contracts.Count)
		{
			UpdateValues();
		}
	}

	public PrivateDriverContract GetSelectedContract()
	{
		return _contracts.GetValueOrDefault(contractDropdown.SelectedOption);
	}

	private void UpdateValues()
	{
		PrivateDriverContract selectedContract = GetSelectedContract();
		if ((bool)selectedContract)
		{
			descriptionText.Key = selectedContract.description;
			maxCarsText.Arguments = new
			{
				number = selectedContract.maxCars
			};
			costPerDayText.SetText(selectedContract.costPerDay.ToShortCurrencyFormat());
		}
	}
}
