using System;
using System.Collections.Generic;

namespace Entities;

[Serializable]
public class OrderHistoryEntry
{
	[Serializable]
	public class ItemReport
	{
		public string itemName;

		public int amountSold;

		public float totalPrice;

		public float totalWholesalePrice;

		public ItemSoldPerPriceEntry[] itemSoldBreakdownEntries;

		public ItemReport(string itemName, int amountSold, float totalPrice, float totalWholesalePrice, ItemSoldPerPriceEntry[] itemSoldBreakdownEntries)
		{
			this.itemName = itemName;
			this.amountSold = amountSold;
			this.totalPrice = totalPrice;
			this.totalWholesalePrice = totalWholesalePrice;
			this.itemSoldBreakdownEntries = itemSoldBreakdownEntries;
		}

		public ItemReport()
		{
		}
	}

	[Serializable]
	public class HourReport
	{
		public int hour;

		public int customers;

		public HourReport(int hour, int customers)
		{
			this.hour = hour;
			this.customers = customers;
		}

		public HourReport()
		{
		}
	}

	public int dayNumber;

	public int totalCustomers;

	public List<ItemReport> itemSales;

	public List<HourReport> hourReports;

	public float totalRevenue;
}
