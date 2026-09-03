using System;
using Helpers;

namespace Controllers;

public class AttractionViewBlockingEntity : ViewBlockingEntity
{
	public Attraction attraction;

	protected override int DefaultLayer => LayerHelper.DefaultLayerIndex;

	public override void Awake()
	{
		base.Awake();
		Attraction obj = attraction;
		obj.onAttractionStart = (Action)Delegate.Combine(obj.onAttractionStart, new Action(OnAttractionStart));
		Attraction obj2 = attraction;
		obj2.onPlayerLeft = (Action)Delegate.Combine(obj2.onPlayerLeft, new Action(OnPlayerLeft));
	}

	private void OnAttractionStart()
	{
		if (attraction.IsPlayerRiding())
		{
			SetCameraBlockMode(isOn: false);
			base.enabled = false;
		}
		else if (isInCameraBlockMode)
		{
			ToggleNpcsVisibility(toggle: false);
		}
	}

	private void OnPlayerLeft()
	{
		base.enabled = true;
	}

	protected override void OnBlockModeChanged(bool isOn)
	{
		base.OnBlockModeChanged(isOn);
		ToggleNpcsVisibility(!isOn);
	}

	private void ToggleNpcsVisibility(bool toggle)
	{
		foreach (CarnivalPedestrian item in attraction.GetNpcsRidingAttraction())
		{
			item.skinnedMeshRenderer.enabled = toggle;
		}
	}
}
