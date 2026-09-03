using System.Collections.Generic;
using Entities;
using UI.Smartphone.Apps.Contacts;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class MoveDeliveryReportsToLogisticsAlerts : ICompatibilityFix
{
	private const string DeliveryReportKeyMarker = "phone_logistics_manager_delivery";

	public void Apply(GameInstance gameInstance)
	{
		List<TextMessage> list = new List<TextMessage>();
		foreach (Contact contact in gameInstance.Contacts)
		{
			if (contact != null && contact.messagesQueue != null && !(contact.id == "logistics_alerts"))
			{
				ExtractDeliveryReports(contact, list);
			}
		}
		if (list.Count != 0)
		{
			SortByTimestamp(list);
			while (list.Count > 20)
			{
				list.RemoveAt(0);
			}
			DeliverToAlertContact(gameInstance, list);
		}
	}

	private static void ExtractDeliveryReports(Contact contact, List<TextMessage> deliveryReports)
	{
		bool flag = false;
		Queue<TextMessage> queue = new Queue<TextMessage>(20);
		foreach (TextMessage item in contact.messagesQueue)
		{
			if (item != null && item.messageKey != null && item.messageKey.Contains("phone_logistics_manager_delivery"))
			{
				deliveryReports.Add(item);
				flag = true;
			}
			else
			{
				queue.Enqueue(item);
			}
		}
		if (flag)
		{
			contact.messagesQueue = queue;
			UpdateLastTimeUpdated(contact);
		}
	}

	private static void DeliverToAlertContact(GameInstance gameInstance, List<TextMessage> deliveryReports)
	{
		Contact contact = FindAlertContact(gameInstance);
		if (contact == null)
		{
			gameInstance.Contacts.Add(new Contact("logistics_alerts", ContactCategoryName.ImportsAndGoods, "contact_description_logistics_alerts", null, new Queue<TextMessage>(deliveryReports)));
			return;
		}
		if (contact.messagesQueue == null)
		{
			contact.messagesQueue = new Queue<TextMessage>(deliveryReports);
		}
		else
		{
			contact.messagesQueue = MergeByTimestamp(contact.messagesQueue, deliveryReports);
		}
		UpdateLastTimeUpdated(contact);
	}

	private static Contact FindAlertContact(GameInstance gameInstance)
	{
		foreach (Contact contact in gameInstance.Contacts)
		{
			if (contact != null && contact.id == "logistics_alerts")
			{
				return contact;
			}
		}
		return null;
	}

	private static void SortByTimestamp(List<TextMessage> messages)
	{
		for (int i = 1; i < messages.Count; i++)
		{
			TextMessage textMessage = messages[i];
			int num = i - 1;
			while (num >= 0 && IsEarlier(textMessage, messages[num]))
			{
				messages[num + 1] = messages[num];
				num--;
			}
			messages[num + 1] = textMessage;
		}
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

	private static bool IsEarlier(TextMessage first, TextMessage second)
	{
		if (first?.timestamp == null || second?.timestamp == null)
		{
			return false;
		}
		return first.timestamp.GetTotalMinutes() < second.timestamp.GetTotalMinutes();
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
