using System;
using System.Collections.Generic;
using BigAmbitions.InputSystem;
using BigAmbitions.PlacementSystem;
using UnityEngine;

namespace Player.HUD.ControlHints;

public class PlacementControlsHintProvider : ConfigurableControlsHintProvider
{
	[SerializeField]
	private List<string> precisePlacementHints = new List<string>();

	protected override void OnEnable()
	{
		base.OnEnable();
		PlacementSystem.onPlacementModeStart = (Action)Delegate.Combine(PlacementSystem.onPlacementModeStart, new Action(OnPlacementModeStart));
		PlacementSystem.onPlacementModeEnd = (Action)Delegate.Combine(PlacementSystem.onPlacementModeEnd, new Action(OnPlacementModeEnd));
		SetActive(PlacementSystem.IsInPlacementMode);
		RefreshPrecisePlacementHints();
	}

	protected override void OnDisable()
	{
		PlacementSystem.onPlacementModeStart = (Action)Delegate.Remove(PlacementSystem.onPlacementModeStart, new Action(OnPlacementModeStart));
		PlacementSystem.onPlacementModeEnd = (Action)Delegate.Remove(PlacementSystem.onPlacementModeEnd, new Action(OnPlacementModeEnd));
		base.OnDisable();
	}

	private void Update()
	{
		if (base.IsActive)
		{
			if (PlayerAction.SnapFreePlacement.Pressed())
			{
				SetHintsEnabled(precisePlacementHints, enabledState: true);
			}
			else if (PlayerAction.SnapFreePlacement.Released())
			{
				SetHintsEnabled(precisePlacementHints, enabledState: false);
			}
		}
	}

	private void OnPlacementModeStart()
	{
		SetActive(active: true);
		RefreshPrecisePlacementHints();
	}

	private void OnPlacementModeEnd()
	{
		SetHintsEnabled(precisePlacementHints, enabledState: false);
		SetActive(active: false);
	}

	private void RefreshPrecisePlacementHints()
	{
		SetHintsEnabled(precisePlacementHints, PlacementSystem.IsInPlacementMode && PlayerAction.SnapFreePlacement.Pressing());
	}
}
