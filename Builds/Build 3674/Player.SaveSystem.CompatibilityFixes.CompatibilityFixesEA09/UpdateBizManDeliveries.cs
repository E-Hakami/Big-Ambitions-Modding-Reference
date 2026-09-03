using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Entities;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateBizManDeliveries : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		int nextDeliveryDay = DeliveryHelper.GetNextDeliveryDay();
		List<DeliveryContract> deliveryContracts = (from x in gameInstance.DeliveryContracts
			group x by (wholesaleAddress: x.wholesaleAddress, businessAddress: x.businessAddress)).Select(delegate(IGrouping<(Address wholesaleAddress, Address businessAddress), DeliveryContract> g)
		{
			bool allContractsDisabled = g.All((DeliveryContract x) => !x.enabled);
			return new DeliveryContract
			{
				enabled = !allContractsDisabled,
				wholesaleAddress = g.Key.wholesaleAddress,
				businessAddress = g.Key.businessAddress,
				deliveryFee = g.Select((DeliveryContract x) => x.deliveryFee).First(),
				items = (from x in g.SelectMany((DeliveryContract x) => (!(x.enabled | allContractsDisabled)) ? new List<DeliveryContractItem>() : x.items)
					group x by x.itemName).Select(delegate(IGrouping<string, DeliveryContractItem> mergedItem)
				{
					Item byName = ItemsGetter.GetByName(mergedItem.Key);
					return new DeliveryContractItem
					{
						itemName = mergedItem.Key,
						amount = Mathf.Clamp(mergedItem.Sum((DeliveryContractItem x) => x.boxes) * byName.boxSize, 0, byName.maxWholesaleOrderAmount)
					};
				}).ToList(),
				nextDeliveryDay = nextDeliveryDay
			};
		}).ToList();
		gameInstance.DeliveryContracts = deliveryContracts;
	}
}
