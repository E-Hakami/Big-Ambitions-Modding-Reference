using System;

namespace Player.HUD.ItemInfoOverlays;

[Flags]
public enum SimpleOverlayType
{
	Stock = 1,
	Employee = 2,
	ClickToAction = 4,
	Price = 8,
	Requirements = 0x10,
	Security = 0x20,
	Info = 0x40,
	MachineRecipe = 0x80,
	CustomerCapacity = 0x100
}
