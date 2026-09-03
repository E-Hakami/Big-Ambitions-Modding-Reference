using System.Collections.Generic;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public class FixContactsDescriptionAndMergeDuplicates : ICompatibilityFix
{
	private const string BusinessTypeKey = "businesstype";

	private const string LocalizationPrefix = "ba:";

	public void Apply(GameInstance gameInstance)
	{
		foreach (Contact contact3 in gameInstance.Contacts)
		{
			UpdateDescription(contact3);
		}
		for (int num = gameInstance.Contacts.Count - 1; num > 0; num--)
		{
			Contact contact = gameInstance.Contacts[num];
			if (contact != null)
			{
				for (int num2 = num - 1; num2 >= 0; num2--)
				{
					Contact contact2 = gameInstance.Contacts[num2];
					if (contact2 != null && !(contact2.id != contact.id) && !(contact2.description != contact.description))
					{
						if (contact2.messagesQueue != null)
						{
							bool flag = contact.messagesQueue != null && contact.messagesQueue.Count > 0;
							Queue<TextMessage> queue = new Queue<TextMessage>(20);
							foreach (TextMessage item in contact2.messagesQueue)
							{
								EnqueueMessage(queue, item);
							}
							if (contact.messagesQueue != null)
							{
								foreach (TextMessage item2 in contact.messagesQueue)
								{
									EnqueueMessage(queue, item2);
								}
							}
							contact.messagesQueue = queue;
							if (!flag)
							{
								contact.lastTimeUpdated = contact2.lastTimeUpdated;
							}
						}
						if (string.IsNullOrEmpty(contact.streetName))
						{
							contact.streetName = contact2.streetName;
							contact.streetNumber = contact2.streetNumber;
						}
						gameInstance.Contacts.RemoveAt(num2);
						num--;
					}
				}
			}
		}
	}

	private static void UpdateDescription(Contact contact)
	{
		if (contact == null || string.IsNullOrEmpty(contact.description))
		{
			return;
		}
		string text = contact.description.ToLower();
		if (!text.Contains("businesstype"))
		{
			return;
		}
		int length = "ba:".Length;
		if (!text.StartsWith("ba:") || text.Substring(length).StartsWith("ba:"))
		{
			while (text.StartsWith("ba:"))
			{
				text = text.Remove(0, length);
			}
			contact.description = "ba:" + text;
		}
	}

	private static void EnqueueMessage(Queue<TextMessage> messages, TextMessage message)
	{
		messages.Enqueue(message);
		while (messages.Count > 20)
		{
			messages.Dequeue();
		}
	}
}
