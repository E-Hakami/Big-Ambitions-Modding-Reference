using System;
using BigAmbitions.InputSystem;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Player.HUD.ControlHints;

public class ControlsHintBinding
{
	private readonly Func<InputAction> _actionResolver;

	public ControlsHintBinding(InputActionReference actionReference)
	{
		if (!actionReference)
		{
			throw new ArgumentNullException("actionReference");
		}
		_actionResolver = () => actionReference.action;
	}

	private InputAction Resolve()
	{
		return _actionResolver();
	}

	public string GetDisplayText(string bindingGroup)
	{
		InputAction inputAction = Resolve();
		int num = inputAction.GetBindingIndex(InputBinding.MaskByGroup(bindingGroup));
		if (num < 0)
		{
			return string.Empty;
		}
		ReadOnlyArray<InputBinding> bindings = inputAction.bindings;
		while (num > 0 && bindings[num].isPartOfComposite)
		{
			num--;
		}
		return InputActionHelper.GetBindingDisplayString(inputAction, num);
	}
}
