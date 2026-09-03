using System;
using Entities;
using UnityEngine;

namespace Streets.Pedestrians;

public class PedestrianStationaryBehavior : MonoBehaviour
{
	private const float MinTimeToChangeState = 30f;

	private const float MaxTimeToChangeState = 60f;

	[SerializeField]
	private StationaryAiBehavior stationaryAiBehavior;

	[SerializeField]
	private ThirdPersonCharacter tpc;

	private Action<bool> _changeState;

	private bool _enabled;

	private bool _isInfinite;

	private float _timeToChangeState;

	public void Initialize()
	{
		stationaryAiBehavior.Initialize(initAppearance: false, initAnimations: false);
	}

	private void Update()
	{
		if (_enabled && !_isInfinite && Time.time > _timeToChangeState)
		{
			_changeState?.Invoke(obj: true);
		}
	}

	public void Enable(bool isInfinite = false)
	{
		_enabled = true;
		_isInfinite = isInfinite;
		tpc.navmeshAgent.enabled = false;
		tpc.capsuleCollider.enabled = false;
		stationaryAiBehavior.Enable();
		_timeToChangeState = Time.time + UnityEngine.Random.Range(30f, 60f);
	}

	public void Disable()
	{
		_enabled = false;
		tpc.navmeshAgent.enabled = true;
		tpc.capsuleCollider.enabled = true;
		stationaryAiBehavior.Disable();
	}

	public void SetChangeStateCallback(Action<bool> changeState)
	{
		_changeState = changeState;
	}
}
