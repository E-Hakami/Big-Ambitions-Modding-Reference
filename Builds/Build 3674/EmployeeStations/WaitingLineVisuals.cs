using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EmployeeStations;

public class WaitingLineVisuals
{
	private readonly WaitingLineData _data;

	private readonly WaitingLineTransforms _transforms;

	private readonly List<WaitingLineAnchor> _anchors = new List<WaitingLineAnchor>();

	public WaitingLineAnchor GetAnchorAtIndex(int index)
	{
		return _anchors[index - 1];
	}

	public WaitingLineVisuals(WaitingLineData data, WaitingLineTransforms transforms)
	{
		_data = data;
		_transforms = transforms;
		WaitingLineData data2 = _data;
		data2.onSelectedAnchorPositionUpdated = (Action)Delegate.Combine(data2.onSelectedAnchorPositionUpdated, new Action(ResetSpots));
		WaitingLineData data3 = _data;
		data3.onAnchorRemoved = (Action)Delegate.Combine(data3.onAnchorRemoved, new Action(Show));
	}

	public void Show()
	{
		ResetAnchors();
		ResetSpots();
		_transforms.waitingLineObject.gameObject.SetActive(value: true);
	}

	public void Hide()
	{
		_transforms.waitingLineObject.gameObject.SetActive(value: false);
	}

	public void ResetAnchors()
	{
		_anchors.Clear();
		_transforms.anchorTemplate.gameObject.SetActive(value: false);
		foreach (Transform item in _transforms.anchorTemplate.parent)
		{
			if (item != _transforms.anchorTemplate && item != _transforms.spotTemplate.parent)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
		for (int i = 1; i < _data.anchorsPositions.Count; i++)
		{
			AddAnchor(i);
		}
	}

	public void ResetSpots()
	{
		_transforms.spotTemplate.gameObject.SetActive(value: false);
		foreach (Transform item in _transforms.spotTemplate.parent)
		{
			if (item != _transforms.spotTemplate)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
		if (_data.spots.Count == 0)
		{
			return;
		}
		foreach (Vector3 spot in _data.spots)
		{
			AddSpot(spot);
		}
	}

	public WaitingLineAnchor AddAnchor(int anchorIndex)
	{
		Transform transform = UnityEngine.Object.Instantiate(_transforms.anchorTemplate, _transforms.anchorTemplate.parent);
		transform.position = _data.GetAnchorPosition(anchorIndex);
		transform.gameObject.SetActive(value: true);
		WaitingLineAnchor component = transform.GetComponent<WaitingLineAnchor>();
		component.Init(anchorIndex);
		_anchors.Add(component);
		return component;
	}

	public void EditAnchor(int anchorIndex)
	{
		if (!(_data.anchorSelected != null))
		{
			WaitingLineAnchor waitingLineAnchor = _anchors.FirstOrDefault((WaitingLineAnchor x) => x.Index == anchorIndex);
			if (!(waitingLineAnchor == null))
			{
				waitingLineAnchor.SetHoverColor();
				_data.anchorSelected = waitingLineAnchor;
			}
		}
	}

	private void AddSpot(Vector3 spotPosition)
	{
		Transform transform = UnityEngine.Object.Instantiate(_transforms.spotTemplate, _transforms.spotTemplate.parent);
		transform.transform.position = spotPosition + Vector3.up * 0.02f;
		transform.gameObject.SetActive(value: true);
	}
}
