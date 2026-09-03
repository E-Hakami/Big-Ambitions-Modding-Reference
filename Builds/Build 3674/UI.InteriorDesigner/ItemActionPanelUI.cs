using System;
using System.Collections.Generic;
using System.Linq;
using Buildings.Indoors.InteriorDesign;
using Extensions;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.InteriorDesigner;

public abstract class ItemActionPanelUI : ActionPanelUI
{
	protected readonly List<ItemController> allItemControllers = new List<ItemController>();

	public Action<ItemController> selectWithItem;

	protected Func<ItemController, Sprite> getOverlayBackground;

	protected Func<ItemController, Sprite> getOverlayIcon;

	private readonly List<IDOtherItemTemplate> _allButtons = new List<IDOtherItemTemplate>();

	private int _selectedButtonHashCode = -1;

	[SerializeField]
	protected Transform itemsContainer;

	[FormerlySerializedAs("idItemTemplateBase")]
	[SerializeField]
	protected IDOtherItemTemplate idItemTemplate;

	[SerializeField]
	protected GameObject noItemsLabel;

	protected virtual bool UseOverlay => false;

	protected virtual void OnEnable()
	{
		InteriorDesignerController.onListsUpdated = (Action)Delegate.Combine(InteriorDesignerController.onListsUpdated, new Action(OnOpen));
	}

	protected virtual void OnDisable()
	{
		InteriorDesignerController.onListsUpdated = (Action)Delegate.Remove(InteriorDesignerController.onListsUpdated, new Action(OnOpen));
	}

	public override void OnOpen()
	{
		_selectedButtonHashCode = -1;
		itemsContainer.ClearChildren();
		_allButtons.Clear();
		foreach (ItemController allItemController in allItemControllers)
		{
			IDOtherItemTemplate iDOtherItemTemplate = UnityEngine.Object.Instantiate(idItemTemplate, itemsContainer);
			iDOtherItemTemplate.SetUp(UseOverlay ? new IDItemUiTemplateData(allItemController, SelectButton, getOverlayBackground?.Invoke(allItemController), getOverlayIcon?.Invoke(allItemController)) : new IDItemUiTemplateData(allItemController, SelectButton));
			_allButtons.Add(iDOtherItemTemplate);
		}
		noItemsLabel.SetActive(_allButtons.Count == 0);
		base.gameObject.SetActive(value: true);
	}

	public override void OnClose()
	{
		base.gameObject.SetActive(value: false);
	}

	public void SelectFocusButton(ItemController itemController)
	{
		int hashCode = itemController.GetHashCode();
		IDOtherItemTemplate iDOtherItemTemplate = _allButtons.FirstOrDefault((IDOtherItemTemplate x) => x.itemHash == hashCode);
		if (!(iDOtherItemTemplate == null) && iDOtherItemTemplate.itemHash != _selectedButtonHashCode)
		{
			SelectButton(iDOtherItemTemplate, itemController);
		}
	}

	private void SelectButton(IDItemTemplateBase selectedButton, ItemController itemController)
	{
		foreach (IDOtherItemTemplate allButton in _allButtons)
		{
			allButton.SetSelected(allButton == selectedButton);
		}
		_selectedButtonHashCode = selectedButton.itemHash;
		selectWithItem?.Invoke(itemController);
	}

	public override void OnEnterInteriorDesignerMode()
	{
	}
}
