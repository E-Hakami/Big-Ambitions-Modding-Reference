using BaTable;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Notification;

public class NotificationListCellView : BaTableCellView<NotificationListModel>
{
	[SerializeField]
	private TextLocalizationComponent text;

	[SerializeField]
	private TextLocalizationComponent timestamp;

	[SerializeField]
	private Transform icon;

	public override void SetData(NotificationListModel notification)
	{
		backgroundImage.sprite = InstanceBehavior<UIs>.Instance.notificationsListUI.GetBackground(notification.Type);
		text.SetData(notification.Key.Localize(notification.Data));
		timestamp.SetData("timestamp_full".Localize(new
		{
			day = notification.Date.Day,
			time = notification.Date.Hour.GetFormattedTime(notification.Date.Minute)
		}));
		foreach (Transform item in icon.transform)
		{
			item.gameObject.SetActive(item.name == notification.Type.ToString());
		}
	}
}
