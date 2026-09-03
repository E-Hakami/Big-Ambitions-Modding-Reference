using System;
using System.Linq;
using BigAmbitions.Items;
using UnityEngine;

[Serializable]
public class SeatSpot
{
	public bool requiresPhysicalChair;

	public SeatAttachmentPoint seatAttachmentPoint;

	[HideInInspector]
	public bool occupied;

	public bool IsAvailable
	{
		get
		{
			if (!occupied)
			{
				return CanSit;
			}
			return false;
		}
	}

	public bool CanSit
	{
		get
		{
			if (requiresPhysicalChair)
			{
				return GetAttachedChair != null;
			}
			return true;
		}
	}

	public Transform SeatTransform
	{
		get
		{
			if (!requiresPhysicalChair)
			{
				return seatAttachmentPoint?.transform;
			}
			return GetAttachedChair?.SittingPosition;
		}
	}

	public ItemController GetAttachedChair => seatAttachmentPoint?.GetComponentsInChildren<ItemController>().FirstOrDefault((ItemController x) => (x.Item.type & ItemType.Seat) != 0);
}
