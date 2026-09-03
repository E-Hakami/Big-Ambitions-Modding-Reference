using System.Collections.Generic;

namespace UI.Smartphone.Apps.BizMan.Factory.Table;

public class BizManFactoryWorkstationGroupModel
{
	public readonly int index;

	public readonly BizManFactoryWorkstationGroupScrollerController scroller;

	public readonly string itemName;

	public readonly int producedPerHour;

	public readonly int inStock;

	public readonly int runsOutInDays;

	public readonly List<BizManFactoryWorkstationGroupModelIngredient> ingredients;

	public BizManFactoryWorkstationGroupModel(int index, BizManFactoryWorkstationGroupScrollerController scroller, string itemName, int producedPerHour, int inStock, int runsOutInDays, List<BizManFactoryWorkstationGroupModelIngredient> ingredients)
	{
		this.index = index;
		this.scroller = scroller;
		this.itemName = itemName;
		this.producedPerHour = producedPerHour;
		this.inStock = inStock;
		this.runsOutInDays = runsOutInDays;
		this.ingredients = ingredients;
	}
}
