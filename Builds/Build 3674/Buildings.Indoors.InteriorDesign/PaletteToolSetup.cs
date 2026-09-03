using System.Collections.Generic;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.Items;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class PaletteToolSetup : ToolSetup
{
	private ColorOverlay _colorOverlay;

	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Palette;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		_colorOverlay = overlay as ColorOverlay;
		if (_colorOverlay == null)
		{
			Debug.LogError("ColorOverlay is not set up correctly in PaletteToolSetup.");
			return;
		}
		Tool = new PaletteTool
		{
			openColorOverlay = OpenColorOverlay,
			closeColorOverlay = CloseColorOverlay,
			hasColorOptions = HasColorOptions
		};
		ColoringRevertibleAction.undoColors = UndoColors;
		ColoringRevertibleAction.applyColors = ApplyColors;
		_colorOverlay.onChangesConfirmed.AddListener(PaletteTool.OnColorChosen);
	}

	private void OpenColorOverlay(int itemIndex)
	{
		if (itemIndex >= 0)
		{
			_colorOverlay.Open(GetItemControllerAtIndex(itemIndex), itemIndex);
		}
	}

	private void CloseColorOverlay()
	{
		_colorOverlay.Close();
	}

	private bool HasColorOptions(int itemIndex)
	{
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		if (itemControllerAtIndex == null)
		{
			return false;
		}
		return itemControllerAtIndex.Item.customColorChannels != (CustomColorChannel)0;
	}

	private void UndoColors(int itemIndex, List<CustomizableColor> colors)
	{
		if (!(_colorOverlay == null))
		{
			ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
			_colorOverlay.UndoColors(itemControllerAtIndex, colors);
		}
	}

	private void ApplyColors(int itemIndex, List<CustomizableColor> colors)
	{
		if (!(_colorOverlay == null))
		{
			ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
			_colorOverlay.ApplyColors(itemControllerAtIndex, colors);
		}
	}
}
