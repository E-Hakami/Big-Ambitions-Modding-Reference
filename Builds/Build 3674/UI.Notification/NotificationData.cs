using System;

namespace UI.Notification;

[Serializable]
[Obsolete("Since EA 0.11")]
public class NotificationData
{
	public string price;

	public string name;

	public string address;

	public string itemname;

	public string skill;

	public string type;

	public string warehouseName;

	public string goal;

	public int amount;

	public int index;

	public int slotNumber;

	public string employeeName;

	public string vehicle;

	public string businessName;

	public string fromname;

	public string toname;

	public string minimumCost;

	public string towAddress;

	public string shelf;

	public string sender;

	public string fromTime;

	public string toTime;

	public string dlc;

	public string dayOfWeek;

	public string healthPlanType;
}
