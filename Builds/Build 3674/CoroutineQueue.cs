using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineQueue
{
	private const int MaxQueueSize = 10;

	private readonly Queue<IEnumerator> _queue = new Queue<IEnumerator>(10);

	private float _lastCoroutineTime;

	private readonly MonoBehaviour _coroutineStarter;

	private bool _executingCoroutine;

	public CoroutineQueue(MonoBehaviour coroutineStarter)
	{
		_coroutineStarter = coroutineStarter;
	}

	public void AddCoroutine(IEnumerator coroutine)
	{
		if (_queue.Count < 10)
		{
			_queue.Enqueue(coroutine);
		}
	}

	public void Clear()
	{
		_queue.Clear();
	}

	public void Update()
	{
		if (!_executingCoroutine && _queue.Count > 0)
		{
			_coroutineStarter.StartCoroutine(ExecuteNextCoroutine());
		}
	}

	private IEnumerator ExecuteNextCoroutine()
	{
		IEnumerator enumerator = _queue.Dequeue();
		if (enumerator != null)
		{
			_executingCoroutine = true;
			yield return enumerator;
			_executingCoroutine = false;
		}
	}
}
