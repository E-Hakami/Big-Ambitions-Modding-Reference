using System;
using System.Collections.Generic;

namespace Player.HUD.ControlHints;

public class ControlsHint
{
	public string TextKey { get; }

	public IReadOnlyList<ControlsHintBinding> Bindings { get; }

	public ControlsHint(string textKey, params ControlsHintBinding[] bindings)
	{
		if (string.IsNullOrWhiteSpace(textKey))
		{
			throw new ArgumentException("A localization key is required.", "textKey");
		}
		if (bindings == null)
		{
			throw new ArgumentNullException("bindings");
		}
		List<ControlsHintBinding> list = new List<ControlsHintBinding>(bindings.Length);
		foreach (ControlsHintBinding controlsHintBinding in bindings)
		{
			if (controlsHintBinding == null)
			{
				throw new ArgumentException("Bindings cannot contain null.", "bindings");
			}
			list.Add(controlsHintBinding);
		}
		TextKey = textKey;
		Bindings = list.AsReadOnly();
	}
}
