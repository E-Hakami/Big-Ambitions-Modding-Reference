using System.Collections;
using System.Collections.Generic;
using Controllers;
using Extensions;
using UnityEngine;

namespace Buildings.Retail.Businesses.CinemaTheater;

public class TheaterStage : MonoBehaviour
{
	private const float RedistributionIntervalMin = 10f;

	private const float RedistributionIntervalMax = 20f;

	private const float PositionOffsetMax = 0.5f;

	private const int MinCustomersForClapping = 5;

	private const float ClappingPitchVariation = 0.1f;

	private static readonly Dictionary<Transform, ActorEmployee> PositionedActors = new Dictionary<Transform, ActorEmployee>();

	private static readonly List<ActorEmployee> TempActors = new List<ActorEmployee>();

	private static readonly Dictionary<ActorEmployee, Transform> TempNewActorPositions = new Dictionary<ActorEmployee, Transform>();

	[SerializeField]
	private Transform[] actorPositions;

	public Transform audienceStandingArea;

	[SerializeField]
	private AudioSource applauseAudioSource;

	private Coroutine _redistributeCoroutine;

	private int _currentActors;

	private int _maxActors;

	private float _redistributionTimer;

	public static TheaterStage ActiveInstance { get; private set; }

	public int CurrentActorCount => _currentActors;

	private void OnEnable()
	{
		if ((bool)ActiveInstance && ActiveInstance != this)
		{
			Debug.LogError("Multiple TheaterStage instances are active in the scene. There should only be one active instance at a time.", this);
			return;
		}
		ActiveInstance = this;
		_redistributionTimer = Random.Range(10f, 20f);
	}

	private void OnDisable()
	{
		if (ActiveInstance == this)
		{
			ActiveInstance = null;
		}
	}

	private void Update()
	{
		if (!(ActiveInstance != this))
		{
			_redistributionTimer -= Time.deltaTime;
			if (_redistributionTimer < 0f)
			{
				DeferRedistributeActors(walkInOut: true);
			}
		}
	}

	private Transform GetFirstAvailablePosition()
	{
		for (int i = 0; i < Mathf.Min(actorPositions.Length, _maxActors); i++)
		{
			Transform transform = actorPositions[i];
			if (!PositionedActors.ContainsKey(transform))
			{
				return transform;
			}
		}
		return null;
	}

	private static void FindActors()
	{
		TempActors.Clear();
		if (!BuildingManager.IsInsideBuilding || (InstanceBehavior<BuildingManager>.Instance.building.BuildingType != "ba:buildingtype_cinema" && InstanceBehavior<BuildingManager>.Instance.building.BuildingType != "ba:buildingtype_theater"))
		{
			return;
		}
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (allItemController is CinemaTheaterBoothController { employee: ActorEmployee employee })
			{
				TempActors.Add(employee);
			}
		}
		TempActors.Shuffle();
	}

	public static void DeferRedistributeActors(bool walkInOut = false)
	{
		if ((bool)ActiveInstance && ActiveInstance._redistributeCoroutine == null)
		{
			ActiveInstance._redistributeCoroutine = ActiveInstance.StartCoroutine(ActiveInstance.RedistributeActorsCoroutine(walkInOut));
		}
	}

	private IEnumerator RedistributeActorsCoroutine(bool walkInOut)
	{
		yield return null;
		if (walkInOut && PositionedActors.Count > 0)
		{
			StartAudienceClapping();
		}
		_currentActors = 0;
		_maxActors = Random.Range(1, actorPositions.Length + 1);
		_redistributionTimer = Random.Range(10f, 20f);
		PositionedActors.Clear();
		FindActors();
		TempNewActorPositions.Clear();
		foreach (ActorEmployee tempActor in TempActors)
		{
			Transform firstAvailablePosition = GetFirstAvailablePosition();
			if ((bool)firstAvailablePosition)
			{
				PositionedActors[firstAvailablePosition] = tempActor;
				TempNewActorPositions[tempActor] = firstAvailablePosition;
			}
			tempActor.ClearActingStage(walkInOut);
		}
		if (walkInOut)
		{
			yield return new WaitUntil(HaveActorsFinishedTransition);
		}
		_currentActors = TempNewActorPositions.Count;
		foreach (var (actorEmployee2, transform2) in TempNewActorPositions)
		{
			if (_currentActors == 1)
			{
				Vector3 vector = Random.onUnitSphere * 0.5f;
				vector.y = 0f;
				actorEmployee2.SetActingStage(this, base.transform.position + vector, base.transform.rotation, walkInOut);
			}
			else
			{
				Vector3 vector2 = Random.Range(-0.25f, 0.25f) * transform2.forward;
				actorEmployee2.SetActingStage(this, transform2.position + vector2, transform2.rotation, walkInOut);
			}
		}
		if (walkInOut)
		{
			yield return new WaitUntil(HaveActorsFinishedTransition);
		}
		_redistributeCoroutine = null;
	}

	private static bool HaveActorsFinishedTransition()
	{
		foreach (ActorEmployee tempActor in TempActors)
		{
			if (tempActor.InTransition)
			{
				return false;
			}
		}
		return true;
	}

	private void StartAudienceClapping()
	{
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (Customer customer in IndoorCustomerSpawner.Customers)
		{
			if (customer is CinemaTheaterCustomer cinemaTheaterCustomer && customer.tpc.isActiveAndEnabled && (bool)customer.tpc.isSittingOn && CinemaTheaterHelper.IsValidSittingPosition(customer.tpc.isSittingOn))
			{
				cinemaTheaterCustomer.Invoke("StartClapping", Random.Range(0f, 0.3f));
				zero += customer.transform.position;
				num++;
			}
		}
		if (num >= 5)
		{
			Vector3 position = zero / num;
			applauseAudioSource.transform.position = position;
			applauseAudioSource.pitch = 1f + Random.Range(-0.1f, 0.1f);
			applauseAudioSource.Play();
		}
	}
}
