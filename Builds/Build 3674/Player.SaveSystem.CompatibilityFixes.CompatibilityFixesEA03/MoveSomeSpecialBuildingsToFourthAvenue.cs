using System.Linq;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class MoveSomeSpecialBuildingsToFourthAvenue : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Address oldCityWorkforceAddress = new Address("ba:street_firstavenue", 2);
		Address address = new Address("ba:street_fourthavenue", 41);
		Address oldScottsSuppliesAddress = new Address("ba:street_firstavenue", 11);
		Address address2 = new Address("ba:street_fourthavenue", 39);
		Contact contact = gameInstance.Contacts.FirstOrDefault((Contact x) => x.Address == oldCityWorkforceAddress);
		if (contact != null)
		{
			contact.streetName = address.streetName;
			contact.streetNumber = address.streetNumber;
		}
		Contact contact2 = gameInstance.Contacts.FirstOrDefault((Contact x) => x.Address == oldScottsSuppliesAddress);
		if (contact2 != null)
		{
			contact2.streetName = address2.streetName;
			contact2.streetNumber = address2.streetNumber;
		}
		Address[] source = new Address[2] { oldCityWorkforceAddress, oldScottsSuppliesAddress };
		Address value = new Address(gameInstance.CurrentStreetName, gameInstance.CurrentStreetNumber);
		if (source.Contains(value))
		{
			SaveGameCompatibilityFixes.forcePlayerExitForCompatibility = true;
		}
	}
}
