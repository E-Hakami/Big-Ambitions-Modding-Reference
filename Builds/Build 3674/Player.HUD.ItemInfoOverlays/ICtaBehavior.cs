using System;

namespace Player.HUD.ItemInfoOverlays;

public abstract class ICtaBehavior
{
	public abstract bool ShouldShow(EntityController entityController);

	public abstract (string, Action) GetCta(EntityController entityController);
}
