using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateWholesalersContactAddress : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Address address = new Address("ba:street_fifthavenue", 2);
		Address address2 = new Address("ba:street_firststreet", 18);
		Address address3 = new Address("ba:street_sixthavenue", 6);
		Address address4 = new Address("ba:street_sixthavenue", 4);
		Address address5 = new Address("ba:street_fifthavenue", 57);
		Address address6 = new Address("ba:street_twentyfifthstreet", 2);
		foreach (Contact contact in gameInstance.Contacts)
		{
			if (contact.Address == address)
			{
				contact.streetName = address2.streetName;
				contact.streetNumber = address2.streetNumber;
			}
			else if (contact.Address == address3)
			{
				contact.streetName = address4.streetName;
				contact.streetNumber = address4.streetNumber;
			}
			else if (contact.Address == address5)
			{
				contact.streetName = address6.streetName;
				contact.streetNumber = address6.streetNumber;
			}
		}
	}
}
