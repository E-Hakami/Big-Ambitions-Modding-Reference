using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Targets/AddressTarget")]
public class AddressTarget : QuestEntryTarget
{
	public Address address;

	public override Address GetAddress()
	{
		return address;
	}
}
