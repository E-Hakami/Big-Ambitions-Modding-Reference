namespace UI.Dialog;

public class ItemToDeliver
{
	public string itemName;

	public int amount;

	public float price;

	public AmountSelector amountSelector;

	public AmountSelector amountSelectorItemsList;

	public ItemToDeliver(string itemName, int amount, float price, AmountSelector amountSelector)
	{
		this.itemName = itemName;
		this.amount = amount;
		this.price = price;
		this.amountSelector = amountSelector;
	}
}
