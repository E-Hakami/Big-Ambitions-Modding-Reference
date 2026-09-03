using UnityEngine;

namespace UI.DraggableWindows;

public class DraggableWindow : MonoBehaviour
{
	[SerializeField]
	private string id;

	[SerializeField]
	private DraggableWindowHandle handle;

	[SerializeField]
	private RectTransform element;

	private bool _isClampScheduled;

	private void Start()
	{
		if (element == null)
		{
			element = GetComponent<RectTransform>();
		}
		if (element == null)
		{
			Debug.LogError("Draggable window '" + base.gameObject.name + "' has no element assigned");
			return;
		}
		Vector3 position = element.position;
		if (handle == null)
		{
			Debug.LogError("Draggable window '" + base.gameObject.name + "' has no handle assigned");
			return;
		}
		if (string.IsNullOrEmpty(id))
		{
			element.position = InstanceBehavior<DraggableWindows>.Instance.ClampPosition(element, element.position);
		}
		else
		{
			DraggableWindowData data = InstanceBehavior<DraggableWindows>.Instance.GetData(id);
			element.position = ((data == null) ? InstanceBehavior<DraggableWindows>.Instance.ClampPosition(element, element.position) : InstanceBehavior<DraggableWindows>.Instance.ClampPosition(element, data.position));
		}
		InstanceBehavior<DraggableWindows>.Instance.RegisterDraggableWindow(new DraggableWindows.Data
		{
			id = id,
			element = element,
			handle = handle,
			defaultPosition = position
		});
	}

	private void OnEnable()
	{
		ScheduleClampCurrentPositionBeforeRender();
	}

	private void OnDisable()
	{
		Canvas.willRenderCanvases -= ClampCurrentPositionBeforeRender;
		_isClampScheduled = false;
	}

	private void OnRectTransformDimensionsChange()
	{
		if (base.isActiveAndEnabled)
		{
			ScheduleClampCurrentPositionBeforeRender();
		}
	}

	private void ScheduleClampCurrentPositionBeforeRender()
	{
		if (!_isClampScheduled)
		{
			_isClampScheduled = true;
			Canvas.willRenderCanvases += ClampCurrentPositionBeforeRender;
		}
	}

	private void ClampCurrentPositionBeforeRender()
	{
		Canvas.willRenderCanvases -= ClampCurrentPositionBeforeRender;
		_isClampScheduled = false;
		ClampCurrentPosition();
	}

	private void ClampCurrentPosition()
	{
		if (!(element == null))
		{
			DraggableWindowData data = InstanceBehavior<DraggableWindows>.Instance.GetData(id);
			Vector3 position = ((data == null) ? element.position : ((Vector3)data.position));
			Vector3 vector = InstanceBehavior<DraggableWindows>.Instance.ClampPosition(element, position);
			if (!(vector == element.position))
			{
				element.position = vector;
			}
		}
	}
}
