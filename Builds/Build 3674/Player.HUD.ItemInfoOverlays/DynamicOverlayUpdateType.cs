using System;

namespace Player.HUD.ItemInfoOverlays;

[Flags]
public enum DynamicOverlayUpdateType
{
	StockUpdate = 1,
	EmployeeUpdate = 2,
	CtaUpdate = 8,
	HourChangeUpdate = 0x10
}
