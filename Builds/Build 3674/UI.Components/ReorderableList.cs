using System;
using System.Collections.Generic;
using DG.Tweening;
using Extensions;
using JimmysUnityUtilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Components;

public class ReorderableList : MonoBehaviour
{
	[SerializeField]
	private VerticalLayoutGroup verticalLayoutGroup;

	[SerializeField]
	private ContentSizeFitter contentSizeFitter;

	[Header("Move out of way")]
	public float moveOutOfWayDuration = 0.08f;

	public Ease moveOutOfWayEase = Ease.OutQuad;

	private readonly List<ReorderableListItem> _items = new List<ReorderableListItem>();

	private readonly List<float> _slotY = new List<float>();

	private readonly Dictionary<Transform, Tweener> _shiftTweens = new Dictionary<Transform, Tweener>();

	private ReorderableListItem _draggingItem;

	private int _startIndex;

	private RectTransform Content => (RectTransform)base.transform;

	public event Action<int, int> OnItemReordered;

	public event Action<ReorderableListItem> OnDragStarted;

	public event Action<ReorderableListItem> OnDragEnded;

	private void OnEnable()
	{
		CoroutineUtility.RunAfterOneFrame(InitializeItems);
	}

	public void Reinitialize()
	{
		CoroutineUtility.RunAfterOneFrame(InitializeItems);
	}

	private void InitializeItems()
	{
		_items.Clear();
		foreach (Transform item in base.transform)
		{
			if (item.gameObject.activeSelf && item.TryGetComponent<ReorderableListItem>(out var component))
			{
				_items.Add(component);
			}
		}
	}

	public void BeginDrag(ReorderableListItem item, PointerEventData _)
	{
		if (_items.Count < 2)
		{
			return;
		}
		_draggingItem = item;
		OnDragStarted?.Invoke(item);
		LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
		if ((bool)verticalLayoutGroup)
		{
			verticalLayoutGroup.gameObject.AddComponent<LayoutElement>().preferredHeight = verticalLayoutGroup.GetRectTransform().rect.height;
			verticalLayoutGroup.enabled = false;
		}
		if ((bool)contentSizeFitter)
		{
			contentSizeFitter.enabled = false;
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
		_startIndex = _items.IndexOf(item);
		_shiftTweens.Clear();
		_slotY.Clear();
		for (int i = 0; i < _items.Count; i++)
		{
			_slotY.Add(_items[i].transform.position.y);
		}
		if (_slotY.Count >= 2)
		{
			float num = _slotY[0];
			List<float> slotY = _slotY;
			if (num < slotY[slotY.Count - 1])
			{
				_slotY.Reverse();
			}
		}
	}

	public void Drag(PointerEventData e)
	{
		if (!_draggingItem)
		{
			return;
		}
		RectTransformUtility.ScreenPointToWorldPointInRectangle(Content, e.position, e.pressEventCamera, out var worldPoint);
		float y = worldPoint.y;
		List<float> slotY = _slotY;
		float y2 = Mathf.Clamp(y, slotY[slotY.Count - 1], _slotY[0]);
		Vector3 position = _draggingItem.transform.position;
		_draggingItem.transform.position = new Vector3(position.x, y2, position.z);
		int num = ComputeInsertIndex(y2);
		for (int i = 0; i < _items.Count; i++)
		{
			ReorderableListItem reorderableListItem = _items[i];
			if (reorderableListItem == _draggingItem)
			{
				continue;
			}
			int index = i;
			if (num < _startIndex)
			{
				if (i >= num && i < _startIndex)
				{
					index = i + 1;
				}
			}
			else if (num > _startIndex && i <= num && i > _startIndex)
			{
				index = i - 1;
			}
			float num2 = _slotY[index];
			Transform transform = reorderableListItem.transform;
			if (!(Mathf.Abs(transform.position.y - num2) < 0.0005f))
			{
				if (!_shiftTweens.TryGetValue(transform, out var value) || value == null || !value.active)
				{
					value = transform.DOMoveY(num2, moveOutOfWayDuration).SetEase(moveOutOfWayEase).SetUpdate(isIndependentUpdate: true)
						.SetAutoKill(autoKillOnCompletion: false);
					_shiftTweens[transform] = value;
				}
				else
				{
					Vector3 vector = new Vector3(transform.position.x, num2, transform.position.z);
					value.ChangeEndValue(vector, snapStartValue: true).Play();
				}
			}
		}
	}

	public void EndDrag()
	{
		if (!_draggingItem)
		{
			return;
		}
		float y = _draggingItem.transform.position.y;
		int num = ComputeInsertIndex(y);
		float y2 = _slotY[num];
		_draggingItem.transform.position = new Vector3(_draggingItem.transform.position.x, y2, _draggingItem.transform.position.z);
		_items.Move(_startIndex, num);
		int siblingIndex = _draggingItem.transform.GetSiblingIndex();
		_draggingItem.transform.SetSiblingIndex(siblingIndex + (num - _startIndex));
		foreach (KeyValuePair<Transform, Tweener> shiftTween in _shiftTweens)
		{
			shiftTween.Value?.Kill();
		}
		_shiftTweens.Clear();
		if (_startIndex != num)
		{
			OnItemReordered?.Invoke(_startIndex, num);
		}
		OnDragEnded?.Invoke(_draggingItem);
		_draggingItem = null;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if ((bool)verticalLayoutGroup)
			{
				verticalLayoutGroup.gameObject.RemoveComponent<LayoutElement>();
				verticalLayoutGroup.enabled = true;
			}
			if ((bool)contentSizeFitter)
			{
				contentSizeFitter.enabled = true;
			}
		});
	}

	private int ComputeInsertIndex(float y)
	{
		int count = _slotY.Count;
		if (count < 2)
		{
			return 0;
		}
		int result = 0;
		float num = Mathf.Abs(_slotY[0] - y);
		for (int i = 1; i < count; i++)
		{
			float num2 = Mathf.Abs(_slotY[i] - y);
			if (!(num2 >= num))
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}
}
