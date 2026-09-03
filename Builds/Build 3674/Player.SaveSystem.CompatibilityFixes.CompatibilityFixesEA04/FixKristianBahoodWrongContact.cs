using System.Collections.Generic;
using System.Linq;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class FixKristianBahoodWrongContact : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Contact contact = gameInstance.Contacts.FirstOrDefault((Contact x) => x.Address == new Address("ba:street_firstavenue", 2));
		if (contact == null)
		{
			return;
		}
		List<TextMessage> list = contact.messagesQueue.ToList();
		list.RemoveAll((TextMessage x) => x.messageKey == "ba:messagetype_phone_recruitment_agency_campaign_finished");
		contact.messagesQueue = new Queue<TextMessage>();
		foreach (TextMessage item in list)
		{
			contact.messagesQueue.Enqueue(item);
		}
	}
}
