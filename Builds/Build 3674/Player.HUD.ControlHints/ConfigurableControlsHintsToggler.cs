using System.Collections.Generic;
using UnityEngine;

namespace Player.HUD.ControlHints;

public class ConfigurableControlsHintsToggler : MonoBehaviour
{
	[SerializeField]
	private bool toggleOnEnable = true;

	[SerializeField]
	private ConfigurableControlsHintProvider hintsProvider;

	[SerializeField]
	private List<string> hintsToToggle;

	private void OnEnable()
	{
		if (toggleOnEnable)
		{
			ToggleHints(enabledState: true);
		}
	}

	private void OnDisable()
	{
		if (toggleOnEnable)
		{
			ToggleHints(enabledState: false);
		}
	}

	public void ToggleHints(bool enabledState)
	{
		hintsProvider.SetHintsEnabled(hintsToToggle, enabledState);
	}
}
