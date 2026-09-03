using BaTable;
using TMPro;
using UnityEngine;

namespace UI.Elements;

public class DropdownOptionCellView : BaTableCellView<DropdownOptionModel>
{
	public TMP_Text optionText;

	public GameObject selectedGo;

	public override void SetData(DropdownOptionModel data)
	{
		optionText.text = data.option;
		selectedGo.SetActive(data.selected);
		base.gameObject.name = $"DropdownOption{data.optionId}";
	}

	public override void VisualizeSelected(bool selected)
	{
		selectedGo.SetActive(selected);
	}
}
