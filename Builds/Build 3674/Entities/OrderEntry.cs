using System;

namespace Entities;

[Serializable]
public class OrderEntry
{
	public string itemName;

	public float price;

	public bool available;

	public bool priceAccceptable = true;

	public bool paid;

	public bool processed;

	public float wholesalePrice;

	public void Reset()
	{
		available = false;
		priceAccceptable = true;
		paid = false;
		processed = false;
	}
}
