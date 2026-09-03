using System;
using BigAmbitions.Items;

namespace UI.Topbar.Accessories;

[Serializable]
public class AccessoriesData
{
	public bool isPanelOpen;

	public CargoInstance handAccessoryCargoInstance;

	public CargoInstance headAccessoryCargoInstance;

	public CargoInstance phoneAccessoryCargoInstance;

	public bool handAccessoryVisible = true;

	public bool headAccessoryVisible = true;
}
