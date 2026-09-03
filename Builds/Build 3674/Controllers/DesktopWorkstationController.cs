using BigAmbitions.Characters;
using Entities;
using UnityEngine;

namespace Controllers;

public class DesktopWorkstationController : EmployeeStationController
{
	public Transform employeeChair;

	[SerializeField]
	protected bool runTypingAnimation = true;

	private const float MinTimeBetweenAnims = 5f;

	private const float MaxTimeBetweenAnims = 15f;

	private Animator _employeeAnimator;

	private float _nextAnim;

	private float _animLength;

	public override void Awake()
	{
		base.Awake();
		employeeType = typeof(Employee);
		workEnergyConsumption = EnergyConsumption.Low;
	}

	private void Update()
	{
		if (!(employee == null) && !(_employeeAnimator == null) && runTypingAnimation)
		{
			if (_nextAnim <= 0f)
			{
				_nextAnim = Random.Range(5f, 15f) + _animLength;
				_employeeAnimator.SetTrigger(AnimationType.UsingDesktopComputer);
			}
			_nextAnim -= Time.deltaTime;
		}
	}

	public override Vector3 GetEmployeePosition()
	{
		if (employeeChair != null)
		{
			return employeeChair.position;
		}
		return base.GetEmployeePosition();
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance employeeInstance)
	{
		base.AssignEmployee(tpc, employeeInstance);
		tpc.SitOnChair(employeeChair);
		if (employeeChair.TryGetComponent<SeatController>(out var component))
		{
			component.UpdateRestingAvailability();
		}
		if (runTypingAnimation)
		{
			bool num = Random.Range(0f, 1f) < 0.15f;
			_employeeAnimator = employee.employeeTpc.animator;
			_animLength = _employeeAnimator.GetAnimationLength(AnimationType.UsingDesktopComputer);
			_nextAnim = Random.Range(0f, 15f);
			if (num)
			{
				_employeeAnimator.SetTrigger(AnimationType.UsingDesktopComputer);
				_nextAnim += _animLength;
			}
		}
	}

	public override void UnassignEmployee()
	{
		if (!(employee == null))
		{
			base.UnassignEmployee();
		}
	}
}
