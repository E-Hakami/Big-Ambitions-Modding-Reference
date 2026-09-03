using Entities;
using Helpers;
using UnityEngine;

namespace Controllers;

public class SecurityGuardLockerController : EmployeeStationController
{
	public override Vector3 GetEmployeePosition()
	{
		if (!IndoorCustomerSpawner.GetRandomPositionInside(out var randomPosition))
		{
			return base.GetEmployeePosition();
		}
		return randomPosition;
	}

	public override void Start()
	{
		employeeType = typeof(SecurityGuardEmployee);
		base.Start();
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance employeeInstance)
	{
		base.AssignEmployee(tpc, employeeInstance);
		tpc.GetComponent<SecurityGuardEmployee>().SetEmployeeStation(this);
	}

	public override EmployeeInstance GetAIEmployeeInstance()
	{
		return EmployeeHelper.CreateAIEmployeeInstance("ba:skill_securityguard");
	}
}
