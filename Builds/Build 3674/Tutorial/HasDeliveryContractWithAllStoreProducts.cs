using Entities;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasDeliveryContractWithAllStoreProducts")]
public class HasDeliveryContractWithAllStoreProducts : QuestRequirement
{
	[SerializeField]
	private QuestEntryTarget playerStoreTarget;

	[SerializeField]
	private int minAmountPerProduct;

	public override bool CheckIfCompleted()
	{
		foreach (DeliveryContract deliveryContract in SaveGameManager.Current.DeliveryContracts)
		{
			if (deliveryContract.businessAddress != playerStoreTarget.GetAddress())
			{
				continue;
			}
			if (!deliveryContract.enabled)
			{
				return false;
			}
			foreach (string item in BuildingHelper.GetBuildingRegistration(playerStoreTarget.GetAddress()).GetListOfItemsForSale())
			{
				foreach (DeliveryContractItem item2 in deliveryContract.items)
				{
					if (!(item2.itemName != item))
					{
						if (item2.amount < minAmountPerProduct)
						{
							return false;
						}
						break;
					}
				}
			}
			return true;
		}
		return false;
	}
}
