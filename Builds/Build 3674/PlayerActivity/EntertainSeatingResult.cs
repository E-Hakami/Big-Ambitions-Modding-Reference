using Controllers;
using UnityEngine;

namespace PlayerActivity;

public readonly struct EntertainSeatingResult(SeatController chair, Transform position)
{
	public readonly SeatController Chair = chair;

	public readonly Transform Position = position;

	public bool HasSeat
	{
		get
		{
			if (Chair != null)
			{
				return Position != null;
			}
			return false;
		}
	}
}
