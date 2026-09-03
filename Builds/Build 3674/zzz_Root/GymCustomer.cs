using System;
using System.Collections.Generic;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Extensions;
using UnityEngine;

public class GymCustomer : Customer
{
	public bool arrivedWithSportClothes;

	public CharacterData characterDataAtStart;

	private readonly List<EmployeeStationController> _availableFitnessPlanningBoards = new List<EmployeeStationController>();

	protected override string GetContainerItemName()
	{
		return ItemsGetter.GetRandomBag();
	}

	public override void Init()
	{
		SetAvailableFitnessPlanningBoards();
		CheckGymTrainerDemand();
		arrivedWithSportClothes = UnityEngine.Random.value < 0.5f;
		PayEntranceFee();
		base.Init();
		SetCustomerService();
		behaviorTree.EnableBehavior();
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(base.OnExitBuilding));
		}
	}

	private void SetAvailableFitnessPlanningBoards()
	{
		_availableFitnessPlanningBoards.Clear();
		InstanceBehavior<BuildingManager>.Instance.GetEmployeeStationControllersWithAssignedEmployee(ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.isgymtrainingstation), _availableFitnessPlanningBoards);
	}

	protected override void SetAppearance()
	{
		if (arrivedWithSportClothes)
		{
			tpc.appearanceSetter.SetRandomAppearance(new AppearanceTag[1] { AppearanceTag.Sport });
		}
		else
		{
			base.SetAppearance();
			characterDataAtStart = tpc.appearanceSetter.data.Copy();
		}
	}

	public override void Leave()
	{
		ForceFinishOrder();
		base.Leave();
	}

	private void CheckGymTrainerDemand()
	{
		otherDemands.Clear();
		if (_availableFitnessPlanningBoards.Count == 0)
		{
			otherDemands.Add(CharacterEmojiName.CustomerNoGymTrainers);
		}
	}

	public override void SetCurrentTimeState()
	{
		if (customerEntry.spawnTime.GetTotalMinutes() + 45f <= TimeHelper.NowInMinutes())
		{
			customerTimeState = CustomerTimeState.AlmostLeaving;
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
		InstanceBehavior<BuildingManager>.Instance.customerSpawner.ReleaseCustomer(this, CustomerType.Gym);
	}

	public void ChangeGymClothes(bool backToOriginal)
	{
		if (backToOriginal)
		{
			tpc.appearanceSetter.SetAppearance(characterDataAtStart);
			return;
		}
		tpc.appearanceSetter.RandomizeClothes(new AppearanceTag[1] { AppearanceTag.Sport });
	}

	private void SetCustomerService()
	{
		EmployeeStationController random = _availableFitnessPlanningBoards.GetRandom();
		order.customerServiceSkill = (random ? random.employeeInstance.GetCustomerSatisfaction(random.ItemInstance) : 0f);
	}
}
