using System;
using System.Collections;
using BigAmbitions.DayNightCycle;
using UnityEngine;

namespace AI;

public class UmbrellaHandler
{
	private readonly BaseHuman _baseHuman;

	private readonly bool _hasUmbrella;

	private readonly Action _onUmbrellaRemoved;

	private readonly Action _onUmbrellaAdded;

	private Coroutine _currentUmbrellaCoroutine;

	private bool _isHoldingUmbrella;

	public UmbrellaHandler(BaseHuman baseHuman, bool hasUmbrella, Action onUmbrellaAdded = null, Action onUmbrellaRemoved = null)
	{
		_baseHuman = baseHuman;
		_hasUmbrella = hasUmbrella;
		_onUmbrellaAdded = onUmbrellaAdded;
		_onUmbrellaRemoved = onUmbrellaRemoved;
	}

	public bool IsHoldingUmbrella()
	{
		return _isHoldingUmbrella;
	}

	public void Update()
	{
		if (_hasUmbrella)
		{
			if (RainHelper.AreRainDropsFalling && !_isHoldingUmbrella)
			{
				StartHoldingUmbrella(instant: false);
			}
			else if (!RainHelper.AreRainDropsFalling && _isHoldingUmbrella)
			{
				StopHoldingUmbrella();
			}
		}
	}

	private void StartHoldingUmbrella(bool instant)
	{
		if (_currentUmbrellaCoroutine != null)
		{
			_baseHuman.StopCoroutine(_currentUmbrellaCoroutine);
			_currentUmbrellaCoroutine = null;
		}
		_currentUmbrellaCoroutine = _baseHuman.StartCoroutine(AddUmbrellaCoroutine(instant ? 0f : UnityEngine.Random.Range(3f, 8f)));
	}

	private void StopHoldingUmbrella()
	{
		if (_currentUmbrellaCoroutine != null)
		{
			_baseHuman.StopCoroutine(_currentUmbrellaCoroutine);
			_currentUmbrellaCoroutine = null;
		}
		_currentUmbrellaCoroutine = _baseHuman.StartCoroutine(RemoveUmbrellaCoroutine(UnityEngine.Random.Range(3f, 8f)));
	}

	public void ForceRemoveUmbrella()
	{
		if (_currentUmbrellaCoroutine != null)
		{
			_baseHuman.StopCoroutine(_currentUmbrellaCoroutine);
			_currentUmbrellaCoroutine = null;
		}
		if (_isHoldingUmbrella)
		{
			_baseHuman.StopHoldingAnItem();
			_baseHuman.RemoveHandObject();
			_isHoldingUmbrella = false;
		}
	}

	public void OnEnable()
	{
		if (_currentUmbrellaCoroutine != null)
		{
			_baseHuman.StopCoroutine(_currentUmbrellaCoroutine);
			_currentUmbrellaCoroutine = null;
		}
		if (RainHelper.AreRainDropsFalling && _hasUmbrella)
		{
			StartHoldingUmbrella(instant: true);
		}
	}

	private IEnumerator AddUmbrellaCoroutine(float secondsToReact)
	{
		_isHoldingUmbrella = true;
		yield return new WaitForSeconds(secondsToReact);
		_onUmbrellaAdded?.Invoke();
		_baseHuman.RemoveHandObject();
		_baseHuman.HoldAnItem();
		_baseHuman.AddHandObject("Umbrella");
	}

	private IEnumerator RemoveUmbrellaCoroutine(float secondsToReact)
	{
		_isHoldingUmbrella = false;
		yield return new WaitForSeconds(secondsToReact);
		_baseHuman.StopHoldingAnItem();
		_baseHuman.RemoveHandObject();
		_onUmbrellaRemoved?.Invoke();
	}
}
