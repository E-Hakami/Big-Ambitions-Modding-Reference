using Entities;

namespace AI.Customers.CustomerEntries;

public class CustomerEntriesCalculatorCinemaTheater : CustomerEntriesCalculatorRetail
{
	protected override void AddProductsToCustomersEntriesList()
	{
		base.AddProductsToCustomersEntriesList();
		string itemName = ((businessType.businessTypeName == "ba:businesstype_cinema") ? "ba:itemname_cinematicket" : "ba:itemname_theaterticket");
		foreach (CustomerEntry customersEntry in customersEntryList)
		{
			customersEntry.order.entries.Insert(0, new OrderEntry
			{
				itemName = itemName
			});
		}
	}
}
