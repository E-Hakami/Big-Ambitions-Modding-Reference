using System;
using HGAttributes;

namespace Tutorial;

[Serializable]
public class RequiredProduct
{
	[AutocompleteDropdown("Items")]
	public string itemName;

	public int minimumAmount;
}
