using System;
using System.Collections;
using BigAmbitions.PlacementSystem;
using IngameDebugConsole;
using NaughtyAttributes;
using UI;
using UnityEngine;

namespace Controllers;

public class StateItemController : ItemController
{
	[Header("State settings")]
	[SerializeField]
	private GameObject[] stateObjects;

	[SerializeField]
	private bool hasSfx;

	[ShowIf("hasSfx")]
	[SerializeField]
	private AudioSource audioSource;

	[ShowIf("hasSfx")]
	[SerializeField]
	private AudioClip[] stateChangeSfx;

	[ShowIf("hasSfx")]
	[SerializeField]
	private AudioClip[] sfxDuringState;

	private Coroutine _sfxCoroutine;

	public override void Start()
	{
		base.Start();
		GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(OnPause));
		UpdateMixerGroup();
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPause));
			base.OnDestroy();
		}
	}

	public override void Hide()
	{
		base.Hide();
		DisableStates();
	}

	public override void Show()
	{
		base.Show();
		UpdateState(base.ItemInstance.stateIndex, playStateChangeSfx: false);
	}

	private void OnPause(bool pause)
	{
		if (pause)
		{
			audioSource?.Pause();
		}
		else
		{
			audioSource?.UnPause();
		}
	}

	private void OnEnable()
	{
		if (initialized)
		{
			UpdateState(base.ItemInstance.stateIndex, playStateChangeSfx: false);
			return;
		}
		onItemInitialized.AddListener(delegate
		{
			UpdateState(base.ItemInstance.stateIndex, playStateChangeSfx: false);
		});
	}

	private void DisableStates()
	{
		for (int i = 0; i < stateObjects.Length; i++)
		{
			if (stateObjects[i] != null)
			{
				stateObjects[i].SetActive(value: false);
			}
		}
		if (hasSfx)
		{
			if (_sfxCoroutine != null)
			{
				StopCoroutine(_sfxCoroutine);
			}
			audioSource.Stop();
		}
	}

	private void UpdateState(int state, bool playStateChangeSfx = true)
	{
		if (stateObjects.Length == 0 || state < 0 || state >= stateObjects.Length)
		{
			return;
		}
		base.ItemInstance.stateIndex = state;
		for (int i = 0; i < stateObjects.Length; i++)
		{
			if (stateObjects[i] != null)
			{
				stateObjects[i].SetActive(i == state);
			}
		}
		if (!hasSfx)
		{
			return;
		}
		if (_sfxCoroutine != null)
		{
			StopCoroutine(_sfxCoroutine);
		}
		float delay = 0f;
		if (playStateChangeSfx && stateChangeSfx.Length > state && stateChangeSfx[state] != null)
		{
			UpdateMixerGroup();
			audioSource.loop = false;
			audioSource.PlayOneShot(stateChangeSfx[state]);
			delay = stateChangeSfx[state].length;
		}
		if (sfxDuringState.Length > state && sfxDuringState[state] != null)
		{
			audioSource.loop = true;
			audioSource.clip = sfxDuringState[state];
			_sfxCoroutine = StartCoroutine(RunAfterSecondsDelay(delay, delegate
			{
				UpdateMixerGroup();
				audioSource.Play();
				if (InstanceBehavior<UIs>.Instance != null && InstanceBehavior<UIs>.Instance.gameSpeed.Paused)
				{
					audioSource.Pause();
				}
			}));
		}
		else
		{
			audioSource.Stop();
		}
	}

	private static IEnumerator RunAfterSecondsDelay(float delay, Action action)
	{
		yield return new WaitForSeconds(delay);
		action();
	}

	public void SetNextState()
	{
		int num = base.ItemInstance.stateIndex + 1;
		if (num >= stateObjects.Length)
		{
			num = 0;
		}
		UpdateState(num);
	}

	private void UpdateMixerGroup()
	{
		if (hasSfx)
		{
			audioSource.outputAudioMixerGroup = ((BuildingManager.IsInsideBuilding && !InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse()) ? InstanceBehavior<GlobalReferences>.Instance.indoorMixerGroup : InstanceBehavior<GlobalReferences>.Instance.foleyMixerGroup);
		}
	}

	[ConsoleMethod("SetItemState", "Set the state of the item", new string[] { })]
	public static void SetTextOnItem(int state)
	{
		IPlaceableItem currentPlaceableItemBeingPlaced = PlacementSystem.CurrentPlaceableItemBeingPlaced;
		if (currentPlaceableItemBeingPlaced == null || !(currentPlaceableItemBeingPlaced is StateItemController stateItemController))
		{
			Debug.LogWarning("No item found to set state on");
			return;
		}
		stateItemController.ItemInstance.stateIndex = state;
		stateItemController.UpdateState(state);
		Debug.Log($"Set state on item: {state}");
	}
}
