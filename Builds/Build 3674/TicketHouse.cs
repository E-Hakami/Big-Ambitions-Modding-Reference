using System;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Extensions;
using UI;
using UI.Notification;
using UI.Purchase;

public class TicketHouse : EntityController
{
	[Serializable]
	public struct CasinoOpeningHours
	{
		public DayOfWeekOrdered dayOfWeek;

		public int startHour;

		public int endHour;
	}

	public float ticketPrice = 5000f;

	public List<CasinoOpeningHours> openingHours = new List<CasinoOpeningHours>();

	public bool IsOpen
	{
		get
		{
			DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
			return openingHours.Exists((CasinoOpeningHours x) => x.dayOfWeek == dayOfWeek && SaveGameManager.Current.Hour.InRange(x.startHour, x.endHour - 1));
		}
	}

	public override bool ShouldReactToIoEnter()
	{
		return true;
	}

	public override bool Interact()
	{
		if ((bool)InstanceBehavior<GameManager>.Instance.selectedVehicle)
		{
			Notifications.ShowError("tickethouse_handtruck");
			return false;
		}
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Open(PurchaseUI.Type.CasinoBoat, null, delegate
		{
			if (!IsOpen)
			{
				CasinoOpeningHours nextOpeningHours = GetNextOpeningHours();
				Dictionary<string, string> notificationData = new Dictionary<string, string>
				{
					{
						"dayOfWeek",
						nextOpeningHours.dayOfWeek.ToStringFast()
					},
					{
						"fromTime",
						nextOpeningHours.startHour.GetFormattedTime()
					},
					{
						"toTime",
						nextOpeningHours.endHour.GetFormattedTime()
					}
				};
				Notifications.Show(NotificationType.Error, "tickethouse_not_open", notificationData);
				InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
			}
			else
			{
				InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_casinoboatticket");
				if (InstanceBehavior<CasinoBoatManager>.Instance.boatIsInHarbor && GameManager.ChangeMoneySafe(0f - ticketPrice, transactionInfo, null, null, force: false, showNotification: true))
				{
					InstanceBehavior<CasinoBoatManager>.Instance.StartSailOutSequence();
				}
			}
		});
		return true;
	}

	public override void SecondaryInteract()
	{
	}

	public CasinoOpeningHours GetNextOpeningHours()
	{
		DayOfWeekOrdered currentDayOfWeek = TimeHelper.GetDayOfWeek();
		int currentHour = SaveGameManager.Current.Hour;
		List<CasinoOpeningHours> list = openingHours.FindAll((CasinoOpeningHours x) => x.dayOfWeek == currentDayOfWeek && x.startHour > currentHour);
		if (list.Count > 0)
		{
			return list[0];
		}
		for (int num = 1; num <= 7; num++)
		{
			DayOfWeekOrdered nextDayOfWeek = (DayOfWeekOrdered)((int)(currentDayOfWeek + num) % 7);
			CasinoOpeningHours result = openingHours.Find((CasinoOpeningHours x) => x.dayOfWeek == nextDayOfWeek);
			if (result.startHour != 0 || result.endHour != 0)
			{
				return result;
			}
		}
		return default(CasinoOpeningHours);
	}
}
