using BigAmbitions.InputSystem;
using Localizor;
using Tooltip;
using UnityEngine;
using UnityEngine.InputSystem;

public class ActionKeyTooltip : TooltipTarget
{
	[SerializeField]
	private string toolNameKey;

	[SerializeField]
	private InputActionReference actionReference;

	protected override void ShowTooltip()
	{
		TooltipSystem.AddHeader("common_value".Localize(new
		{
			value = toolNameKey.GetLocalization() + " [" + InputActionHelper.GetInputKeyLabel(actionReference.action) + "]"
		}));
	}
}
