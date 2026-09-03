using System;
using BigAmbitions.Items;

namespace BigAmbitions.Factories;

[Obsolete("Only for backwards compatibility")]
public class MachineWithRecipe : ItemInstance
{
	public string selectedRecipeId;

	public MachineWithRecipe(string itemName)
		: base(itemName)
	{
	}
}
