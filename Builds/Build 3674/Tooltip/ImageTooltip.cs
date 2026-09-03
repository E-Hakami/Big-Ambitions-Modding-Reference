using UnityEngine;

namespace Tooltip;

public class ImageTooltip : TooltipTarget
{
	private Sprite _sprite;

	public void Setup(Sprite sprite)
	{
		_sprite = sprite;
	}

	protected override void ShowTooltip()
	{
		if ((bool)_sprite)
		{
			TooltipSystem.AddImage(_sprite);
		}
	}
}
