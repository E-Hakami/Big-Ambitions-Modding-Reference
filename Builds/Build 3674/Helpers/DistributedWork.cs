using System;
using System.Collections.Generic;
using UnityEngine;

namespace Helpers;

public class DistributedWork<T>
{
	private const float MaxMsPerFrame = 2f;

	private readonly Queue<T> _pendingWork;

	private readonly Action<T> _updateAction;

	private readonly int _minItemsToProcess;

	private readonly int _maxQueueSize;

	public bool HasPendingWork => _pendingWork.Count > 0;

	public DistributedWork(Action<T> updateAction, int maxQueueSize = 2000, int minItemsToProcess = 1)
	{
		_updateAction = updateAction;
		_maxQueueSize = maxQueueSize;
		_minItemsToProcess = minItemsToProcess;
		_pendingWork = new Queue<T>((_maxQueueSize > 0) ? (_maxQueueSize / 2) : 10);
	}

	public bool Enqueue(T item)
	{
		if (_maxQueueSize <= 0 || _pendingWork.Count < _maxQueueSize)
		{
			_pendingWork.Enqueue(item);
			return true;
		}
		if (Application.isEditor)
		{
			Debug.LogWarning($"Pending work queue is full. Item {item} was not enqueued.");
		}
		return false;
	}

	public void ProgressWork()
	{
		if (_pendingWork.Count == 0)
		{
			return;
		}
		int num = 0;
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		while (_pendingWork.Count > 0)
		{
			T obj = _pendingWork.Dequeue();
			_updateAction(obj);
			num++;
			if (Time.realtimeSinceStartup - realtimeSinceStartup >= 0.002f && num >= _minItemsToProcess)
			{
				break;
			}
		}
	}

	public T PeekNextWork()
	{
		if (_pendingWork.Count <= 0)
		{
			return default(T);
		}
		return _pendingWork.Peek();
	}

	public void ForceCompleteAllWork()
	{
		while (_pendingWork.Count > 0)
		{
			T obj = _pendingWork.Dequeue();
			_updateAction(obj);
		}
	}

	public void DiscardPendingWork()
	{
		_pendingWork.Clear();
	}
}
