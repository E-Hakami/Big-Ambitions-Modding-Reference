using Entities;
using UI.Smartphone.Apps.Contacts;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class FixEmployeeContactCategories : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (Contact contact in gameInstance.Contacts)
		{
			if (contact != null && contact.IsEmployeeContact)
			{
				contact.category = ContactCategoryName.Employees;
				if (contact.id == gameInstance.PlayerDefaults.contactsLastName)
				{
					gameInstance.PlayerDefaults.contactsLastCategoryName = ContactCategoryName.Employees;
				}
			}
		}
	}
}
