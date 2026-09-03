using UnityEngine;

namespace Player.HUD.ControlHints;

public class ControlsHintsUIController : MonoBehaviour
{
	[SerializeField]
	private ControlsHintRegistry registry;

	[SerializeField]
	private ControlsHintsUI controlsHintsUI;

	private ControlsHintController _controller;

	private void Awake()
	{
		_controller = new ControlsHintController(registry);
		_controller.Changed += Refresh;
		PlayerPrefs.Changed += OnPlayerPrefChanged;
		Refresh();
	}

	private void OnDestroy()
	{
		_controller.Changed -= Refresh;
		PlayerPrefs.Changed -= OnPlayerPrefChanged;
		_controller.Dispose();
	}

	private void OnPlayerPrefChanged(PlayerPref playerPref)
	{
		if (playerPref == PlayerPref.ControlHints)
		{
			Refresh();
		}
	}

	private void Refresh()
	{
		if (!PlayerPrefSettings.ControlHints)
		{
			controlsHintsUI.gameObject.SetActive(value: false);
		}
		else
		{
			controlsHintsUI.ShowHints(_controller.ActiveProviders);
		}
	}
}
