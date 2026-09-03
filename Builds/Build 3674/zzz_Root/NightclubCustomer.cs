using System;
using System.Linq;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.DayNightCycle;
using Buildings;
using Controllers;
using Entities;
using UnityEngine;

public class NightclubCustomer : Customer
{
	private const int MinHoursInNightclub = 1;

	private const int MaxHoursInNightclub = 6;

	public const int MinDancingMinutes = 10;

	public const int MaxDancingMinutes = 30;

	public CoatCheckController coatCheckController;

	public bool hasCoat;

	public bool isHoldingACoat;

	public Timestamp leavingTime;

	public DanceSpot danceSpot;

	public NightclubRandomAction lastAction;

	private static readonly string[] Coats = new string[4] { "Coat1", "Coat2", "Coat3", "Coat4" };

	public override void Init()
	{
		base.Init();
		SetHasCoat();
		SetLeavingTime();
		PayEntranceFee();
		behaviorTree.EnableBehavior();
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(base.OnExitBuilding));
		}
	}

	protected override void SetAppearance()
	{
		tpc.appearanceSetter.SetRandomAppearance(new AppearanceTag[1] { AppearanceTag.Party });
	}

	public override void Leave()
	{
		ForceFinishOrder();
		base.Leave();
	}

	public void SetHasCoat()
	{
		hasCoat = order.entries.Exists((OrderEntry x) => x.itemName == "ba:itemname_coatcheckfee");
	}

	public void SetLeavingTime()
	{
		int currentHour = TimeHelper.CurrentHour;
		OpeningHourSlot openingHourSlot = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.scheduleDays.Find((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek()).openingHourSlots.First((OpeningHourSlot x) => x.startingHour <= currentHour && x.endingHour > currentHour);
		int num = openingHourSlot.endingHour - currentHour;
		if (openingHourSlot.endingHour >= 23)
		{
			OpeningHourSlot openingHourSlot2 = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.scheduleDays.Find((ScheduleDay x) => x.day == TimeHelper.GetNextDayOfWeek()).openingHourSlots.OrderBy((OpeningHourSlot x) => x.startingHour).FirstOrDefault();
			if (openingHourSlot2 != null && openingHourSlot2.startingHour == 0)
			{
				num = Mathf.Min(6, num + openingHourSlot2.GetDurationInHours);
			}
		}
		leavingTime = TimeHelper.Now();
		int num2 = UnityEngine.Random.Range(60, num * 60);
		float num3 = (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness ? NightclubBusinessHelper.cachedAverageDJSkill : 100f) / 100f;
		num2 = Mathf.RoundToInt((float)num2 * num3);
		leavingTime.AddMinutes(num2);
	}

	public override void SetCurrentTimeState()
	{
		if (customerEntry.spawnTime.GetTotalMinutes() + 15f <= TimeHelper.NowInMinutes())
		{
			customerTimeState = CustomerTimeState.AlreadyInAction;
		}
		else if (customerEntry.spawnTime.GetTotalMinutes() + 5f <= TimeHelper.NowInMinutes())
		{
			customerTimeState = CustomerTimeState.RecentlyArrived;
		}
		else
		{
			customerTimeState = CustomerTimeState.JustArrived;
		}
	}

	protected override void ReleaseGameObject()
	{
		InstanceBehavior<BuildingManager>.Instance.customerSpawner.ReleaseCustomer(this, CustomerType.Nightclub);
	}

	public string GetRandomCoat()
	{
		int num = UnityEngine.Random.Range(0, Coats.Length - 1);
		return Coats[num];
	}

	public void PutACoatInTheArm()
	{
		if (!hasCoat)
		{
			tpc.AddHandObject(GetRandomCoat());
			tpc.HoldACoat();
			isHoldingACoat = true;
		}
	}

	public void RemoveCoat()
	{
		tpc.RemoveHandObject();
		tpc.StopHoldingACoat();
		isHoldingACoat = false;
		order.entries.First((OrderEntry x) => x.itemName == "ba:itemname_coatcheckfee").processed = true;
	}

	public void ReleaseDanceSpot()
	{
		if (danceSpot != null)
		{
			danceSpot.Release();
			danceSpot = null;
		}
	}
}
