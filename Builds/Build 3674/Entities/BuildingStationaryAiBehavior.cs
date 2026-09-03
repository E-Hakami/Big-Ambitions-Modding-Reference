using System;
using System.Collections;
using System.Collections.Generic;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using Controllers;
using Extensions;
using UnityEngine;

namespace Entities;

public class BuildingStationaryAiBehavior : MonoBehaviour
{
	private const int ToiletIntervalMinutes = 60;

	private const float ToiletChance = 0.2f;

	private const float WashHandsChance = 0.9f;

	private const float ChairRetryMinutes = 5f;

	private const int ToiletTries = 3;

	[SerializeField]
	private ThirdPersonCharacter tpc;

	private bool _enabled;

	private bool _initialized;

	private BuildingStationaryAiData _data;

	private SeatController _seatController;

	private Transform _sittingPosition;

	private string _seatItemName;

	private Timestamp _nextToiletTime;

	private Coroutine _toiletCoroutine;

	private readonly List<SeatController> _availableChairs = new List<SeatController>();

	private bool _toiletUsed;

	public void Initialize(BuildingStationaryAiData data, SeatController seatController, Transform sittingPosition)
	{
		_data = data;
		_seatController = seatController;
		_sittingPosition = sittingPosition;
		_seatItemName = seatController.itemName;
		tpc.SitOnChair(sittingPosition);
		InitAppearance();
		InitScreenIfRequired();
		ScheduleNextToilet(UnityEngine.Random.Range(0, 60));
		_initialized = true;
	}

	private void InitScreenIfRequired()
	{
		if (_data.useScreen)
		{
			_seatController.parentItemController?.PlayVideoOnScreen(_data.screenVideoType);
		}
	}

	private void InitAppearance()
	{
		tpc.appearanceSetter.SetRandomAge();
		tpc.appearanceSetter.SetRandomAppearance(_data.appearanceTags);
		if (_data.isEmployee)
		{
			tpc.appearanceSetter.data.skills = new List<Skill>
			{
				new Skill
				{
					name = _data.skill,
					value = 50f
				}
			};
			EmployeeInstance employeeInstance = new EmployeeInstance
			{
				characterData = tpc.appearanceSetter.data
			};
			tpc.appearanceSetter.UpdateElements(employeeInstance.GetUniformElements(null).Copy());
		}
	}

	private void Update()
	{
		if (_enabled && _initialized && _toiletCoroutine == null && !_nextToiletTime.IsInTheFuture())
		{
			if (UnityEngine.Random.value >= 0.2f)
			{
				ScheduleNextToilet(60f);
			}
			else
			{
				_toiletCoroutine = StartCoroutine(GoToToiletAndReturn());
			}
		}
	}

	private IEnumerator GoToToiletAndReturn()
	{
		_toiletUsed = false;
		int toiletTries = 0;
		while (!_toiletUsed && toiletTries < 3)
		{
			yield return UseToiletOrSink(isSink: false);
			toiletTries++;
		}
		if (tpc.isSittingOn == null)
		{
			yield return ReturnToChair();
		}
		_toiletCoroutine = null;
		ScheduleNextToilet(60f);
	}

	private IEnumerator UseToiletOrSink(bool isSink)
	{
		HygieneItemController controller = (isSink ? TryUseToilet.FindSink(tpc) : TryUseToilet.FindToilet(tpc, wantsPrivacy: false));
		if (controller == null)
		{
			yield break;
		}
		if ((bool)tpc.isSittingOn)
		{
			StandUpFromChair();
		}
		if (!tpc.navmeshAgent.SetDestination(controller.GetNavMeshTargetPosition()))
		{
			yield break;
		}
		yield return new WaitUntil(() => controller.Occupied || (!tpc.navmeshAgent.pathPending && tpc.navmeshAgent.remainingDistance <= 0.1f));
		if (controller.BeginUse(tpc))
		{
			if (!isSink)
			{
				_toiletUsed = true;
			}
			Timestamp waitUntil = TimeHelper.Now().AddMinutes(controller.hygieneEnvironment.GetDefaultMinutes());
			while (waitUntil.IsInTheFuture())
			{
				controller.UpdateRotation(tpc);
				yield return null;
			}
			controller.EndUse(tpc);
			if (!isSink && UnityEngine.Random.value < 0.9f)
			{
				yield return UseToiletOrSink(isSink: true);
			}
		}
	}

	private void StandUpFromChair()
	{
		tpc.Reset();
		tpc.WarpSafely(_seatController.GetNavMeshTargetPosition());
		_seatController.OnSittingChanged(_sittingPosition, _data.isEmployee);
	}

	private IEnumerator ReturnToChair()
	{
		while (_enabled)
		{
			UpdateChair();
			if (_seatController == null || !tpc.navmeshAgent.SetDestination(_seatController.GetClosestNavMeshTargetPositionStraightLine(_sittingPosition.position)))
			{
				if (IndoorCustomerSpawner.GetRandomPositionInside(out var randomPosition))
				{
					tpc.navmeshAgent.SetDestination(randomPosition);
				}
				Timestamp retryAt = TimeHelper.Now().AddMinutes(5f);
				yield return new WaitUntil(() => retryAt.IsInThePast());
			}
			else
			{
				yield return new WaitUntil(() => !IsCurrentChairAvailable() || (!tpc.navmeshAgent.pathPending && tpc.navmeshAgent.remainingDistance <= 0.1f));
				if (IsCurrentChairAvailable())
				{
					tpc.SitOnChair(_sittingPosition);
					break;
				}
			}
		}
	}

	private void UpdateChair()
	{
		if (IsCurrentChairAvailable())
		{
			return;
		}
		_availableChairs.Clear();
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (allItemController is SeatController seatController && !(seatController.itemName != _seatItemName))
			{
				PlayerItemPurchaserSettings playerItemPurchaserSettings = seatController.playerItemPurchaserSettings;
				if ((playerItemPurchaserSettings == null || !playerItemPurchaserSettings.enabled) && (bool)seatController.SittingPosition)
				{
					_availableChairs.Add(seatController);
				}
			}
		}
		_seatController = _availableChairs.GetRandom();
		if (!(_seatController == null))
		{
			_sittingPosition = _seatController.SittingPosition;
		}
	}

	private bool IsCurrentChairAvailable()
	{
		if (_data.isEmployee)
		{
			return true;
		}
		if (_seatController != null)
		{
			return _seatController.IsSittingPositionAvailable(_sittingPosition);
		}
		return false;
	}

	private void ScheduleNextToilet(float minutes)
	{
		_nextToiletTime = TimeHelper.Now().AddMinutes(minutes);
	}

	public void Disable()
	{
		if (_toiletCoroutine != null)
		{
			StopCoroutine(_toiletCoroutine);
			_toiletCoroutine = null;
		}
		if (tpc.CurrentEntityController is HygieneItemController hygieneItemController)
		{
			hygieneItemController.EndUse(tpc);
		}
		_initialized = false;
		_enabled = false;
	}

	public void Enable()
	{
		_enabled = true;
	}
}
