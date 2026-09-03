using System.Collections.Generic;
using Entities;
using UI.Smartphone.Apps.Contacts;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class MoveEmployeeMessagesToEmployeeContacts : ICompatibilityFix
{
	private const string EmployeeMessageKeyMarker = "messagetype_employee_contact";

	public void Apply(GameInstance gameInstance)
	{
		int count = gameInstance.Contacts.Count;
		for (int i = 0; i < count; i++)
		{
			Contact contact = gameInstance.Contacts[i];
			if (contact != null && !contact.IsEmployeeContact && contact.messagesQueue != null)
			{
				List<TextMessage> list = ExtractEmployeeMessages(contact);
				if (list.Count != 0)
				{
					DeliverToEmployeeContact(gameInstance, contact.id, list);
				}
			}
		}
	}

	private static List<TextMessage> ExtractEmployeeMessages(Contact contact)
	{
		List<TextMessage> list = new List<TextMessage>();
		Queue<TextMessage> queue = new Queue<TextMessage>(20);
		foreach (TextMessage item in contact.messagesQueue)
		{
			if (item != null && item.messageKey != null && item.messageKey.Contains("messagetype_employee_contact"))
			{
				list.Add(item);
			}
			else
			{
				queue.Enqueue(item);
			}
		}
		if (list.Count == 0)
		{
			return list;
		}
		contact.messagesQueue = queue;
		UpdateLastTimeUpdated(contact);
		return list;
	}

	private static void DeliverToEmployeeContact(GameInstance gameInstance, string id, List<TextMessage> messages)
	{
		Contact contact = FindEmployeeContact(gameInstance, id);
		if (contact == null)
		{
			gameInstance.Contacts.Add(new Contact(id, ContactCategoryName.Employees, "employee_contact_description", null, new Queue<TextMessage>(messages)));
			return;
		}
		if (contact.messagesQueue == null)
		{
			contact.messagesQueue = new Queue<TextMessage>(messages);
		}
		else
		{
			contact.messagesQueue = MergeByTimestamp(contact.messagesQueue, messages);
		}
		UpdateLastTimeUpdated(contact);
	}

	private static Contact FindEmployeeContact(GameInstance gameInstance, string id)
	{
		foreach (Contact contact in gameInstance.Contacts)
		{
			if (contact != null && contact.IsEmployeeContact && contact.id == id)
			{
				return contact;
			}
		}
		return null;
	}

	private static Queue<TextMessage> MergeByTimestamp(Queue<TextMessage> existing, List<TextMessage> moved)
	{
		Queue<TextMessage> queue = new Queue<TextMessage>(20);
		int i = 0;
		foreach (TextMessage item in existing)
		{
			for (; i < moved.Count && IsEarlier(moved[i], item); i++)
			{
				queue.Enqueue(moved[i]);
			}
			queue.Enqueue(item);
		}
		for (; i < moved.Count; i++)
		{
			queue.Enqueue(moved[i]);
		}
		while (queue.Count > 20)
		{
			queue.Dequeue();
		}
		return queue;
	}

	private static bool IsEarlier(TextMessage moved, TextMessage existing)
	{
		if (moved?.timestamp == null || existing?.timestamp == null)
		{
			return false;
		}
		return moved.timestamp.GetTotalMinutes() < existing.timestamp.GetTotalMinutes();
	}

	private static void UpdateLastTimeUpdated(Contact contact)
	{
		TextMessage textMessage = null;
		foreach (TextMessage item in contact.messagesQueue)
		{
			if (item?.timestamp != null)
			{
				textMessage = item;
			}
		}
		if (textMessage != null)
		{
			contact.lastTimeUpdated = textMessage.timestamp;
		}
	}
}
