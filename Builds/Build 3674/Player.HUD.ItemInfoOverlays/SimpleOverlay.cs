using System;

namespace Player.HUD.ItemInfoOverlays;

public class SimpleOverlay : OverlayBase<SimpleOverlayType>
{
	protected override bool HasFlag(SimpleOverlayType value)
	{
		return (relevantController.simpleOverlayType & value) != 0;
	}

	protected override IOverlay GetOverlayComponent(SimpleOverlayType overlayType)
	{
		if (overlayComponents.TryGetValue(overlayType, out var value))
		{
			return value;
		}
		Type overlayType2 = OverlayHelper.GetOverlayType(overlayType);
		if (overlayType2 == null)
		{
			return null;
		}
		IOverlay overlay = GetComponentInChildren(overlayType2) as IOverlay;
		overlayComponents.Add(overlayType, overlay);
		return overlay;
	}
}
