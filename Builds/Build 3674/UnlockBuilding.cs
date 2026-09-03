using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Rewards/Unlock Building")]
public class UnlockBuilding : Reward
{
	[SerializeField]
	private string buildingStreetName;

	[SerializeField]
	private int buildingStreetNumber;

	public Address Address => new Address(buildingStreetName, buildingStreetNumber);

	public override void OnComplete()
	{
	}

	public override LanguageChangeEventDataHolder GetTitle()
	{
		LanguageChangeEventDataHolder result = base.GetTitle();
		string[] array = new string[1];
		string text = (base.name = BuildingHelper.GetBuildingRegistration(Address).BusinessName);
		array[0] = text;
		result.Arguments = array;
		return result;
	}
}
