using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Components;

public class StateButtonBehaviour : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	[ReadOnly]
	public int state;

	[SerializeField]
	private List<GameObject> stateObjects;

	private Action _onStateChanged;

	public void OnPointerClick(PointerEventData eventData)
	{
		state = (state + 1) % stateObjects.Count;
		for (int i = 0; i < stateObjects.Count; i++)
		{
			if (stateObjects[i] != null)
			{
				stateObjects[i]?.SetActive(i == state);
			}
		}
		_onStateChanged?.Invoke();
	}

	public void SetState(int newState, bool updateState)
	{
		state = newState;
		for (int i = 0; i < stateObjects.Count; i++)
		{
			if (stateObjects[i] != null)
			{
				stateObjects[i]?.SetActive(i == newState);
			}
		}
		if (updateState)
		{
			_onStateChanged?.Invoke();
		}
	}

	protected void SetUp(Action onStateChanged)
	{
		_onStateChanged = onStateChanged;
	}
}
