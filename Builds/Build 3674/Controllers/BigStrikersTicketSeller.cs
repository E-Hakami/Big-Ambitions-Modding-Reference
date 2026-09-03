using PlayerActivity.Activities.Paid;

namespace Controllers;

public class BigStrikersTicketSeller : TicketSeller
{
	public BigStrikers bigStrikers;

	public override void OnPlayerReached(PlayerController playerController, PaidActivity paidActivity)
	{
		paidActivity.OnPaidActivityStarted();
		bigStrikers.GetPlayerUnit().Use(playerController.Character);
	}

	public override void OnFinish()
	{
		if (bigStrikers.GetPlayerUnit().isOccupied)
		{
			bigStrikers.GetPlayerUnit().Cancel();
		}
	}
}
