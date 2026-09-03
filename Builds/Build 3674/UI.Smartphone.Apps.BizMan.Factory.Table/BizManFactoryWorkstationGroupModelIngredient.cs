namespace UI.Smartphone.Apps.BizMan.Factory.Table;

public class BizManFactoryWorkstationGroupModelIngredient
{
	public readonly string itemName;

	public readonly int inStock;

	public readonly int runsOutInDays;

	public BizManFactoryWorkstationGroupModelIngredient(string itemName, int inStock, int runsOutInDays)
	{
		this.itemName = itemName;
		this.inStock = inStock;
		this.runsOutInDays = runsOutInDays;
	}
}
