using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.Items;
using TMPro;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class EyedropperToolSetup : ToolSetup
{
	private EyedropperColorOverlay _eyedropperOverlay;

	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Eyedropper;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		_eyedropperOverlay = overlay as EyedropperColorOverlay;
		if (_eyedropperOverlay == null)
		{
			Debug.LogError("EyedropperColorOverlay is not set up correctly.");
			return;
		}
		Tool = new EyedropperTool
		{
			getSelectedColor = GetSelectedColor,
			getSupportedChannels = GetSupportedChannels,
			isHoldingPerform = () => PlayerAction.Click.Pressing(),
			setEyedropperColorOverlay = SetEyedropperColorOverlay,
			onPasteModeChange = OnPasteModeChange,
			resetCursor = delegate
			{
				MouseController.SetCursor(null);
			},
			getOriginalColor = GetOriginalColor
		};
	}

	private List<CustomColor> GetSelectedColor(int itemIndex)
	{
		return GetFurnitureColors(GetItemControllerAtIndex(itemIndex));
	}

	private CustomColorChannel GetSupportedChannels(int itemIndex)
	{
		return GetItemControllerAtIndex(itemIndex).Item.customColorChannels;
	}

	private void SetEyedropperColorOverlay(List<CustomColor> colors)
	{
		if (!(_eyedropperOverlay == null))
		{
			_eyedropperOverlay.SetSelectedColors(colors);
		}
	}

	private void OnPasteModeChange(bool pasteMode)
	{
		ICursorHoverEvent cursor;
		if (!pasteMode)
		{
			ICursorHoverEvent cursorHoverEvent = new EyedropperPickerCursorChangeEvent
			{
				ChangedCursor = true
			};
			cursor = cursorHoverEvent;
		}
		else
		{
			ICursorHoverEvent cursorHoverEvent = new EyedropperPaintCursorChangeEvent
			{
				ChangedCursor = true
			};
			cursor = cursorHoverEvent;
		}
		MouseController.SetCursor(cursor);
	}

	private Color GetOriginalColor(int itemIndex, CustomColorChannel channel)
	{
		Material material = GetItemControllerAtIndex(itemIndex).Renderers[0].material;
		int maskId = ColorOverlay.GetMaskId(channel);
		if (!material.HasProperty(maskId))
		{
			return Color.clear;
		}
		return material.GetColor(maskId);
	}

	private static List<CustomColor> GetFurnitureColors(ItemController itemController)
	{
		List<CustomColor> list = new List<CustomColor>();
		CustomColor furnitureColor = GetFurnitureColor(itemController, CustomColorChannel.Red);
		if (furnitureColor != null)
		{
			list.Add(furnitureColor);
		}
		CustomColor furnitureColor2 = GetFurnitureColor(itemController, CustomColorChannel.Green);
		if (furnitureColor2 != null)
		{
			list.Add(furnitureColor2);
		}
		CustomColor furnitureColor3 = GetFurnitureColor(itemController, CustomColorChannel.Blue);
		if (furnitureColor3 != null)
		{
			list.Add(furnitureColor3);
		}
		CustomColor furnitureColor4 = GetFurnitureColor(itemController, CustomColorChannel.Alpha);
		if (furnitureColor4 != null)
		{
			list.Add(furnitureColor4);
		}
		CustomColor furnitureColor5 = GetFurnitureColor(itemController, CustomColorChannel.Emission);
		if (furnitureColor5 != null)
		{
			list.Add(furnitureColor5);
		}
		CustomColor furnitureColor6 = GetFurnitureColor(itemController, CustomColorChannel.Text);
		if (furnitureColor6 != null)
		{
			list.Add(furnitureColor6);
		}
		CustomColor furnitureColor7 = GetFurnitureColor(itemController, CustomColorChannel.Light);
		if (furnitureColor7 != null)
		{
			list.Add(furnitureColor7);
		}
		return list;
	}

	private static CustomColor GetFurnitureColor(ItemController itemController, CustomColorChannel colorChannel)
	{
		if ((ItemsGetter.GetByName(itemController.itemName).customColorChannels & colorChannel) == 0)
		{
			return null;
		}
		CustomColor customColor = itemController.customColors.FirstOrDefault((CustomColor x) => x.channel == colorChannel);
		if (customColor != null)
		{
			return customColor;
		}
		if (colorChannel == CustomColorChannel.Text)
		{
			TMP_Text[] componentsInChildren = itemController.GetComponentsInChildren<TMP_Text>();
			if (componentsInChildren.Length == 0)
			{
				Debug.LogError(itemController.itemName + " doesn't have text components, but has the Text custom color channel enabled");
				return null;
			}
			return new CustomColor
			{
				channel = colorChannel,
				color = componentsInChildren[0].color
			};
		}
		Material material = itemController.Renderers[0].material;
		int maskId = ColorOverlay.GetMaskId(colorChannel);
		if (!material.HasProperty(maskId))
		{
			return null;
		}
		Color color = material.GetColor(maskId);
		color.a = 1f;
		return new CustomColor
		{
			channel = colorChannel,
			color = color
		};
	}
}
