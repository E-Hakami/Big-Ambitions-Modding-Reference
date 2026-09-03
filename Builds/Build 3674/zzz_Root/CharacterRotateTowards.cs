using System.Collections;
using UnityEngine;

public class CharacterRotateTowards
{
	private ThirdPersonCharacter _tpc;

	private MonoBehaviour _coroutineHolder;

	private bool _isRotating;

	private Coroutine _coroutine;

	public void Init(ThirdPersonCharacter tpc, MonoBehaviour coroutineHolder = null)
	{
		_tpc = tpc;
		_coroutineHolder = coroutineHolder ?? tpc;
	}

	public void StartRotatingTowards(Vector3 target, float duration = 1f)
	{
		_isRotating = true;
		_coroutine = _coroutineHolder.StartCoroutine(RotateTowards(target, duration));
	}

	private IEnumerator RotateTowards(Vector3 target, float duration)
	{
		yield return _tpc.RotateTowards(target, duration);
		_isRotating = false;
	}

	public bool HasFinishedRotating()
	{
		return !_isRotating;
	}

	public void StopRotating()
	{
		if (_coroutine != null)
		{
			_coroutineHolder.StopCoroutine(_coroutine);
		}
		_isRotating = false;
	}
}
