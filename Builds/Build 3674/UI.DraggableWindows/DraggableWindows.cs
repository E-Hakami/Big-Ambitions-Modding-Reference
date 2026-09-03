using System;
using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace UI.DraggableWindows;

public class DraggableWindows : InstanceBehavior<DraggableWindows>
{
	public struct Data
	{
		public string id;

		public RectTransform element;

		public DraggableWindowHandle handle;

		public Vector3 defaultPosition;
	}

	public SignAppearance signAppearance;

	public List<DraggableWindowData> data = new List<DraggableWindowData>();

	[SerializeField]
	private RectTransform safeArea;

	private readonly List<Data> _draggableWindows = new List<Data>();

	private readonly Vector3[] _elementCorners = new Vector3[4];

	private readonly Vector3[] _safeAreaCorners = new Vector3[4];

	[NonSerialized]
	public bool isCurrentlyDragging;

	protected override void Awake()
	{
		base.Awake();
		if (base.IsMainInstance)
		{
			data = PlayerSettingsHelper.Data?.draggableWindows ?? new List<DraggableWindowData>();
			GlobalEvents.onSaveGame = (Action)Delegate.Combine(GlobalEvents.onSaveGame, new Action(Save));
		}
	}

	private void LateUpdate()
	{
		Vector2 vector = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		isCurrentlyDragging = false;
		foreach (Data draggableWindow in _draggableWindows)
		{
			if (!(draggableWindow.element == null) && !(draggableWindow.handle == null) && draggableWindow.handle.isDragging)
			{
				HandleDragging(draggableWindow, vector);
			}
		}
	}

	protected override void OnDestroy()
	{
		if (base.IsMainInstance)
		{
			base.OnDestroy();
			Save();
		}
	}

	public void RegisterDraggableWindow(Data window)
	{
		if (window.element == null)
		{
			Debug.LogError("Draggable window has no element assigned");
			return;
		}
		if (window.handle == null)
		{
			Debug.LogError("Draggable window has no handle assigned");
			return;
		}
		window.element.position = ClampPosition(window.element, window.element.position);
		window.handle.OnDragEnded += delegate
		{
			SaveWindowPosition(window);
		};
		_draggableWindows.Add(window);
	}

	public void ResetWindowsPositions()
	{
		foreach (Data draggableWindow in _draggableWindows)
		{
			DraggableWindowData draggableWindowData = GetData(draggableWindow.id);
			if (draggableWindowData != null)
			{
				draggableWindowData.position = ClampPosition(draggableWindow.element, draggableWindow.defaultPosition);
				draggableWindow.element.position = draggableWindowData.position;
			}
		}
		Save();
	}

	private void HandleDragging(Data window, Vector3 inputPosition)
	{
		if (window.handle.offset == Vector3.zero)
		{
			window.handle.offset = window.element.position - inputPosition;
		}
		window.element.position = ClampPosition(window.element, inputPosition + window.handle.offset);
		isCurrentlyDragging = true;
	}

	private void SaveWindowPosition(Data window)
	{
		DraggableWindowData draggableWindowData = GetData(window.id);
		if (draggableWindowData == null)
		{
			if (string.IsNullOrEmpty(window.id))
			{
				return;
			}
			draggableWindowData = new DraggableWindowData
			{
				id = window.id
			};
			data.Add(draggableWindowData);
		}
		if (draggableWindowData.position != window.element.position)
		{
			draggableWindowData.position = window.element.position;
			Save();
		}
	}

	public Vector3 ClampPosition(RectTransform element, Vector3 position)
	{
		if (safeArea == null || element == null)
		{
			return position;
		}
		safeArea.GetWorldCorners(_safeAreaCorners);
		element.GetWorldCorners(_elementCorners);
		Vector3 vector = position - element.position;
		float x = _safeAreaCorners[0].x;
		float y = _safeAreaCorners[0].y;
		float x2 = _safeAreaCorners[2].x;
		float y2 = _safeAreaCorners[2].y;
		float elementMin = _elementCorners[0].x + vector.x;
		float elementMin2 = _elementCorners[0].y + vector.y;
		float elementMax = _elementCorners[2].x + vector.x;
		float elementMax2 = _elementCorners[2].y + vector.y;
		float containmentOffset = GetContainmentOffset(elementMin, elementMax, x, x2);
		float containmentOffset2 = GetContainmentOffset(elementMin2, elementMax2, y, y2);
		return position + new Vector3(containmentOffset, containmentOffset2, 0f);
	}

	private static float GetContainmentOffset(float elementMin, float elementMax, float safeMin, float safeMax)
	{
		float num = elementMax - elementMin;
		float num2 = safeMax - safeMin;
		if (num > num2)
		{
			return (safeMin + safeMax - elementMin - elementMax) * 0.5f;
		}
		if (elementMin < safeMin)
		{
			return safeMin - elementMin;
		}
		if (elementMax > safeMax)
		{
			return safeMax - elementMax;
		}
		return 0f;
	}

	public DraggableWindowData GetData(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		foreach (DraggableWindowData datum in data)
		{
			if (datum.id == id)
			{
				return datum;
			}
		}
		return null;
	}

	private void Save()
	{
		PlayerSettingsHelper.SaveDraggableWindows(data);
	}
}
