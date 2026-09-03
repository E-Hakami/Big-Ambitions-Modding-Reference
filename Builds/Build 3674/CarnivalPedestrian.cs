using System.Collections;
using System.Collections.Generic;
using Controllers;
using Extensions;
using UnityEngine;

public class CarnivalPedestrian : MonoBehaviour
{
	public ThirdPersonCharacter tpc;

	[HideInInspector]
	public SkinnedMeshRenderer skinnedMeshRenderer;

	private readonly List<ICarnivalNpcItem> _carnivalNpcItems = new List<ICarnivalNpcItem>();

	private Transform _parentTransform;

	private ICarnivalNpcItem _lastCarnivalNpcItem;

	private IEnumerator _movementCoroutine;

	private int _currentWaitingIndex;

	public int GetCurrentWaitingIndex()
	{
		return _currentWaitingIndex;
	}

	public void SetCurrentWaitingIndex(int index)
	{
		_currentWaitingIndex = index;
	}

	public void Init(IReadOnlyCollection<ICarnivalNpcItem> carnivalNpcItems)
	{
		_parentTransform = tpc.transform.parent;
		_carnivalNpcItems.Clear();
		_carnivalNpcItems.AddRange(carnivalNpcItems);
	}

	public void SetLastCarnivalNpcItem(ICarnivalNpcItem carnivalNpcItem)
	{
		_lastCarnivalNpcItem = carnivalNpcItem;
	}

	public void ResetCarnivalPedestrian()
	{
		skinnedMeshRenderer.enabled = true;
		if (_parentTransform != null)
		{
			tpc.transform.SetParent(_parentTransform);
		}
		tpc.Reset();
		if (_movementCoroutine != null)
		{
			StopCoroutine(_movementCoroutine);
		}
	}

	public void OnCarnivalItemEnd(Vector3 exitPosition = default(Vector3))
	{
		if (exitPosition != default(Vector3))
		{
			tpc.navmeshAgent.Warp(exitPosition);
		}
		ResetCarnivalPedestrian();
		GoNextCarnivalItem();
	}

	private void GoNextCarnivalItem()
	{
		_carnivalNpcItems.Remove(_lastCarnivalNpcItem);
		_carnivalNpcItems.Shuffle();
		_carnivalNpcItems.Add(_lastCarnivalNpcItem);
		foreach (ICarnivalNpcItem carnivalNpcItem in _carnivalNpcItems)
		{
			int waitingPositionIndex = carnivalNpcItem.GetWaitingPositionIndex();
			if (waitingPositionIndex != -1)
			{
				_currentWaitingIndex = waitingPositionIndex;
				_lastCarnivalNpcItem = carnivalNpcItem;
				Vector3 waitingPositionFromIndex = carnivalNpcItem.GetWaitingPositionFromIndex(_currentWaitingIndex);
				Quaternion waitingRotationFromIndex = carnivalNpcItem.GetWaitingRotationFromIndex(_currentWaitingIndex);
				Vector3 lookTarget = waitingPositionFromIndex + waitingRotationFromIndex * Vector3.forward;
				_movementCoroutine = tpc.MoveToPosition(lookTarget, waitingPositionFromIndex, 0.5f, rotateToLookTarget: true, OnCarnivalAttractionReached);
				StartCoroutine(_movementCoroutine);
				return;
			}
		}
		Debug.LogError("Carnival customers exceeded the available carnival items.");
	}

	private void OnCarnivalAttractionReached()
	{
		if (!_lastCarnivalNpcItem.TryEnqueueNpc(this))
		{
			GoNextCarnivalItem();
		}
	}
}
