using System;

namespace Player.HUD.ItemInfoOverlays;

[Flags]
public enum DetailedOverlayType
{
	Radio = 1,
	Employee = 2,
	Fridge = 4,
	Dropdown = 8,
	Vehicle = 0x10,
	StorageShelf = 0x20,
	Machine = 0x40,
	Buttons = 0x80,
	TextInput = 0x100,
	JobBoard = 0x200,
	SellerStand = 0x400,
	MachineInfo = 0x800,
	CustomizableButtons = 0x1000
}
