using System.Collections.Generic;
using System.Linq;
using BaTable;
using EnhancedUI.EnhancedScroller;
using Localizor;
using TMPro;
using UnityEngine;

namespace UI.Elements;

public class DropdownOptionScrollerController : BaTable<DropdownOptionCellView, DropdownOptionModel>
{
	[SerializeField]
	private TextMeshProUGUI placeholderText;

	public void LoadDropdownOptions(List<string> options, List<int> filteredOptions, bool localizeOptions, int selectedOptionIndex, bool updateSelected = true)
	{
		data.Clear();
		for (int i = 0; i < options.Count; i++)
		{
			string option = options[i];
			if (filteredOptions == null || filteredOptions.Contains(i))
			{
				AddOption(option, i, localizeOptions, selectedOptionIndex);
			}
		}
		scroller.ReloadData();
		if (updateSelected)
		{
			UpdateSelectedCurrentOption(selectedOptionIndex);
		}
	}

	private void AddOption(string option, int optionId, bool localizeOptions, int selectedOptionIndex)
	{
		string option2 = (localizeOptions ? option.GetLocalization() : option);
		data.Add(new DropdownOptionModel(option2, optionId, optionId == selectedOptionIndex));
	}

	public float GetHighestPreferredOptionsWidth()
	{
		string text = null;
		int num = 0;
		foreach (DropdownOptionModel datum in data)
		{
			int length = datum.option.Length;
			if (length > num)
			{
				num = length;
				text = datum.option;
			}
		}
		placeholderText.text = text;
		return placeholderText.preferredWidth;
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 80f;
	}

	public float GetTotalOptionsHeight()
	{
		return GetCellViewSize(scroller, 0) * (float)data.Count + scroller.spacing * (float)(data.Count - 1);
	}

	public void UpdateSelectedCurrentOption(int selectedOptionIndex)
	{
		if (selectedOptionIndex != -1)
		{
			int num = data.IndexOf(data.FirstOrDefault((DropdownOptionModel x) => x.optionId == selectedOptionIndex));
			if (num != -1)
			{
				DropdownOptionCellView row = scroller.GetCellViewAtDataIndex(num) as DropdownOptionCellView;
				SelectRow(row, GetDataId(data[num]));
			}
		}
	}

	public void ScrollToSelectedOption(int selectedOptionIndex)
	{
		int num = data.IndexOf(data.FirstOrDefault((DropdownOptionModel x) => x.optionId == selectedOptionIndex));
		if (num != -1)
		{
			scroller.JumpToDataIndex(num);
		}
	}

	protected override string GetDataId(DropdownOptionModel dropdownOptionModel)
	{
		return dropdownOptionModel.optionId.ToString();
	}
}
