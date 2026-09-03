using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Components;

[RequireComponent(typeof(Button))]
public class HoldRepeatButton : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IPointerExitHandler
{
	public Button button;

	[Header("Hold Settings")]
	[SerializeField]
	[MinMaxSlider(0.05f, 0.5f)]
	private Vector2 holdClickIntervals = new Vector2(0.05f, 0.5f);

	[SerializeField]
	[Range(2f, 15f)]
	private int cyclesToReachMaxSpeed = 7;

	[SerializeField]
	private AnimationCurve speedTransitionCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

	[Header("Events")]
	[SerializeField]
	private UnityEvent onRepeatAction;

	private Coroutine _holdCoroutine;

	private bool _isHolding;

	private WaitForSecondsRealtime _waitForSecondsRealtime;

	private int _currentCycle;

	private void OnDisable()
	{
		StopHolding();
	}

	private void OnDestroy()
	{
		StopHolding();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (button.interactable)
		{
			StartHolding();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		StopHolding();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		StopHolding();
	}

	private void StartHolding()
	{
		StopHolding();
		_isHolding = true;
		_currentCycle = 0;
		onRepeatAction?.Invoke();
		_holdCoroutine = StartCoroutine(HoldCoroutine());
	}

	private void StopHolding()
	{
		_isHolding = false;
		if (_holdCoroutine != null)
		{
			StopCoroutine(_holdCoroutine);
			_holdCoroutine = null;
		}
	}

	private IEnumerator HoldCoroutine()
	{
		_waitForSecondsRealtime = new WaitForSecondsRealtime(holdClickIntervals.y);
		yield return _waitForSecondsRealtime;
		while (_isHolding && button.interactable)
		{
			_currentCycle++;
			float time = Mathf.Clamp01((float)_currentCycle / (float)cyclesToReachMaxSpeed);
			float t = 1f - speedTransitionCurve.Evaluate(time);
			float waitTime = Mathf.Lerp(holdClickIntervals.x, holdClickIntervals.y, t);
			onRepeatAction?.Invoke();
			_waitForSecondsRealtime.waitTime = waitTime;
			yield return _waitForSecondsRealtime;
		}
	}
}
