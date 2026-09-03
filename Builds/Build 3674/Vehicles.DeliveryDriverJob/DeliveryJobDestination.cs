using System;
using BigAmbitions.Items;

namespace Vehicles.DeliveryDriverJob;

[Serializable]
public class DeliveryJobDestination
{
	public Address address;

	public readonly ItemAmountTarget[] itemAmounts;

	public readonly bool[] itemAmountsDelivered;

	public float playerDistanceCached;

	public DeliveryJobDestination(Address address, ItemAmountTarget[] itemAmounts)
	{
		this.address = address;
		this.itemAmounts = itemAmounts;
		itemAmountsDelivered = new bool[itemAmounts.Length];
	}

	public bool IsCompleted()
	{
		for (int i = 0; i < itemAmounts.Length; i++)
		{
			if (!itemAmountsDelivered[i])
			{
				return false;
			}
		}
		return true;
	}

	public int GetDeliveredCount()
	{
		int num = 0;
		for (int i = 0; i < itemAmounts.Length; i++)
		{
			if (itemAmountsDelivered[i])
			{
				num++;
			}
		}
		return num;
	}
}
