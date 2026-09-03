using System.Collections.Generic;
using System.Linq;
using Dialogs;
using Localizor;
using TMPro;
using UI.Elements;
using UnityEngine;

namespace UI.Dialog;

public class RouletteBetSettings : MonoBehaviour
{
	[SerializeField]
	private Dropdown colorDropdown;

	[SerializeField]
	private Dropdown numberDropdown;

	public TMP_InputField amountInput;

	[HideInInspector]
	public RouletteDialog.RouletteColor selectedColor;

	[HideInInspector]
	public bool hasColorSelected;

	[HideInInspector]
	public int selectedNumber;

	private List<string> _colors;

	private List<string> _numbers;

	private void Start()
	{
		selectedNumber = -1;
		_colors = new List<string> { "colors_red", "colors_black" };
		colorDropdown.SetPlaceholder("dialog_roulette_bet_color_select");
		colorDropdown.SetOptions(_colors);
		colorDropdown.onOptionSelected.AddListener(SelectColor);
		numberDropdown.SetPlaceholder("dialog_roulette_bet_number_select");
		UpdateNumbersInDropdown();
		numberDropdown.onOptionSelected.AddListener(SelectNumber);
	}

	private void SelectColor(int index)
	{
		hasColorSelected = true;
		selectedColor = ((index != 0) ? RouletteDialog.RouletteColor.Black : RouletteDialog.RouletteColor.Red);
		UpdateNumbersInDropdown();
		selectedNumber = -1;
	}

	private void SelectNumber(int index)
	{
		int num = ((index != 0) ? int.Parse(_numbers[index]) : 0);
		selectedNumber = num;
		if (selectedNumber > 0 && !hasColorSelected)
		{
			int num2 = ((RouletteDialog.slots.First((RouletteDialog.RouletteSlot x) => x.number == selectedNumber).color != RouletteDialog.RouletteColor.Red) ? 1 : 0);
			hasColorSelected = true;
			selectedColor = ((num2 != 0) ? RouletteDialog.RouletteColor.Black : RouletteDialog.RouletteColor.Red);
			UpdateNumbersInDropdown();
			colorDropdown.ResetSelectedOption(num2);
		}
	}

	private void UpdateNumbersInDropdown()
	{
		_numbers = new List<string> { "common_any".GetLocalization() };
		_numbers.AddRange(hasColorSelected ? (from number in RouletteDialog.GetNumbersOnColor(selectedColor)
			select $"{number}") : RouletteDialog.slots.Select((RouletteDialog.RouletteSlot slot) => $"{slot.number}"));
		numberDropdown.SetOptions(_numbers, localize: false);
		if (selectedNumber > 0)
		{
			if (!_numbers.Contains($"{selectedNumber}"))
			{
				numberDropdown.ResetSelectedOption();
			}
			else
			{
				numberDropdown.ResetSelectedOption(_numbers.IndexOf($"{selectedNumber}"));
			}
		}
	}
}
