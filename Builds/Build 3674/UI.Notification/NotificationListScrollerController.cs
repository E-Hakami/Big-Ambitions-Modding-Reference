using System.Collections.Generic;
using BaTable;
using EnhancedUI.EnhancedScroller;

namespace UI.Notification;

public class NotificationListScrollerController : BaTable<NotificationListCellView, NotificationListModel>
{
	public void LoadNotifications(IEnumerable<Notification> notifications)
	{
		data.Clear();
		foreach (Notification notification in notifications)
		{
			data.Add(new NotificationListModel(notification.key, notification.type, notification.notificationData, notification.date));
		}
		scroller.ReloadData();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 150f;
	}
}
