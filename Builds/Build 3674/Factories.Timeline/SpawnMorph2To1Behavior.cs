using System;
using Helpers;
using UnityEngine;
using UnityEngine.Playables;

namespace Factories.Timeline;

[Serializable]
public class SpawnMorph2To1Behavior : PlayableBehaviour
{
	[Header("Spawn options")]
	public ExposedReference<Transform> startPointA;

	public ExposedReference<Transform> startPointB;

	public ExposedReference<Transform> endPoint;

	public ExposedReference<Transform> parent;

	public ExposedReference<Transform> cornerPoint;

	[Range(0f, 1f)]
	public float swapAt = 0.5f;

	public int cornerAtMs;

	public int startBOffsetMs;

	[Header("Pausing")]
	public bool stopAtSwap;

	public int stopAtSwapMs;

	private PlayableDirector _director;

	private Transform _startA;

	private Transform _startB;

	private Transform _end;

	private Transform _parent;

	private Transform _corner;

	private GameObject _startAInstance;

	private GameObject _startBInstance;

	private GameObject _endInstance;

	private SpawnMorphPlayerData _data;

	private string _currentStartItemA;

	private string _currentStartItemB;

	private string _currentEndItem;

	private string _effectiveStartItemA;

	private string _effectiveStartItemB;

	private string _effectiveEndItem;

	private bool _itemsDirty = true;

	public override void OnGraphStart(Playable playable)
	{
		_director = playable.GetGraph().GetResolver() as PlayableDirector;
		_startA = startPointA.Resolve(_director);
		_startB = startPointB.Resolve(_director);
		_end = endPoint.Resolve(_director);
		_parent = parent.Resolve(_director);
		_corner = cornerPoint.Resolve(_director);
		_data = null;
		_itemsDirty = true;
		_currentStartItemA = null;
		_currentStartItemB = null;
		_currentEndItem = null;
		_effectiveStartItemA = null;
		_effectiveStartItemB = null;
		_effectiveEndItem = null;
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		ResolveReferences();
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		ResolveReferences();
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
			Quaternion rotA = (_startA ? _startA.rotation : Quaternion.identity);
			Quaternion rotB = (_startB ? _startB.rotation : Quaternion.identity);
			Quaternion rotCorner = (_corner ? _corner.rotation : Quaternion.identity);
			EnsureInstances(_effectiveStartItemA, _effectiveStartItemB, _effectiveEndItem, rotA, rotB, rotCorner);
			_itemsDirty = false;
		}
		if (string.IsNullOrEmpty(_effectiveEndItem))
		{
			return;
		}
		float safeDuration = GetSafeDuration(playable);
		float normalizedTime = GetNormalizedTime(playable, safeDuration);
		Vector3 s = (_startA ? _startA.position : Vector3.zero);
		Vector3 s2 = (_startB ? _startB.position : Vector3.zero);
		Vector3 vector = (_corner ? _corner.position : (_end ? _end.position : Vector3.zero));
		Vector3 vector2 = (_end ? _end.position : vector);
		Quaternion rotation = (_end ? _end.rotation : Quaternion.identity);
		float num = Mathf.Clamp01(swapAt);
		float num2 = ((stopAtSwap && stopAtSwapMs > 0) ? Mathf.Clamp01((float)stopAtSwapMs / 1000f / safeDuration) : 0f);
		float num3 = Mathf.Clamp01((cornerAtMs > 0) ? ((float)cornerAtMs / 1000f / safeDuration) : 0f);
		float num4 = ((startBOffsetMs > 0) ? Mathf.Clamp01((float)startBOffsetMs / 1000f / safeDuration) : 0f);
		float t = Mathf.Clamp01(normalizedTime - num4);
		Vector3 position = PathPos(s, vector, vector2, t, num3);
		Vector3 position2 = PathPos(s2, vector, vector2, normalizedTime, num3);
		Vector3 vector3 = PathPos(vector, vector, vector2, (num < num3) ? num3 : num, num3);
		float num5 = num;
		float num6 = Mathf.Clamp01(num5 + num2);
		bool num7 = normalizedTime < num;
		bool flag = normalizedTime >= num5 && normalizedTime < num6;
		if (num7)
		{
			if (_startAInstance != null)
			{
				if (normalizedTime >= num4)
				{
					_startAInstance.SetActive(value: true);
					_startAInstance.transform.position = position;
				}
				else
				{
					_startAInstance.SetActive(value: false);
				}
			}
			if (_startBInstance != null)
			{
				_startBInstance.SetActive(value: true);
				_startBInstance.transform.position = position2;
			}
			if (_endInstance != null)
			{
				_endInstance.SetActive(value: false);
			}
		}
		else if (flag)
		{
			if (_startAInstance != null)
			{
				_startAInstance.SetActive(value: false);
			}
			if (_startBInstance != null)
			{
				_startBInstance.SetActive(value: false);
			}
			_endInstance.SetActive(value: true);
			_endInstance.transform.rotation = rotation;
			_endInstance.transform.position = vector3;
		}
		else
		{
			if (_startAInstance != null)
			{
				_startAInstance.SetActive(value: false);
			}
			if (_startBInstance != null)
			{
				_startBInstance.SetActive(value: false);
			}
			_endInstance.SetActive(value: true);
			_endInstance.transform.rotation = rotation;
			float num8 = Mathf.Max(1f - num6, 1E-06f);
			float t2 = Mathf.Clamp01((normalizedTime - num6) / num8);
			_endInstance.transform.position = Vector3.Lerp(vector3, vector2, t2);
		}
		static Vector3 PathPos(Vector3 a, Vector3 c, Vector3 e, float num9, float cn)
		{
			if (num9 <= cn)
			{
				return Vector3.Lerp(a, c, num9 / Mathf.Max(cn, 1E-06f));
			}
			float value = (num9 - cn) / Mathf.Max(1f - cn, 1E-06f);
			return Vector3.Lerp(c, e, Mathf.Clamp01(value));
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (_startAInstance != null)
		{
			_startAInstance.SetActive(value: false);
		}
		if (_startBInstance != null)
		{
			_startBInstance.SetActive(value: false);
		}
		if (_endInstance != null)
		{
			_endInstance.SetActive(value: false);
		}
	}

