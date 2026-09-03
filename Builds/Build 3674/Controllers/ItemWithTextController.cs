using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.PlacementSystem;
using Buildings.Indoors.InteriorDesign;
using IngameDebugConsole;
using TMPro;
using UI;
using UnityEngine;

namespace Controllers;

public class ItemWithTextController : ItemController
{
	[Header("Text settings")]
	[SerializeField]
	private TMP_Text[] textLabels;

	public int maxTextLength = 10;

	protected virtual void OnEnable()
	{
		if (initialized)
		{
			UpdateTextOnItem();
		}
		else
		{
			onItemInitialized.AddListener(UpdateTextOnItem);
		}
	}

	public string GetText()
	{
		return base.ItemInstance.worldSpaceTextValue ?? "";
	}

	public void SetText(string text)
	{
		base.ItemInstance.worldSpaceTextValue = text;
		UpdateTextOnItem();
	}

	private void UpdateTextOnItem()
	{
		TMP_Text[] array = textLabels;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetText(base.ItemInstance.worldSpaceTextValue);
		}
	}

	public void OpenChangeTextOverlayInInteriorDesigner()
	{
		InstanceBehavior<UIs>.Instance.interiorDesignerUI.DisableTool(ToolName.Furniture);
		InstanceBehavior<UIs>.Instance.interiorDesignerUI.DisableTool(ToolName.Duplicate);
		InstanceBehavior<UIs>.Instance.interiorDesignerUI.Show();
		InstanceBehavior<UIs>.Instance.interiorDesignerUI.ToggleTool(ToolName.Producer);
		IInteriorDesignerTool.overrideItemIndex = InteriorDesignerController.ItemControllersCache.IndexOf(this);
		InstanceBehavior<UIs>.Instance.interiorDesignerUI.ProcessAction(ToolInputAction.Perform);
		IInteriorDesignerTool.overrideItemIndex = -1;
	}

	[ConsoleMethod("SetTextOnItem", "Set text on item in placement mode", new string[] { })]
	public static void SetTextOnItem(string text)
	{
		IPlaceableItem currentPlaceableItemBeingPlaced = PlacementSystem.CurrentPlaceableItemBeingPlaced;
		if (currentPlaceableItemBeingPlaced == null || !(currentPlaceableItemBeingPlaced is ItemWithTextController itemWithTextController))
		{
			Debug.LogWarning("No item found to set text on");
			return;
		}
		itemWithTextController.ItemInstance.worldSpaceTextValue = text;
		itemWithTextController.UpdateTextOnItem();
		Debug.Log("Set text on item: " + text);
	}
}
