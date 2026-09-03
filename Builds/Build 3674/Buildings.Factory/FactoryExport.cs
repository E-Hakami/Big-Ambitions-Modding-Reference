using System;

namespace Buildings.Factory;

[Serializable]
public class FactoryExport
{
	public string itemName;

	public int amount;

	public float totalIngredientsCost;

	public float totalPrice;
}
