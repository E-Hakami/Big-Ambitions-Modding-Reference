using UnityEngine;

public class CasinoBuildingController : CityBuildingController
{
	public TicketHouse ticketHouse;

	public override Transform GetPoiPosition()
	{
		return ticketHouse.transform;
	}
}
