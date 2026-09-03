using System;
using System.Collections;
using System.Collections.Generic;
using BigAmbitions.PlacementSystem;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public class FenceDoor : MonoBehaviour
{
	private enum State
	{
		Closed,
		Opening,
		Open,
		Closing
	}

	private static readonly WaitForFixedUpdate WaitFixedUpdate = new WaitForFixedUpdate();

	public Transform[] doors;

	public bool acceptsVehicles;

	public FenceDoorMode mode;

	[ShowIf("IsSwingMode")]
	public bool openBothSides;

	[ShowIf("IsSwingMode")]
	public float openAngle = 90f;

	[ShowIf("IsSlideMode")]
	public float slideDistance = 3f;

	[ShowIf("IsSlideMode")]
	public Vector3 slideDirection = Vector3.right;

	public float openDuration = 1f;

	[HideIf("ShouldHideInvertRotation")]
	public bool invertRotation;

	public ItemController itemController;

	[BoxGroup("SFX")]
	[SerializeField]
	private AudioSource sfxSource;

	[BoxGroup("SFX")]
	[SerializeField]
	private AudioClip openSfx;

	[BoxGroup("SFX")]
	[SerializeField]
	private AudioClip closeSfx;

	private readonly Dictionary<Collider, int> _collidersInZone = new Dictionary<Collider, int>();

	private readonly List<Collider> _collidersToRemove = new List<Collider>(4);

	private Vector3[] _closedPositions;

	private Vector3[] _closedRotations;

	private Vector3[] _openPositions;

	private Vector3[] _openRotations;

	private bool _pendingInitialOverlapCheck;

	private Coroutine _refreshCoroutine;

	private State _state;

	private void Awake()
	{
		SetItemControllerReference();
		if (IsForSale())
		{
			return;
		}
		_closedRotations = new Vector3[doors.Length];
		_openRotations = new Vector3[doors.Length];
		_closedPositions = new Vector3[doors.Length];
		_openPositions = new Vector3[doors.Length];
		Vector3 vector = slideDirection.normalized * (invertRotation ? (0f - slideDistance) : slideDistance);
		Vector3 worldTargetDir = (invertRotation ? base.transform.forward : (-base.transform.forward));
		for (int i = 0; i < doors.Length; i++)
		{
			_closedRotations[i] = doors[i].localEulerAngles;
			_closedPositions[i] = doors[i].localPosition;
			if (mode == FenceDoorMode.Slide)
			{
				_openPositions[i] = _closedPositions[i] + vector;
			}
			else if (!openBothSides)
			{
				_openRotations[i] = ComputeOpenRotation(i, worldTargetDir);
			}
		}
	}

	public void Reset()
	{
		_collidersInZone.Clear();
		_state = State.Closed;
		if (_closedRotations == null)
		{
			return;
		}
		for (int i = 0; i < doors.Length; i++)
		{
			doors[i].DOKill();
			if (mode == FenceDoorMode.Slide)
			{
				doors[i].localPosition = _closedPositions[i];
			}
			else
			{
				doors[i].localRotation = Quaternion.Euler(_closedRotations[i]);
			}
		}
	}

	private void OnEnable()
	{
		if (!IsForSale())
		{
			SubscribeToEvents();
			InvokeRepeating("Tick", 1f, 1f);
		}
	}

	private void OnDisable()
	{
		if (!IsForSale())
		{
			UnsubscribeFromEvents();
			CancelInvoke("Tick");
			_pendingInitialOverlapCheck = false;
			if (_refreshCoroutine != null)
			{
				StopCoroutine(_refreshCoroutine);
				_refreshCoroutine = null;
			}
			Reset();
		}
	}

	public bool IsForSale()
	{
		if (itemController != null)
		{
			return itemController.playerItemPurchaserSettings.enabled;
		}
		return false;
	}

	protected virtual bool IsLocked()
	{
		return false;
	}

	protected virtual void ShowLockedMessage()
	{
	}

	protected virtual void SetItemControllerReference()
	{
		itemController = GetComponent<ItemController>();
	}

	protected virtual void SubscribeToEvents()
	{
		PlacementSystem.onPlacementModeStart = (Action)Delegate.Combine(PlacementSystem.onPlacementModeStart, new Action(OnPlacementStarted));
		PlacementSystem.onItemPlaced = (Action)Delegate.Combine(PlacementSystem.onItemPlaced, new Action(OnItemPlaced));
	}

	protected virtual void UnsubscribeFromEvents()
	{
		PlacementSystem.onPlacementModeStart = (Action)Delegate.Remove(PlacementSystem.onPlacementModeStart, new Action(OnPlacementStarted));
		PlacementSystem.onItemPlaced = (Action)Delegate.Remove(PlacementSystem.onItemPlaced, new Action(OnItemPlaced));
	}

	private void CheckInitialOverlaps()
	{
		if (_refreshCoroutine != null)
		{
			StopCoroutine(_refreshCoroutine);
		}
		_refreshCoroutine = StartCoroutine(RefreshTriggerDetection());
	}

	public void HandleTriggerEnter(Collider other, bool ignoreLockedCondition)
	{
		if (!ignoreLockedCondition && IsLocked())
		{
			ShowLockedMessage();
			return;
		}
		if (mode == FenceDoorMode.Swing && openBothSides && _collidersInZone.Count == 0)
		{
			State state = _state;
			if (state == State.Closed || state == State.Closing)
			{
				SetOpenDirectionFrom(other);
			}
		}
		_collidersInZone.TryGetValue(other, out var value);
		_collidersInZone[other] = value + 1;
		Tick(ignoreLockedCondition);
	}

	public void HandleTriggerExit(Collider other)
	{
		if (_collidersInZone.TryGetValue(other, out var value))
		{
			if (value <= 1)
			{
				_collidersInZone.Remove(other);
			}
			else
			{
				_collidersInZone[other] = value - 1;
			}
			Tick();
		}
	}

	private void OnPlacementStarted()
	{
		if (PlacementSystem.CurrentPlaceableItemBeingPlaced is ItemController itemController && itemController == this.itemController)
		{
			_pendingInitialOverlapCheck = true;
			Reset();
		}
	}

	private void OnItemPlaced()
	{
		if (_pendingInitialOverlapCheck)
		{
			_pendingInitialOverlapCheck = false;
			CheckInitialOverlaps();
		}
	}

	private void Tick()
	{
		Tick(ignoreLockedCondition: false);
	}

	private void Tick(bool ignoreLockedCondition)
	{
		_collidersToRemove.Clear();
		foreach (Collider key in _collidersInZone.Keys)
		{
			if (!key)
			{
				_collidersToRemove.Add(key);
			}
		}
		foreach (Collider item in _collidersToRemove)
		{
			_collidersInZone.Remove(item);
		}
		bool flag = _collidersInZone.Count > 0;
		switch (_state)
		{
		case State.Closed:
			if (flag && (ignoreLockedCondition || !IsLocked()))
			{
				StartOpening();
			}
			break;
		case State.Open:
			if (!flag)
			{
				StartClosing();
			}
			break;
		case State.Closing:
			if (flag && (ignoreLockedCondition || !IsLocked()))
			{
				StartOpening();
			}
			break;
		case State.Opening:
			break;
		}
	}

	private void StartOpening()
	{
		_state = State.Opening;
		if ((bool)sfxSource && (bool)openSfx)
		{
			sfxSource.PlayOneShot(openSfx);
		}
		for (int i = 0; i < doors.Length; i++)
		{
			doors[i].DOKill();
			Tween t = ((mode == FenceDoorMode.Slide) ? ((Tweener)doors[i].DOLocalMove(_openPositions[i], openDuration)) : ((Tweener)doors[i].DOLocalRotate(_openRotations[i], openDuration)));
			t.SetLink(doors[i].gameObject);
			if (i == 0)
			{
				t.OnComplete(delegate
				{
					_state = State.Open;
				});
			}
		}
	}

	private void StartClosing()
	{
		_state = State.Closing;
		if ((bool)sfxSource && (bool)closeSfx)
		{
			sfxSource.PlayOneShot(closeSfx);
		}
		for (int i = 0; i < doors.Length; i++)
		{
			doors[i].DOKill();
			Tween t = ((mode == FenceDoorMode.Slide) ? ((Tweener)doors[i].DOLocalMove(_closedPositions[i], openDuration)) : ((Tweener)doors[i].DOLocalRotate(_closedRotations[i], openDuration)));
			t.SetLink(doors[i].gameObject);
			if (i == 0)
			{
				t.OnComplete(delegate
				{
					_state = State.Closed;
				});
			}
		}
	}

	private IEnumerator RefreshTriggerDetection()
	{
		List<Collider> triggerColliders = new List<Collider>();
		FenceDoorTrigger[] componentsInChildren = GetComponentsInChildren<FenceDoorTrigger>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Collider component = componentsInChildren[i].GetComponent<Collider>();
			if (component != null)
			{
				triggerColliders.Add(component);
			}
		}
		foreach (Collider item in triggerColliders)
		{
			item.enabled = false;
		}
		yield return WaitFixedUpdate;
		foreach (Collider item2 in triggerColliders)
		{
			item2.enabled = true;
		}
		_refreshCoroutine = null;
	}

	private void SetOpenDirectionFrom(Collider triggerer)
	{
		Vector3 worldTargetDir = ((base.transform.InverseTransformPoint(triggerer.transform.position).z >= 0f) ? (-base.transform.forward) : base.transform.forward);
		worldTargetDir.y = 0f;
		for (int i = 0; i < doors.Length; i++)
		{
			_openRotations[i] = ComputeOpenRotation(i, worldTargetDir);
		}
	}

	private Vector3 ComputeOpenRotation(int doorIndex, Vector3 worldTargetDir)
	{
		float y = ComputeOpenAngle(doors[doorIndex], worldTargetDir);
		return _closedRotations[doorIndex] + new Vector3(0f, y, 0f);
	}

	private float ComputeOpenAngle(Transform door, Vector3 worldTargetDir)
	{
		Vector3 vector = Vector3.Cross(GetFreeEndDirection(door), worldTargetDir.normalized);
		return openAngle * ((vector.y >= 0f) ? 1f : (-1f));
	}

	private static Vector3 GetFreeEndDirection(Transform door)
	{
		Collider doorCollider = GetDoorCollider(door);
		if (doorCollider == null)
		{
			return door.right;
		}
		Vector3 vector = doorCollider.bounds.center - door.position;
		vector.y = 0f;
		if (!(vector.sqrMagnitude > 0.001f))
		{
			return door.right;
		}
		return vector.normalized;
	}

	private static Collider GetDoorCollider(Transform door)
	{
		Collider[] components = door.GetComponents<Collider>();
		foreach (Collider collider in components)
		{
			if (!collider.isTrigger)
			{
				return collider;
			}
		}
		return null;
	}

	private bool IsSwingMode()
	{
		return mode == FenceDoorMode.Swing;
	}

	private bool IsSlideMode()
	{
		return mode == FenceDoorMode.Slide;
	}

	private bool ShouldHideInvertRotation()
	{
		if (mode == FenceDoorMode.Swing)
		{
			return openBothSides;
		}
		return false;
	}
}
