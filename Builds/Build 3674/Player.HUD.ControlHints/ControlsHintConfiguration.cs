using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.HUD.ControlHints;

[Serializable]
public class ControlsHintConfiguration
{
	[SerializeField]
	private string textKey;

	[SerializeField]
	private List<InputActionReference> bindings = new List<InputActionReference>();

	[SerializeField]
	private bool startEnabled;

	public ControlsHint ControlsHintDto { get; private set; }

	public string TextKey => textKey;

	public bool IsEnabled { get; private set; }

	public ControlsHint Initialize()
	{
		SetEnabled(startEnabled);
		ControlsHintDto = CreateHint();
		return ControlsHintDto;
	}

	public bool SetEnabled(bool enabled)
	{
		if (IsEnabled == enabled)
		{
			return false;
		}
		IsEnabled = enabled;
		return true;
	}

	private ControlsHint CreateHint()
	{
		ControlsHintBinding[] array = new ControlsHintBinding[bindings.Count];
		for (int i = 0; i < bindings.Count; i++)
		{
			array[i] = new ControlsHintBinding(bindings[i]);
		}
		return new ControlsHint(textKey, array);
	}
}
