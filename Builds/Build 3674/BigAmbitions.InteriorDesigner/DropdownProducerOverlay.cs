using System.Collections.Generic;
using BigAmbitions.InteriorDesigner.Tools;
using Controllers;
using Helpers;
using Items.SpecialItems;
using TMPro;
using UI.Elements;
using UnityEngine;

namespace BigAmbitions.InteriorDesigner;

public class DropdownProducerOverlay : IProducerOverlay
{
	[SerializeField]
	private Dropdown itemDropdown;

	[SerializeField]
	private TMP_Text amountText;

	[SerializeField]
	private GameObject amountPanel;

	private List<string> _currentSelectableItems = new List<string>();

	private string _newItemName;

	private string _originalItemName;

	private void Awake()
	{
		itemDropdown.onOptionSelected.AddListener(OnDropdownSelect);
	}

	public override bool HasChanges()
	{
		return _originalItemName != _newItemName;
	}

	public override bool ShouldShow(ItemController itemController)
	{
		if (itemController.StockTypes.Count <= 1)
		{
			return itemController is SignController;
		}
		return true;
	}

	public override void OnOpen(ItemController itemController)
	{
		int num = Mathf.Clamp(itemController.GetSelectedStockIndex(), 0, itemController.StockTypes.Count);
		_originalItemName = itemController.StockTypes[num];
		_newItemName = _originalItemName;
		_currentSelectableItems = itemController.StockTypes;
		List<string> newOptions = _currentSelectableItems.ConvertAll((string x) => LocalizationHelper.GetItemLabel(x).ToString());
		itemDropdown.SetOptions(newOptions, localize: false, num);
		UpdateAmountText();
		itemDropdown.ResetSearch();
		base.gameObject.SetActive(value: true);
	}

	public override void ExecuteRevertibleAction()
	{
		IInteriorDesignerTool.executeActionThroughCode(new ProducerDropdownRevertibleAction(IProducerOverlay.currentItemIndex, _originalItemName, _newItemName));
	}

	public Dropdown GetItemDropdown(int itemIndex)
	{
		if (itemIndex != IProducerOverlay.currentItemIndex)
		{
			return null;
		}
		return itemDropdown;
	}

	private void OnDropdownSelect(int index)
	{
		IProducerOverlay.currentItemController.OnStockOptionSelected(index, itemDropdown);
		_newItemName = _currentSelectableItems[index];
		if (InstanceBehavior<GameManager>.Instance == null && IProducerOverlay.currentItemController is ShowcaseShelfController showcaseShelfController)
		{
			showcaseShelfController.ShowItemVisuals();
		}
		UpdateAmountText();
	}

	private void UpdateAmountText()
	{
		if (IProducerOverlay.currentItemController is SignController || string.IsNullOrEmpty(IProducerOverlay.currentItemController.CurrentStockDropdownOption))
		{
			amountPanel.SetActive(value: false);
		}
		else if (!(InstanceBehavior<GameManager>.Instance == null))
		{
			string stockAmountText = IProducerOverlay.currentItemController.StockAmountText;
			if (stockAmountText == null)
			{
				amountPanel.SetActive(value: false);
				return;
			}
			amountText.text = stockAmountText;
			amountPanel.SetActive(value: true);
		}
	}
}