	public override void OnGraphStop(Playable playable)
	{
		if (_startAInstance != null)
		{
			DestroyItem(_startAInstance);
			_startAInstance = null;
		}
		if (_startBInstance != null)
		{
			DestroyItem(_startBInstance);
			_startBInstance = null;
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
		_currentStartItemA = null;
		_currentStartItemB = null;
		_currentEndItem = null;
		_effectiveStartItemA = null;
		_effectiveStartItemB = null;
		_effectiveEndItem = null;
		_startA = null;
		_startB = null;
		_end = null;
		_parent = null;
		_director = null;
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
		if ((object)_startA == null)
		{
			_startA = startPointA.Resolve(_director);
		}
		if ((object)_startB == null)
		{
			_startB = startPointB.Resolve(_director);
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
	}

	private void UpdateEffectiveItems()
	{
		if (_data != null)
		{
			_effectiveStartItemA = _data.startItem;
			_effectiveStartItemB = _data.secondaryStartItem;
			_effectiveEndItem = _data.endItem;
		}
		else
		{
			_effectiveStartItemA = null;
			_effectiveStartItemB = null;
			_effectiveEndItem = null;
		}
	}

	private void OnItemsChanged()
	{
		_itemsDirty = true;
	}

	private void EnsureInstances(string effectiveStartItemA, string effectiveStartItemB, string effectiveEndItem, Quaternion rotA, Quaternion rotB, Quaternion rotCorner)
	{
		if (effectiveStartItemA != _currentStartItemA)
		{
			if (_startAInstance != null)
			{
				DestroyItem(_startAInstance);
				_startAInstance = null;
			}
			_currentStartItemA = effectiveStartItemA;
		}
		if (effectiveStartItemB != _currentStartItemB)
		{
			if (_startBInstance != null)
			{
				DestroyItem(_startBInstance);
				_startBInstance = null;
			}
			_currentStartItemB = effectiveStartItemB;
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
		if (_startAInstance == null && !string.IsNullOrEmpty(effectiveStartItemA))
		{
			_startAInstance = PrefabHelper.CreateVisualForItemNamePooled(effectiveStartItemA);
			if (_parent != null)
			{
				_startAInstance.transform.SetParent(_parent, worldPositionStays: true);
			}
			_startAInstance.transform.rotation = rotA;
			_startAInstance.SetActive(value: false);
		}
		if (_startBInstance == null && !string.IsNullOrEmpty(effectiveStartItemB))
		{
			_startBInstance = PrefabHelper.CreateVisualForItemNamePooled(effectiveStartItemB);
			if (_parent != null)
			{
				_startBInstance.transform.SetParent(_parent, worldPositionStays: true);
			}
			_startBInstance.transform.rotation = rotB;
			_startBInstance.SetActive(value: false);
		}
		if (_endInstance == null && !string.IsNullOrEmpty(effectiveEndItem))
		{
			_endInstance = PrefabHelper.CreateVisualForItemNamePooled(effectiveEndItem);
			if (_parent != null)
			{
				_endInstance.transform.SetParent(_parent, worldPositionStays: true);
			}
			_endInstance.transform.rotation = rotCorner;
			_endInstance.SetActive(value: false);
		}
	}

	private void DestroyItem(GameObject item)
	{
		UnityEngine.Object.Destroy(item);
	}
}
