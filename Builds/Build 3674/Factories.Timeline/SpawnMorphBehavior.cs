using System;
using Helpers;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Playables;

namespace Factories.Timeline;

[Serializable]
public class SpawnMorphBehavior : PlayableBehaviour
{
	[Header("Spawn options")]
	public ExposedReference<Transform> startPoint;

	public ExposedReference<Transform> endPoint;

	public ExposedReference<Transform> parent;

	public ExposedReference<Transform> cornerPoint;

	public ExposedReference<VisibilityProbe> visibilityProbe;

	[Header("Swap")]
	public bool doSwap = true;

	[Range(0f, 1f)]
	public float swapAt = 0.5f;

	[Header("Pausing")]
	public bool stopAtSwap;

	public int stopAtSwapMs;

	public int swapBeforePauseEndMs;

	public ExposedReference<Transform> pauseAttachTarget;

	[Header("Visibility")]
	public bool hideBetween;

	[MinMaxSlider(0f, 1f)]
	public Vector2 hideRange = new Vector2(0.4f, 0.6f);

	private PlayableDirector _director;

	private double _duration;

	private Transform _start;

	private Transform _end;

	private Transform _parent;

	private Transform _corner;

	private Transform _pauseAttach;

	private GameObject _startInstance;

	private GameObject _endInstance;

	private VisibilityProbe _visibilityProbe;

	private SpawnMorphPlayerData _data;

	private string _currentStartItem;

	private string _currentEndItem;

	private string _effectiveStartItem;

	private string _effectiveEndItem;

	private bool _itemsDirty = true;

	public override void OnGraphStart(Playable playable)
	{
		_director = playable.GetGraph().GetResolver() as PlayableDirector;
		_start = startPoint.Resolve(_director);
		_end = endPoint.Resolve(_director);
		_parent = parent.Resolve(_director);
		_corner = cornerPoint.Resolve(_director);
		_pauseAttach = pauseAttachTarget.Resolve(_director);
		_visibilityProbe = visibilityProbe.Resolve(_director);
		_duration = 0.0;
		_data = null;
		_itemsDirty = true;
		_currentStartItem = null;
		_currentEndItem = null;
		_effectiveStartItem = null;
		_effectiveEndItem = null;
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if ((object)_start == null)
		{
			_start = startPoint.Resolve(_director);
		}
		if ((object)_end == null)
		{
			_end = endPoint.Resolve(_director);
		}
		if ((object)_parent == null)
		{
			_parent = parent.Resolve(_director);
		}
		if ((object)_corner == null)
		{
			_corner = cornerPoint.Resolve(_director);
		}
		if ((object)_pauseAttach == null)
		{
			_pauseAttach = pauseAttachTarget.Resolve(_director);
		}
		if ((object)_visibilityProbe == null)
		{
			_visibilityProbe = visibilityProbe.Resolve(_director);
		}
		_duration = playable.GetDuration();
		if (_duration <= 0.0001)
		{
			_duration = 0.0001;
		}
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (_data == null && playerData is SpawnMorphPlayerData data)
		{
			_data = data;
			SpawnMorphPlayerData data2 = _data;
			data2.onItemsChanged = (Action)Delegate.Combine(data2.onItemsChanged, new Action(OnItemsChanged));
			_itemsDirty = true;
		}
		if (_itemsDirty)
		{
			UpdateEffectiveItems();
			EnsureInstances(_effectiveStartItem, _effectiveEndItem);
			_itemsDirty = false;
		}
		if (string.IsNullOrEmpty(_effectiveStartItem))
		{
			return;
		}
		ResolveReferences();
		if (_visibilityProbe != null && !_visibilityProbe.IsVisible)
		{
			return;
		}
		float safeDuration = GetSafeDuration(playable);
		float normalizedTime = GetNormalizedTime(playable, safeDuration);
		Vector3 vector = (_start ? _start.position : Vector3.zero);
		Vector3 vector2 = (_end ? _end.position : vector);
		Quaternion startRot = (_start ? _start.rotation : Quaternion.identity);
		if (InHideRange(normalizedTime))
		{
			if (_startInstance != null)
			{
				_startInstance.SetActive(value: false);
			}
			if (_endInstance != null)
			{
				_endInstance.SetActive(value: false);
			}
			return;
		}
		ComputeTimings(safeDuration, out var swap, out var startEnd, out var pauseStart, out var pauseEnd, out var endDuration, out var switchTime);
		Vector3 swapPos = (_corner ? _corner.position : Vector3.Lerp(vector, vector2, swap));
		bool flag = normalizedTime >= pauseStart && normalizedTime < pauseEnd;
		bool num = normalizedTime < startEnd;
		bool beforeSwitch = normalizedTime < switchTime;
		if (num)
		{
			UpdateBeforePause(vector, swapPos, startRot, normalizedTime, startEnd);
			return;
		}
		if (flag)
		{
			UpdatePause(beforeSwitch, swapPos, startRot);
			return;
		}
		bool useStartInstance = !doSwap || _endInstance == null;
		UpdateAfterPause(swapPos, vector2, startRot, normalizedTime, pauseEnd, endDuration, useStartInstance);
	}

