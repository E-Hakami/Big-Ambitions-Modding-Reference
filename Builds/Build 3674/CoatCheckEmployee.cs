using System.Collections;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.SoundSystem;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Controllers;
using Entities;
using Helpers;
using UnityEngine;

public class CoatCheckEmployee : Employee
{
	public override void Start()
	{
		base.Start();
		SetAgentProperties();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		RevertAgentProperties();
	}

	protected override void Update()
	{
		if (IsEmployeeAvailable())
		{
			CallForNextCustomer();
			TryStartToiletCoroutine();
		}
	}

	public override IEnumerator CancelCurrentOrder()
	{
		if (activeCoroutine != null)
		{
			StopCoroutine(activeCoroutine);
			activeCoroutine = null;
			customer = null;
			employeeTpc.SetHandContent(null);
			yield return employeeTpc.MoveToPosition(employeeStationController.transform, employeeStationController.GetEmployeePosition());
		}
		else
		{
			yield return new WaitForEndOfFrame();
		}
	}

	protected override IEnumerator ServeCustomer()
	{
		customer.order.customerServiceSkill = employeeInstance.GetSkillValue("ba:skill_customerservice") * (employeeInstance.satisfaction / 100f);
		customer.state = CustomerState.BeingServed;
		if (customer != null)
		{
			yield return null;
		}
		ThirdPersonCharacter componentInChildren = customer.GetComponentInChildren<ThirdPersonCharacter>();
		OrderEntry coatCheckFeeEntry = customer.order.entries.FirstOrDefault((OrderEntry x) => x.itemName == "ba:itemname_coatcheckfee");
		if (coatCheckFeeEntry == null)
		{
			FinishServingCustomer();
			yield break;
		}
		coatCheckFeeEntry.available = true;
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && !IsCoatCheckFeeAcceptable())
		{
			coatCheckFeeEntry.processed = true;
			coatCheckFeeEntry.priceAccceptable = false;
			yield return componentInChildren.ShowExpression(CharacterEmojiName.CustomerTooHighPrice, 1f, new
			{
				itemname = LocalizationHelper.GetItemLabel("ba:itemname_coatcheckfee")
			});
			FinishServingCustomer();
			yield break;
		}
		componentInChildren.RemoveHandObject();
		componentInChildren.StopHoldingACoat();
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, employeeStationController.ItemInstance);
		CoatCheckController coatCheckController = (CoatCheckController)employeeStationController;
		yield return employeeTpc.animator.RunAnimation(AnimationType.StoringAJacket, 2f);
		coatCheckController.IncreaseStoredCoats();
		employeeTpc.RemoveHandObject();
		NightclubCustomer nightclubCustomer = (NightclubCustomer)customer;
		nightclubCustomer.coatCheckController = coatCheckController;
		nightclubCustomer.isHoldingACoat = false;
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.PurchaseSuccess, coatCheckController.transform.position, 1f, isPlayer);
		coatCheckFeeEntry.paid = true;
		coatCheckFeeEntry.processed = true;
		if (IsLastOrderEntry())
		{
			nightclubCustomer.CompleteOrder();
		}
		FinishServingCustomer();
	}

	private bool IsLastOrderEntry()
	{
		return customer.order.entries.All((OrderEntry x) => x.processed);
	}

	private bool IsCoatCheckFeeAcceptable()
	{
		float price = ItemHelper.GetPrice("ba:itemname_coatcheckfee", InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		return customer.citizenData.IsPriceAcceptable("ba:itemname_coatcheckfee", price);
	}

	private void FinishServingCustomer()
	{
		if ((bool)customer)
		{
			customer.state = CustomerState.Served;
			employeeStationController.GetWaitingLine().customersManagement.RemoveCustomer(customer);
			customer = null;
			activeCoroutine = null;
		}
	}

	public override void SetEmployeeStation(EmployeeStationController stationController)
	{
		base.SetEmployeeStation(stationController);
		employeeTpc.navmeshAgent.Warp(stationController.GetEmployeePosition());
		employeeTpc.LookTarget = stationController.transform.position;
		employeeTpc.LookTarget.y = base.transform.position.y;
	}
}
