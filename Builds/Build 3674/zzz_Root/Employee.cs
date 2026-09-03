using System;
using System.Collections;
using BigAmbitions.DayNightCycle;
using Entities;
using Helpers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class Employee : MonoBehaviour
{
	private const bool WantsToiletPrivacy = false;

	private const int ToiletIntervalMinutes = 360;

	private const int ToiletRetryMinutesMin = 20;

	private const int ToiletRetryMinutesMax = 40;

	private const float WashHandsChance = 0.9f;

	public EmployeeInstance employeeInstance;

	public Customer customer;

	public ThirdPersonCharacter employeeTpc;

	public bool isPlayer;

	[FormerlySerializedAs("employeeStation")]
	public EmployeeStationController employeeStationController;

	protected Coroutine activeCoroutine;

	private ObstacleAvoidanceType _oldType;

	private float _oldSpeed;

	private Timestamp _nextToiletTime;

	private Vector3 _returnPosition;

	private Quaternion _returnRotation;

	private MultipleHeightsBuildingController _multipleHeightsBuildingController;

	public bool IsAway { get; private set; }

	protected virtual float GetAgentSpeed()
	{
		return InstanceBehavior<GlobalReferences>.Instance.walkingSpeedWalkFast;
	}

	protected virtual void Awake()
	{
		employeeTpc = GetComponent<ThirdPersonCharacter>();
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(OnNewHour));
		GlobalEvents.onGameUnloaded = (Action)Delegate.Combine(GlobalEvents.onGameUnloaded, new Action(OnGameUnloaded));
	}

	private void OnGameUnloaded()
	{
		if (activeCoroutine != null)
		{
			StopCoroutine(activeCoroutine);
		}
		base.enabled = false;
	}

	private void OnNewHour()
	{
		if (activeCoroutine != null && !InstanceBehavior<BuildingManager>.Instance.isOpen && customer != null && !customer.isPlayer)
		{
			StartCoroutine(CancelCurrentOrder());
		}
	}

	public virtual void Start()
	{
		isPlayer = base.gameObject.CompareTag("Player");
		_nextToiletTime = TimeHelper.Now().AddMinutes(UnityEngine.Random.Range(0, 360));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		_multipleHeightsBuildingController = InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController;
		if (_multipleHeightsBuildingController != null)
		{
			GlobalEvents.onCurrentHeightChanged = (Action)Delegate.Combine(GlobalEvents.onCurrentHeightChanged, new Action(OnCurrentHeightChanged));
			AppearanceSetter appearanceSetter = employeeTpc.appearanceSetter;
			appearanceSetter.visualsUpdated = (Action)Delegate.Combine(appearanceSetter.visualsUpdated, new Action(OnVisualsUpdated));
		}
	}

	private void OnVisualsUpdated()
	{
		employeeTpc.visible = true;
	}

	private void OnCurrentHeightChanged()
	{
		employeeTpc.OnVisibleChanged(_multipleHeightsBuildingController.GetPositionVisible(base.transform.position));
	}

	protected virtual void Update()
	{
		TryStartToiletCoroutine();
	}

	private void LateUpdate()
	{
		if (_multipleHeightsBuildingController != null)
		{
			employeeTpc.OnVisibleChanged(_multipleHeightsBuildingController.GetPositionVisible(base.transform.position));
		}
	}

	private void OnExitBuilding(Address _)
	{
		if (employeeStationController.employee == this)
		{
			employeeStationController = null;
		}
		StopAllCoroutines();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	protected void SetAgentProperties()
	{
		_oldSpeed = employeeTpc.navmeshAgent.speed;
		employeeTpc.navmeshAgent.speed = GetAgentSpeed();
		_oldType = employeeTpc.navmeshAgent.obstacleAvoidanceType;
		employeeTpc.navmeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
	}

	protected void RevertAgentProperties()
	{
		if (isPlayer)
		{
			PlayerHelper.PlayerController.Character.SetWalkingSpeed(PlayerHelper.PlayerController.Character.walkingSpeed);
		}
		else
		{
			employeeTpc.navmeshAgent.speed = _oldSpeed;
		}
		employeeTpc.navmeshAgent.obstacleAvoidanceType = _oldType;
	}

	protected bool IsEmployeeAvailable()
	{
		if (!customer)
		{
			return activeCoroutine == null;
		}
		return false;
	}

	protected void CallForNextCustomer()
	{
		customer = employeeStationController.GetWaitingLine().data.GetNextCustomer();
		if (customer != null)
		{
			activeCoroutine = StartCoroutine(ServeCustomer());
		}
	}

	protected virtual IEnumerator ServeCustomer()
	{
		yield break;
	}

	protected virtual void TryStartToiletCoroutine()
	{
		if (!isPlayer && !_nextToiletTime.IsInTheFuture() && IsEmployeeAvailable() && (!employeeStationController || !employeeStationController.GetWaitingLine() || employeeStationController.GetWaitingLine().data.GetCustomersInWaitingLine(includeGoingToWaitingLine: true) <= 0))
		{
			_returnPosition = base.transform.position;
			_returnRotation = base.transform.rotation;
			activeCoroutine = StartCoroutine(UseToiletOrSink(isSink: false));
		}
	}

	private IEnumerator UseToiletOrSink(bool isSink)
	{
		NavMeshAgent agent = employeeTpc.navmeshAgent;
		HygieneItemController controller = (isSink ? TryUseToilet.FindSink(employeeTpc) : TryUseToilet.FindToilet(employeeTpc, wantsPrivacy: false));
		if (!controller)
		{
			if (!isSink)
			{
				RescheduleToilet();
				activeCoroutine = null;
			}
			yield break;
		}
		IsAway = true;
		employeeTpc.Reset();
		employeeTpc.ReleaseItemIKTargets();
		if (!agent.SetDestination(controller.GetNavMeshTargetPosition()))
		{
			controller = null;
		}
		yield return new WaitUntil(() => !controller || controller.Occupied || (!agent.pathPending && agent.remainingDistance <= 0.1f));
		if (!controller || !controller.BeginUse(employeeTpc))
		{
			if (!isSink)
			{
				RescheduleToilet();
				employeeTpc.visible = true;
				activeCoroutine = StartCoroutine(ReturnToStation());
			}
			yield break;
		}
		_nextToiletTime = TimeHelper.Now().AddMinutes(controller.hygieneEnvironment.GetDefaultMinutes());
		while (_nextToiletTime.IsInTheFuture())
		{
			controller.UpdateRotation(employeeTpc);
			yield return null;
		}
		if ((bool)controller)
		{
			controller.EndUse(employeeTpc);
		}
		if (!isSink)
		{
			if (UnityEngine.Random.value < 0.9f)
			{
				yield return UseToiletOrSink(isSink: true);
			}
			_nextToiletTime = TimeHelper.Now().AddMinutes(360f);
			activeCoroutine = StartCoroutine(ReturnToStation());
		}
	}

	private void RescheduleToilet()
	{
		_nextToiletTime = TimeHelper.Now().AddMinutes(UnityEngine.Random.Range(20, 40));
	}

	private IEnumerator ReturnToStation()
	{
		employeeTpc.Reset();
		NavMeshAgent agent = employeeTpc.navmeshAgent;
		if (agent.SetDestination(_returnPosition))
		{
			yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= 0.1f);
		}
		IsAway = false;
		agent.ResetPath();
		if ((bool)employeeStationController)
		{
			employeeStationController.AssignEmployee(employeeTpc, employeeInstance);
		}
		agent.Warp(_returnPosition);
		base.transform.SetPositionAndRotation(_returnPosition, _returnRotation);
		activeCoroutine = null;
	}

	public virtual void SetEmployeeStation(EmployeeStationController stationController)
	{
		employeeStationController = stationController;
	}

	public virtual IEnumerator CancelCurrentOrder()
	{
		yield break;
	}

	private void OnMouseDown()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			employeeStationController.Interact();
		}
	}

	private void OnDisable()
	{
		if (activeCoroutine != null)
		{
			StopCoroutine(activeCoroutine);
			activeCoroutine = null;
		}
		if (!GameManager.isCitySceneBeingUnloaded && employeeTpc.CurrentEntityController is HygieneItemController hygieneItemController)
		{
			hygieneItemController.EndUse(employeeTpc);
		}
	}

	protected virtual void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
			GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(OnNewHour));
			GlobalEvents.onGameUnloaded = (Action)Delegate.Remove(GlobalEvents.onGameUnloaded, new Action(OnGameUnloaded));
			if (!(_multipleHeightsBuildingController == null))
			{
				GlobalEvents.onCurrentHeightChanged = (Action)Delegate.Remove(GlobalEvents.onCurrentHeightChanged, new Action(OnCurrentHeightChanged));
				AppearanceSetter appearanceSetter = employeeTpc.appearanceSetter;
				appearanceSetter.visualsUpdated = (Action)Delegate.Remove(appearanceSetter.visualsUpdated, new Action(OnVisualsUpdated));
			}
		}
	}
}
