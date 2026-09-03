using System.Linq;
using Entities;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasDeliveryContract")]
public class HasDeliveryContract : QuestRequirement
{
	[SerializeField]
	private CustomBuildingTarget playerStoreTarget;

	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.DeliveryContracts.Any((DeliveryContract x) => x.businessAddress == playerStoreTarget.GetAddress());
	}
}
