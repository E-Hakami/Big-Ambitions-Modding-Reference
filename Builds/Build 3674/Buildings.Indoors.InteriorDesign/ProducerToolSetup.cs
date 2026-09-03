using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using Controllers;
using Items.SpecialItems;
using UI.Elements;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class ProducerToolSetup : ToolSetup
{
	private ProducerActionPanelUi _producerActionPanel;

	private ProducerOverlay _producerOverlay;

	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Producer;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		_producerOverlay = overlay as ProducerOverlay;
		_producerActionPanel = actionPanel as ProducerActionPanelUi;
		if (_producerOverlay == null || _producerActionPanel == null)
		{
			Debug.LogError("ProducerOverlay or ProducerActionPanelUi is not set in ProducerToolSetup.");
			return;
		}
		Tool = new ProducerTool
		{
			openProducerOverlay = delegate(int i)
			{
				_producerOverlay.Open(GetItemControllerAtIndex(i), i);
			},
			closeProducerOverlay = _producerOverlay.Close,
			showInTool = ShowInTool,
			isOverlayOpen = () => _producerOverlay != null && _producerOverlay.isOpen
		};
		ProducerDropdownRevertibleAction.setStockOption = SetStockOption;
		ProducerWorldTextRevertibleAction.setWorldSpaceText = SetWorldSpaceText;
		ProducerFactoryMachineRevertibleAction.setFactoryMachineOption = SetFactoryMachineOption;
		_producerOverlay.selectInActionPanel = SelectInActionPanel;
		_producerActionPanel.selectWithItem = SelectWithItem;
	}

	private bool ShowInTool(int itemIndex)
	{
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		if (itemControllerAtIndex.StockTypes.Count <= 1)
		{
			if (!(itemControllerAtIndex is ItemWithTextController) && !(itemControllerAtIndex is SignController))
			{
				return itemControllerAtIndex is FactoryAssemblyMachineController;
			}
			return true;
		}
		return true;
	}

	private void SetStockOption(int itemIndex, string itemName)
	{
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		Dropdown itemDropdown = _producerOverlay.GetItemDropdown(itemIndex);
		int stockIndex = itemControllerAtIndex.StockTypes.IndexOf(itemName);
		itemControllerAtIndex.OnStockOptionSelected(stockIndex, itemDropdown);
		if (InstanceBehavior<GameManager>.Instance == null && itemControllerAtIndex is ShowcaseShelfController showcaseShelfController)
		{
			showcaseShelfController.ShowItemVisuals();
		}
	}

	private void SetWorldSpaceText(int itemIndex, string text)
	{
		if (GetItemControllerAtIndex(itemIndex) is ItemWithTextController itemWithTextController)
		{
			itemWithTextController.SetText(text);
		}
	}

	private void SetFactoryMachineOption(int itemIndex, string selectedRecipeId, string factoryWorkstationTypeId)
	{
		if (GetItemControllerAtIndex(itemIndex) is FactoryAssemblyMachineController factoryAssemblyMachineController)
		{
			factoryAssemblyMachineController.WorkstationInstance.selectedRecipeId = selectedRecipeId;
			factoryAssemblyMachineController.WorkstationInstance.workstationType = factoryWorkstationTypeId;
			GameEvent.Invoke("ba:gameevent_onfactorymachinerecipechanged");
		}
	}

	private void SelectInActionPanel(ItemController itemController)
	{
		if (!(_producerActionPanel == null))
		{
			_producerActionPanel.SelectFocusButton(itemController);
		}
	}

	private void SelectWithItem(ItemController itemController)
	{
		if (!(_producerOverlay == null))
		{
			_producerOverlay.Open(itemController, GetIndexOfItemController(itemController));
		}
	}
}
