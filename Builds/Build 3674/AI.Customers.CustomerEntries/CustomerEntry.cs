using BigAmbitions.DayNightCycle;

namespace AI.Customers.CustomerEntries;

public class CustomerEntry
{
	public Timestamp spawnTime;

	public bool completed;

	public Order order = new Order();

	public void AddPaperBagEntryToOrder(float wholesalePrice)
	{
		order.AddPaperBagEntry(wholesalePrice);
	}
}
