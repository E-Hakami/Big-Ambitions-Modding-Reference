using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class DetailedOverlay : OverlayBase<DetailedOverlayType>
{
	public RectTransform rectTransform;

	[SerializeField]
	private List<IOverlay> overlayPriority;

	[SerializeField]
	private EditableWorkstationHeader editableWorkstationHeader;

	private void Start()
	{
		if ((bool)editableWorkstationHeader)
		{
			editableWorkstationHeader.OnHeaderTextChanged += RefreshHeaderName;
		}
	}

	private void OnDestroy()
	{
		if ((bool)editableWorkstationHeader)
		{
			editableWorkstationHeader.OnHeaderTextChanged -= RefreshHeaderName;
		}
	}

	protected override bool HasFlag(DetailedOverlayType value)
	{
		return (relevantController.detailedOverlayType & value) != 0;
	}

	public override int UpdateOverlay(EntityController entityController, string headerText, bool ctaDisabled = false, bool isBlueprintMode = false)
	{
		bool flag = (bool)editableWorkstationHeader && editableWorkstationHeader.TryUpdateHeader(entityController, headerText);
		headerField.gameObject.SetActive(!flag);
		return base.UpdateOverlay(entityController, headerText, ctaDisabled, isBlueprintMode);
	}

	private void RefreshHeaderName(string headerText)
	{
		UpdateOverlay(relevantController, headerText);
	}

	protected override IOverlay GetOverlayComponent(DetailedOverlayType overlayType)
	{
		if (overlayComponents.TryGetValue(overlayType, out var value))
		{
			return value;
		}
		Type overlayType2 = OverlayHelper.GetOverlayType(overlayType);
		if (overlayType2 == null)
		{
			return null;
		}
		IOverlay overlay = GetComponentInChildren(overlayType2) as IOverlay;
		overlayComponents.Add(overlayType, overlay);
		return overlay;
	}

	protected override IOverlay GetHighestPriorityOverlay()
	{
		foreach (IOverlay item in overlayPriority)
		{
			if (item.gameObject.activeSelf)
			{
				return item;
			}
		}
		return null;
	}
}
