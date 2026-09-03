using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using BigAmbitions.DayNightCycle;
using BigAmbitions.SaveSystem.Legacy.CompatParsers;
using UnityEngine;

namespace UI.Notification;

[Serializable]
public class Notification : IDeserializationCallback
{
	public NotificationType type;

	public string key;

	public Dictionary<string, string> notificationData;

	public Timestamp date;

	[SerializeField]
	[Obsolete("Since EA 0.11")]
	private NotificationData data;

	public void OnDeserialization(object sender)
	{
		if (data != null && (notificationData == null || notificationData.Count == 0))
		{
			notificationData = NotificationDataParser.ParseData(data);
		}
		if (data != null && notificationData != null)
		{
			data = null;
		}
		if (notificationData == null)
		{
			notificationData = new Dictionary<string, string>();
		}
	}
}
