using System;
using System.Collections.Generic;
using Helpers;
using Parking.UndergroundParking;
using Streets;
using UnityEngine;

namespace UI.Guiders;

public class GuidersManager : InstanceBehavior<GuidersManager>
{
	private const float MinDistanceSquared = 4f;

	public DirectionGuider destinationGuider;

	public DirectionGuider mainQuestGuider;

	public DirectionGuider sideQuestGuider;

	public DirectionGuider jobDestinationGuider;

	public DirectionGuider privateDriverGuider;

	[SerializeField]
	private Color mainQuestColor;

	[SerializeField]
	private Color sideQuestColor;

	[SerializeField]
	private Color jobDestinationColor;

	[SerializeField]
	private Color privateDriverColor;

	public static readonly List<DirectionGuider> AllGuiders = new List<DirectionGuider>();

	protected override void Awake()
	{
		base.Awake();
		if (base.IsMainInstance)
		{
			AllGuiders.Clear();
			AllGuiders.Add(InstanceBehavior<GuidersManager>.Instance.destinationGuider);
			AllGuiders.Add(InstanceBehavior<GuidersManager>.Instance.mainQuestGuider);
			AllGuiders.Add(InstanceBehavior<GuidersManager>.Instance.sideQuestGuider);
			AllGuiders.Add(InstanceBehavior<GuidersManager>.Instance.jobDestinationGuider);
			AllGuiders.Add(InstanceBehavior<GuidersManager>.Instance.privateDriverGuider);
			GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, (Action<Address>)delegate
			{
				UpdateGuidersVisibility();
			});
			GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate
			{
				UpdateGuidersVisibility();
			});
			GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(UpdateGuidersWithAddress));
		}
	}

	private void Start()
	{
		mainQuestGuider.SetColor(mainQuestColor);
		sideQuestGuider.SetColor(sideQuestColor);
		jobDestinationGuider.SetColor(jobDestinationColor);
		privateDriverGuider.SetColor(privateDriverColor);
		UpdateGuidersVisibility();
	}

	public static void ResetGuider(DirectionGuiderType guiderType)
	{
		if (!(InstanceBehavior<GuidersManager>.Instance == null))
		{
			GetGuider(guiderType).Reset();
		}
	}

	public static void SetGuiderTarget(Address address, DirectionGuiderType guiderType)
	{
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(address);
		if (!(cityBuildingController == null))
		{
			string text = BuildingHelper.GetBuildingRegistration(address).BusinessName;
			if (string.IsNullOrEmpty(text))
			{
				text = address.ToFormattedString();
			}
			DirectionGuider guider = GetGuider(guiderType);
			if (guiderType == DirectionGuiderType.Destination)
			{
				SaveGameManager.Current.customDestination = address;
			}
			guider.SetTarget(cityBuildingController.GetPoiPosition(), text, address, cityBuildingController);
		}
	}

	public static void SetGuiderTarget(Vector3 position, string targetName, Sprite poiIcon, Color backgroundColor, DirectionGuiderType guiderType)
	{
		GetGuider(guiderType).SetTarget(position, targetName, null, poiIcon, backgroundColor);
	}

	public static void UpdateGuidersVisibility()
	{
		if (ShouldGuidersBeHidden())
		{
			SetAllHidden(hidden: true);
			return;
		}
		List<DirectionGuider> list = new List<DirectionGuider>();
		foreach (DirectionGuider allGuider in AllGuiders)
		{
			if (allGuider.target != null)
			{
				list.Add(allGuider);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			for (int j = i + 1; j < list.Count; j++)
			{
				if (Vector3.SqrMagnitude(list[i].target.position - list[j].target.position) < 4f)
				{
					list.RemoveAt(j--);
				}
			}
		}
		HashSet<DirectionGuider> hashSet = new HashSet<DirectionGuider>(list);
		foreach (DirectionGuider allGuider2 in AllGuiders)
		{
			allGuider2.SetHidden(!hashSet.Contains(allGuider2));
		}
	}

	public static void UpdateGuidersWithAddress(Address address)
	{
		foreach (DirectionGuider allGuider in AllGuiders)
		{
			allGuider.UpdateAddressName(address);
			allGuider.UpdatePoiIcon(address);
		}
	}

	public static Address GetGuiderAddress(DirectionGuiderType guiderType)
	{
		return GetGuider(guiderType).CurrentAddress;
	}

	private static bool ShouldGuidersBeHidden()
	{
		if (BuildingPreview.isPreviewing)
		{
			return true;
		}
		if (!CityMap.IsOpen)
		{
			if (!BuildingManager.IsInsideBuilding)
			{
				return UndergroundParkingManager.IsInsideParking;
			}
			return true;
		}
		return false;
	}

	private static void SetAllHidden(bool hidden)
	{
		foreach (DirectionGuider allGuider in AllGuiders)
		{
			allGuider.SetHidden(hidden);
		}
	}

	private static DirectionGuider GetGuider(DirectionGuiderType guiderType)
	{
		return guiderType switch
		{
			DirectionGuiderType.Destination => InstanceBehavior<GuidersManager>.Instance.destinationGuider, 
			DirectionGuiderType.MainQuest => InstanceBehavior<GuidersManager>.Instance.mainQuestGuider, 
			DirectionGuiderType.SideQuest => InstanceBehavior<GuidersManager>.Instance.sideQuestGuider, 
			DirectionGuiderType.JobDestination => InstanceBehavior<GuidersManager>.Instance.jobDestinationGuider, 
			DirectionGuiderType.PrivateDriver => InstanceBehavior<GuidersManager>.Instance.privateDriverGuider, 
			_ => throw new ArgumentOutOfRangeException("guiderType", guiderType, null), 
		};
	}

	public static Color GetGuiderColor(DirectionGuiderType guiderType)
	{
		return guiderType switch
		{
			DirectionGuiderType.Destination => Color.clear, 
			DirectionGuiderType.MainQuest => InstanceBehavior<GuidersManager>.Instance.mainQuestColor, 
			DirectionGuiderType.SideQuest => InstanceBehavior<GuidersManager>.Instance.sideQuestColor, 
			DirectionGuiderType.JobDestination => InstanceBehavior<GuidersManager>.Instance.jobDestinationColor, 
			DirectionGuiderType.PrivateDriver => InstanceBehavior<GuidersManager>.Instance.privateDriverColor, 
			_ => throw new ArgumentOutOfRangeException("guiderType", guiderType, null), 
		};
	}
}
