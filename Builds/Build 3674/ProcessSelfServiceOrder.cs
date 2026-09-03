using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;
using Helpers;
using UnityEngine;

[TaskCategory("Big Ambitions/Order")]
public class ProcessSelfServiceOrder : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	[RequiredField]
	public bool usePaperBag;

	public override void OnStart()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
			{
				entry.processed = true;
				entry.available = true;
				entry.priceAccceptable = true;
			}
			if (InstanceBehavior<BuildingManager>.Instance.businessType.CustomersNeedShoppingContainer)
			{
				sharedCustomer.Value.tpc.UpdateHandContentVisuals(sharedCustomer.Value.order.entries.Count);
				return;
			}
			ItemController itemController = PrefabHelper.CreatePrefabItem(GetContainerItemName());
			sharedCustomer.Value.tpc.SetHandContent(itemController.transform);
			return;
		}
		foreach (OrderEntry entry2 in sharedCustomer.Value.order.entries)
		{
			entry2.processed = true;
			BuildingManager instance = InstanceBehavior<BuildingManager>.Instance;
			string itemName = entry2.itemName;
			Vector3 navMeshPosition = default(Vector3);
			ItemController itemController2 = instance.FindOptimalItemController(itemName, navMeshPosition);
			if (!(itemController2 == null) && (!ItemsGetter.GetByName(entry2.itemName).requiresWeighing || InstanceBehavior<BuildingManager>.Instance.AreThereItemsByName(ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.isweighingscale))))
			{
				OrderHelper.Validate(sharedCustomer.Value.citizenData, entry2, itemController2, payIfAcceptable: true);
				if (entry2.available && entry2.priceAccceptable)
				{
					entry2.available = itemController2.TryGetRandomAvailableNavMeshTargetPosition(out navMeshPosition);
					GrabItem(itemController2);
					BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, itemController2.ItemInstance);
				}
			}
		}
		if (InstanceBehavior<BuildingManager>.Instance.businessType.CustomersNeedShoppingContainer)
		{
			sharedCustomer.Value.tpc.UpdateHandContentVisuals(sharedCustomer.Value.order.entries.Count((OrderEntry x) => x.available && x.priceAccceptable));
		}
	}

	private void GrabItem(ItemController itemController)
	{
		itemController.ItemInstance.SubtractFromStock();
		if (!InstanceBehavior<BuildingManager>.Instance.businessType.CustomersNeedShoppingContainer)
		{
			ItemController itemController2 = PrefabHelper.CreatePrefabItem(GetContainerItemName());
			sharedCustomer.Value.tpc.SetHandContent(itemController2.transform);
		}
	}

	private string GetContainerItemName()
	{
		if (!usePaperBag)
		{
			return "ba:itemname_closedcardboardbox";
		}
		return ItemsGetter.GetRandomBag();
	}
}