	private float GetSafeDuration(Playable playable)
	{
		float num = (float)playable.GetDuration();
		if (num <= 0.0001f)
		{
			num = 0.0001f;
		}
		return num;
	}

	private float GetNormalizedTime(Playable playable, float duration)
	{
		return Mathf.Clamp01((float)(playable.GetTime() / (double)duration));
	}

	private void ResolveReferences()
	{
		if ((object)_start == null)
		{
			_start = startPoint.Resolve(_director);
		}
		if ((object)_end == null)
		{
			_end = endPoint.Resolve(_director);
		}
		if ((object)_parent == null)
		{
			_parent = parent.Resolve(_director);
		}
		if ((object)_pauseAttach == null)
		{
			_pauseAttach = pauseAttachTarget.Resolve(_director);
		}
		if ((object)_corner == null)
		{
			_corner = cornerPoint.Resolve(_director);
		}
		if ((object)_visibilityProbe == null)
		{
			_visibilityProbe = visibilityProbe.Resolve(_director);
		}
	}

	private void EnsureInstances(string effectiveStartItem, string effectiveEndItem)
	{
		if (effectiveStartItem != _currentStartItem)
		{
			if (_startInstance != null)
			{
				DestroyItem(_startInstance);
				_startInstance = null;
			}
			_currentStartItem = effectiveStartItem;
		}
		if (effectiveEndItem != _currentEndItem)
		{
			if (_endInstance != null)
			{
				DestroyItem(_endInstance);
				_endInstance = null;
			}
			_currentEndItem = effectiveEndItem;
		}
		if (_startInstance == null && !string.IsNullOrEmpty(effectiveStartItem))
		{
			_startInstance = PrefabHelper.CreateVisualForItemNamePooled(effectiveStartItem);
			if (_parent != null)
			{
				_startInstance.transform.SetParent(_parent, worldPositionStays: true);
			}
			_startInstance.SetActive(value: false);
		}
		if (doSwap && !string.IsNullOrEmpty(effectiveEndItem) && _endInstance == null)
		{
			_endInstance = PrefabHelper.CreateVisualForItemNamePooled(effectiveEndItem);
			if (_parent != null)
			{
				_endInstance.transform.SetParent(_parent, worldPositionStays: true);
			}
			_endInstance.SetActive(value: false);
		}
	}

	private bool InHideRange(float tNorm)
	{
		if (!hideBetween)
		{
			return false;
		}
		float a = Mathf.Clamp01(hideRange.x);
		float b = Mathf.Clamp01(hideRange.y);
		float num = Mathf.Min(a, b);
		float num2 = Mathf.Max(a, b);
		if (tNorm >= num)
		{
			return tNorm < num2;
		}
		return false;
	}

	private void ComputeTimings(float duration, out float swap, out float startEnd, out float pauseStart, out float pauseEnd, out float endDuration, out float switchTime)
	{
		swap = Mathf.Clamp01(swapAt);
		float num = ((stopAtSwap && stopAtSwapMs > 0) ? Mathf.Clamp01((float)stopAtSwapMs / 1000f / duration) : 0f);
		float num2 = Mathf.Clamp01(1f - num);
		startEnd = swap * num2;
		pauseStart = startEnd;
		pauseEnd = Mathf.Clamp01(pauseStart + num);
		endDuration = Mathf.Max((1f - swap) * num2, 1E-06f);
		float num3 = 0f;
		if (stopAtSwap && num > 0f && swapBeforePauseEndMs > 0)
		{
			num3 = Mathf.Clamp((float)swapBeforePauseEndMs / 1000f / duration, 0f, num);
		}
		switchTime = Mathf.Max(pauseStart, pauseEnd - num3);
	}

