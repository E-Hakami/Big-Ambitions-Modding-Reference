using System.Linq;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class InitLastCategoryName : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (!string.IsNullOrEmpty(gameInstance.PlayerDefaults.contactsLastName))
		{
			Contact contact = gameInstance.Contacts.FirstOrDefault((Contact x) => x.id == gameInstance.PlayerDefaults.contactsLastName);
			if (contact != null)
			{
				gameInstance.PlayerDefaults.contactsLastCategoryName = contact.category;
			}
		}
	}
}
