using System;
using Buildings.Retail.Businesses.CinemaTheater;
using Entities;
using JimmysUnityUtilities;
using Player.HUD.ItemWarningIcons;
using UnityEngine;
using UnityEngine.AI;

namespace Controllers;

public class CinemaTheaterBoothController : EmployeeStationController
{
	public override void Start()
	{
		if (itemName == "ba:itemname_dressingroom")
		{
			employeeType = typeof(ActorEmployee);
			if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
			{
				GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(OnDressingRoomNewHour));
			}
		}
		base.Start();
		if (!(itemName != "ba:itemname_dressingroom"))
		{
			employeeType = typeof(ActorEmployee);
			if (base.BuildingContext.IsPlayerOwnedBusiness)
			{
				GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(OnDressingRoomNewHour));
			}
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (itemName == "ba:itemname_dressingroom")
		{
			GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(OnDressingRoomNewHour));
		}
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance newEmployeeInstance)
	{
		base.AssignEmployee(tpc, newEmployeeInstance);
		if (employee is ActorEmployee)
		{
			tpc.SitOnChair(employeeSpot);
			tpc.DestroyBoredAnimations();
			TheaterStage.DeferRedistributeActors();
			return;
		}
		Vector3 position = base.transform.position;
		Quaternion rotation = base.transform.rotation;
		if ((bool)employeeSpot)
		{
			position = employeeSpot.position;
			rotation = employeeSpot.rotation;
		}
		else
		{
			bool flag = false;
			Transform[] array = navMeshTargets;
			foreach (Transform transform in array)
			{
				if (!PathHelper.IsThereAWallBetweenTargetAndEntity(transform.position + Vector3.up, base.transform.position + Vector3.up) && NavMesh.SamplePosition(transform.position, out var hit, 1f, -1))
				{
					position = hit.position;
					rotation = transform.rotation;
					flag = true;
					break;
				}
			}
			if (!flag && navMeshTargets.Length != 0)
			{
				position = navMeshTargets[0].position;
				rotation = navMeshTargets[0].rotation;
			}
		}
		tpc.ForceToPosition(position);
		tpc.transform.rotation = rotation;
		tpc.DestroyBoredAnimations();
	}

	public override void UnassignEmployee()
	{
		base.UnassignEmployee();
		if (employeeType == typeof(ActorEmployee))
		{
			TheaterStage.DeferRedistributeActors();
		}
	}

	private void OnDressingRoomNewHour()
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if ((bool)InstanceBehavior<ItemWarningIconManager>.Instance)
			{
				InstanceBehavior<ItemWarningIconManager>.Instance.UpdateWarningIcon(this);
			}
		});
	}
}
