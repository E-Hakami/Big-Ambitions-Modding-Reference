using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Entities;
using UnityEngine;

namespace Controllers;

public class CoatCheckController : EmployeeStationController
{
	public Transform pickingSpot;

	[SerializeField]
	private MeshRenderer[] coats;

	private int _storedAmountOfCoats;

	public override void Awake()
	{
		base.Awake();
		if (!playerItemPurchaserSettings.enabled)
		{
			employeeType = typeof(CoatCheckEmployee);
		}
	}

	public void OnEnable()
	{
		SubscribeToGlobalEvents();
	}

	public void OnDisable()
	{
		UnsubscribeToGlobalEvents();
	}

	protected override void InitWaitingLine()
	{
		if (playerItemPurchaserSettings.enabled)
		{
			return;
		}
		waitingLine.Init(this, base.ItemInstance?.customPositions ?? customPositions, ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.iscoatcheck), delegate
		{
			if (base.ItemInstance != null)
			{
				base.ItemInstance.customPositions = waitingLine.data.GetMergedAnchorsAndSpotsList();
			}
		});
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		if (base.ItemInstance != null)
		{
			List<SerializableVector3> list = base.ItemInstance.customPositions;
			if (list == null || list.Count <= 0)
			{
				list = customPositions;
				if (list == null || list.Count <= 0)
				{
					waitingLine.creator.Reset();
					return;
				}
			}
			if (waitingLine.data.spots.Count == 0)
			{
				waitingLine.creator.SetUpWaitingLine();
			}
		}
		else
		{
			List<SerializableVector3> list = customPositions;
			if (list != null && list.Count > 1 && waitingLine.data.spots.Count == 0)
			{
				waitingLine.creator.SetUpWaitingLine();
			}
		}
	}

	private void OnExitBuilding(Address _)
	{
		waitingLine.data.customers.Clear();
	}

	private void SubscribeToGlobalEvents()
	{
		GlobalEvents.onTimeMachineStarted = (Action)Delegate.Combine(GlobalEvents.onTimeMachineStarted, new Action(OnTimeMachineStarted));
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(OnNewHour));
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	private void UnsubscribeToGlobalEvents()
	{
		GlobalEvents.onTimeMachineStarted = (Action)Delegate.Remove(GlobalEvents.onTimeMachineStarted, new Action(OnTimeMachineStarted));
		GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(OnNewHour));
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
	}

	private void OnBuildingRegistrationChange(Address address)
	{
		if (!(base.BuildingContext.Building.Address != address) && base.BuildingContext.Registration.temporarilyClosed)
		{
			ResetCoats();
		}
	}

	private void OnNewHour()
	{
		if (base.isActiveAndEnabled && !InstanceBehavior<BuildingManager>.Instance.isOpen)
		{
			ResetCoats();
		}
	}

	private void OnTimeMachineStarted()
	{
		if (base.isActiveAndEnabled)
		{
			ResetCoats();
		}
	}

	public void IncreaseStoredCoats()
	{
		if (_storedAmountOfCoats < coats.Length)
		{
			SetCoatVisibility(isVisible: true, _storedAmountOfCoats);
		}
		_storedAmountOfCoats++;
	}

	public void DecreaseStoredCoats()
	{
		_storedAmountOfCoats--;
		if (_storedAmountOfCoats < coats.Length && _storedAmountOfCoats >= 0)
		{
			SetCoatVisibility(isVisible: false, _storedAmountOfCoats);
		}
	}

	private void SetCoatVisibility(bool isVisible, int coatIndex)
	{
		coats[coatIndex].enabled = isVisible;
	}

	private void ResetCoats()
	{
		_storedAmountOfCoats = 0;
		for (int i = 0; i < coats.Length; i++)
		{
			SetCoatVisibility(isVisible: false, i);
		}
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance employeeInstance)
	{
		base.AssignEmployee(tpc, employeeInstance);
		tpc.GetComponent<CoatCheckEmployee>().SetEmployeeStation(this);
	}
}
