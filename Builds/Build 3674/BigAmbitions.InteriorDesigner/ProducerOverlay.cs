using System;
using System.Collections.Generic;
using System.Linq;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UI.InteriorDesigner;
using UnityEngine;

namespace BigAmbitions.InteriorDesigner;

public class ProducerOverlay : MonoBehaviour
{
	[SerializeField]
	private UIHoverAboveObject hoverAboveObject;

	[SerializeField]
	private TextLocalizationComponent titleText;

	[SerializeField]
	private List<IProducerOverlay> producerOverlays;

	[HideInInspector]
	public bool isOpen;

	public Action<ItemController> selectInActionPanel;

	private void Awake()
	{
		InteriorDesignerUI.OnUndoRedo.AddListener(OnUndoRedo);
	}

	private void OnUndoRedo()
	{
		if (isOpen)
		{
			Close();
		}
	}

	public Dropdown GetItemDropdown(int itemIndex)
	{
		if (!isOpen || itemIndex != IProducerOverlay.currentItemIndex)
		{
			return null;
		}
		foreach (IProducerOverlay producerOverlay in producerOverlays)
		{
			if (producerOverlay is DropdownProducerOverlay dropdownProducerOverlay)
			{
				return dropdownProducerOverlay.GetItemDropdown(itemIndex);
			}
		}
		return null;
	}

	private bool HasChanges()
	{
		return producerOverlays.Any((IProducerOverlay x) => x.HasChanges());
	}

	public void Open(ItemController itemController, int itemIndex)
	{
		if (itemController == null)
		{
			return;
		}
		if (isOpen && HasChanges())
		{
			Close();
		}
		hoverAboveObject.SetObjectToFollow(itemController.transform);
		titleText.Key = itemController.itemName;
		IProducerOverlay.currentItemController = itemController;
		IProducerOverlay.currentItemIndex = itemIndex;
		foreach (IProducerOverlay producerOverlay in producerOverlays)
		{
			if (producerOverlay.ShouldShow(itemController))
			{
				producerOverlay.OnOpen(itemController);
				foreach (GameObject attachedObject in producerOverlay.attachedObjects)
				{
					attachedObject.gameObject.SetActive(value: true);
				}
				continue;
			}
			producerOverlay.gameObject.SetActive(value: false);
			foreach (GameObject attachedObject2 in producerOverlay.attachedObjects)
			{
				attachedObject2.gameObject.SetActive(value: false);
			}
		}
		selectInActionPanel?.Invoke(itemController);
		base.gameObject.SetActive(value: true);
		isOpen = true;
	}

	public void Close()
	{
		if (!isOpen)
		{
			return;
		}
		foreach (IProducerOverlay producerOverlay in producerOverlays)
		{
			if (producerOverlay.HasChanges())
			{
				producerOverlay.ExecuteRevertibleAction();
			}
		}
		isOpen = false;
		base.gameObject.SetActive(value: false);
	}
}
