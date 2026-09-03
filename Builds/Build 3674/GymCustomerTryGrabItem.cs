using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;
using UnityEngine;

[TaskCategory("Big Ambitions/GymCustomer")]
public class GymCustomerTryGrabItem : TryGrabItemBase
{
	protected override void OnRotationFinished()
	{
		bool flag = false;
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			if (entry.itemName != sharedOrderEntry.Value.itemName)
			{
				continue;
			}
			entry.processed = true;
			OrderHelper.Validate(sharedCustomer.Value.citizenData, entry, sharedItemController.Value);
			if (!complained)
			{
				expressionDataContainer.itemName = entry.itemName;
				if (!entry.available)
				{
					Complain(CharacterEmojiName.CustomerCantFindItem);
				}
				else if (!entry.priceAccceptable)
				{
					Complain(CharacterEmojiName.CustomerTooHighPrice);
				}
				else
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			PlayCharacterGrabSound();
			characterRunAnimation.StartRunningAnimation(AnimationType.UsingProducer, 1.5f);
		}
		hasStartedAnimation = true;
	}

	protected override void OnAnimationFinished()
	{
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			if (!(entry.itemName != sharedOrderEntry.Value.itemName) && entry.available && entry.priceAccceptable && Customer.GrabItem(sharedItemController.Value, sharedCustomer.Value, entry))
			{
				entry.paid = true;
			}
		}
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, sharedItemController.Value.ItemInstance);
		stopWaitingTime = Time.time + Random.Range(0.2f, 0.8f);
		hasStartedWaiting = true;
	}
}
