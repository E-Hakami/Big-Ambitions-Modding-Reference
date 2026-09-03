using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using Buildings.BuildingTypes.Shared.Dirtiness;
using UnityEngine;

[TaskCategory("Big Ambitions/SelfServiceCustomer")]
public class SelfServiceCustomerTryGrabItem : TryGrabItemBase
{
	protected override void OnRotationFinished()
	{
		OrderHelper.Validate(sharedCustomer.Value.citizenData, sharedOrderEntry.Value, sharedItemController.Value);
		expressionDataContainer.itemName = sharedOrderEntry.Value.itemName;
		if (!sharedOrderEntry.Value.available)
		{
			Complain(CharacterEmojiName.CustomerCantFindItem);
		}
		else if (!sharedOrderEntry.Value.priceAccceptable)
		{
			Complain(CharacterEmojiName.CustomerTooHighPrice);
		}
		else
		{
			PlayCharacterGrabSound();
			characterRunAnimation.StartRunningAnimation(AnimationType.UsingProducer, 1.5f);
		}
		hasStartedAnimation = true;
	}

	protected override void OnAnimationFinished()
	{
		Customer.GrabItem(sharedItemController.Value, sharedCustomer.Value, sharedOrderEntry.Value);
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, sharedItemController.Value.ItemInstance);
		stopWaitingTime = Time.time + Random.Range(0.2f, 0.8f);
		hasStartedWaiting = true;
	}
}
