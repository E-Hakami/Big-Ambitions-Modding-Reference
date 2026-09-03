using System.Collections;
using UnityEngine;

public class SingleCoroutineStarterStopper : MonoBehaviour
{
	private IEnumerator _activeCoroutine;

	public void StopActiveCoroutine()
	{
		StopCoroutine(_activeCoroutine);
	}

	public void StartNewCoroutine(IEnumerator coroutine)
	{
		_activeCoroutine = coroutine;
		StartCoroutine(_activeCoroutine);
	}
}