	private void UpdateBeforePause(Vector3 startPos, Vector3 swapPos, Quaternion startRot, float tNorm, float startEnd)
	{
		_startInstance.SetActive(value: true);
		if (_endInstance != null)
		{
			_endInstance.SetActive(value: false);
		}
		_startInstance.transform.rotation = startRot;
		float t = Mathf.Clamp01(tNorm / Mathf.Max(startEnd, 1E-06f));
		_startInstance.transform.position = Vector3.Lerp(startPos, swapPos, t);
	}

	private void UpdatePause(bool beforeSwitch, Vector3 swapPos, Quaternion startRot)
	{
		if (beforeSwitch || !doSwap || _endInstance == null)
		{
			if (_startInstance != null)
			{
				_startInstance.SetActive(value: true);
				if (_pauseAttach != null)
				{
					_startInstance.transform.position = _pauseAttach.position;
					_startInstance.transform.rotation = _pauseAttach.rotation;
				}
				else
				{
					_startInstance.transform.position = swapPos;
					_startInstance.transform.rotation = startRot;
				}
			}
			if (_endInstance != null)
			{
				_endInstance.SetActive(value: false);
			}
			return;
		}
		if (_startInstance != null)
		{
			_startInstance.SetActive(value: false);
		}
		if (!(_endInstance == null))
		{
			_endInstance.SetActive(value: true);
			if (_pauseAttach != null)
			{
				_endInstance.transform.position = _pauseAttach.position;
				_endInstance.transform.rotation = _pauseAttach.rotation;
			}
			else
			{
				_endInstance.transform.position = swapPos;
				_endInstance.transform.rotation = startRot;
			}
		}
	}

	private void UpdateAfterPause(Vector3 swapPos, Vector3 endPos, Quaternion startRot, float tNorm, float pauseEnd, float endDuration, bool useStartInstance)
	{
		float t = Mathf.Clamp01((tNorm - pauseEnd) / endDuration);
		if (useStartInstance)
		{
			if (_endInstance != null)
			{
				_endInstance.SetActive(value: false);
			}
			if (!(_startInstance == null))
			{
				_startInstance.SetActive(value: true);
				_startInstance.transform.rotation = startRot;
				_startInstance.transform.position = Vector3.Lerp(swapPos, endPos, t);
			}
		}
		else
		{
			if (_startInstance != null)
			{
				_startInstance.SetActive(value: false);
			}
			if (!(_endInstance == null))
			{
				_endInstance.SetActive(value: true);
				_endInstance.transform.rotation = startRot;
				_endInstance.transform.position = Vector3.Lerp(swapPos, endPos, t);
			}
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (_startInstance != null)
		{
			_startInstance.SetActive(value: false);
		}
		if (_endInstance != null)
		{
			_endInstance.SetActive(value: false);
		}
	}

	public override void OnGraphStop(Playable playable)
	{
		if (_startInstance != null)
		{
			DestroyItem(_startInstance);
			_startInstance = null;
		}
		if (_endInstance != null)
		{
			DestroyItem(_endInstance);
			_endInstance = null;
		}
		if (_data != null)
		{
			SpawnMorphPlayerData data = _data;
			data.onItemsChanged = (Action)Delegate.Remove(data.onItemsChanged, new Action(OnItemsChanged));
			_data = null;
		}
		_currentStartItem = null;
		_currentEndItem = null;
		_effectiveStartItem = null;
		_effectiveEndItem = null;
		_start = null;
		_end = null;
		_corner = null;
		_parent = null;
		_director = null;
	}

	private void UpdateEffectiveItems()
	{
		if (_data != null)
		{
			_effectiveStartItem = _data.startItem;
			_effectiveEndItem = _data.endItem;
		}
		else
		{
			_effectiveStartItem = null;
			_effectiveEndItem = null;
		}
	}

	private void OnItemsChanged()
	{
		_itemsDirty = true;
	}

	private void DestroyItem(GameObject item)
	{
		UnityEngine.Object.Destroy(item);
	}
}
