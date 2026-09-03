using System.Collections.Generic;
using BigAmbitions.Characters;
using Controllers;
using Helpers;
using UnityEngine;

public class AttractionStateManager
{
	private const float InitialAnimationOffset = 1f;

	private const float EndAnimationOffset = 1f;

	private readonly Attraction _attraction;

	private AttractionState _currentAttractionState;

	private float _timer;

	private readonly float _runningTime;

	private bool _enabled;

	private readonly List<KeyValuePair<CarnivalPedestrian, float>> _npcsAnimationTimes = new List<KeyValuePair<CarnivalPedestrian, float>>();

	private float _playerAnimationTime = -1f;

	private float _npcAnimationLength;

	public AttractionStateManager(Attraction attraction)
	{
		_attraction = attraction;
		_currentAttractionState = AttractionState.Waiting;
		_runningTime = attraction.GetRunningTime();
		_npcAnimationLength = PlayerHelper.GetAnimator().GetAnimationLength(AnimationType.RidingAttraction);
	}

	public void Enable()
	{
		_enabled = true;
	}

	public void Reset(bool enabled = false)
	{
		_currentAttractionState = AttractionState.Waiting;
		_timer = 0f;
		_attraction.playableDirector.time = 0.0;
		_attraction.playableDirector.Evaluate();
		_attraction.playableDirector.Stop();
		_enabled = enabled;
	}

	public void Update()
	{
		if (!_enabled)
		{
			return;
		}
		_timer += Time.deltaTime;
		if (_currentAttractionState == AttractionState.Waiting)
		{
			if (_timer >= _attraction.waitTime)
			{
				OnAttractionStart();
			}
			return;
		}
		UpdateCharactersAnimations();
		if (_timer >= _runningTime)
		{
			OnAttractionEnd();
		}
	}

	private void UpdateCharactersAnimations()
	{
		for (int num = _npcsAnimationTimes.Count - 1; num >= 0; num--)
		{
			KeyValuePair<CarnivalPedestrian, float> keyValuePair = _npcsAnimationTimes[num];
			if (_timer > keyValuePair.Value)
			{
				keyValuePair.Key.tpc.animator.RunAnimationLength(AnimationType.RidingAttraction);
				_npcsAnimationTimes.RemoveAt(num);
			}
		}
		if (_attraction.IsPlayerRiding() && _playerAnimationTime > 0f && _timer > _playerAnimationTime)
		{
			PlayerHelper.GetAnimator().RunAnimationLength(AnimationType.RidingAttraction);
			_playerAnimationTime = -1f;
		}
	}

	private void OnAttractionStart()
	{
		_currentAttractionState = AttractionState.Running;
		_timer = 0f;
		_attraction.playableDirector.Play();
		_attraction.OnAttractionStart();
		SetAnimationTimes();
	}

	private void SetAnimationTimes()
	{
		_npcsAnimationTimes.Clear();
		foreach (CarnivalPedestrian item in _attraction.GetNpcsRidingAttraction())
		{
			float randomAnimationTime = GetRandomAnimationTime();
			_npcsAnimationTimes.Add(new KeyValuePair<CarnivalPedestrian, float>(item, randomAnimationTime));
		}
		_playerAnimationTime = GetRandomAnimationTime();
	}

	private float GetRandomAnimationTime()
	{
		float maxInclusive = _runningTime - _npcAnimationLength - 1f;
		return Random.Range(1f, maxInclusive);
	}

	private void OnAttractionEnd()
	{
		_currentAttractionState = AttractionState.Waiting;
		_timer = 0f;
		_attraction.OnAttractionEnd();
	}
}
