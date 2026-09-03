using System.Collections.Generic;
using BigAmbitions.DayNightCycle;

namespace UI.Notification;

public class NotificationListModel
{
	public Dictionary<string, string> Data;

	public Timestamp Date;

	public string Key;

	public NotificationType Type;

	public NotificationListModel(string key, NotificationType type, Dictionary<string, string> data, Timestamp date)
	{
		Key = key;
		Type = type;
		Data = data;
		Date = date;
	}
}
