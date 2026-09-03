using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Components;

public class ReorderableListItem : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
	public RectTransform handle;

	public ReorderableList list;

	[Header("Move left on hover")]
	[SerializeField]
	private bool moveLeftOnHover;

	[SerializeField]
	[ShowIf("moveLeftOnHover")]
	private RectTransform dragArea;

	[SerializeField]
	[ShowIf("moveLeftOnHover")]
	private float moveLeftAmount = 15f;

	[SerializeField]
	[ShowIf("moveLeftOnHover")]
	private float moveLeftDuration = 0.1f;

	[SerializeField]
	[ShowIf("moveLeftOnHover")]
	private Transform moveLeftTarget;

	[SerializeField]
	[ShowIf("moveLeftOnHover")]
	private Ease moveLeftEase = Ease.OutBounce;

	private float _initialMoveLeftXPosition;

	private bool _isDragging;

	private bool _isLeft;

	private void Awake()
	{
		if ((bool)moveLeftTarget)
		{
			_initialMoveLeftXPosition = moveLeftTarget.localPosition.x;
		}
	}

	private void Reset()
	{
		if (list == null)
		{
			list = GetComponentInParent<ReorderableList>();
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!(list == null) && IsHandle(eventData))
		{
			DoMoveLeft();
			_isDragging = true;
			list.BeginDrag(this, eventData);
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!(list == null))
		{
			list.Drag(eventData);
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!(list == null))
		{
			list.EndDrag();
			_isDragging = false;
		}
	}

	private bool IsHandle(PointerEventData e)
	{
		if (handle == null)
		{
			return true;
		}
		if (e.pointerPressRaycast.gameObject?.transform is RectTransform rectTransform)
		{
			return rectTransform.IsChildOf(handle);
		}
		return false;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (moveLeftOnHover && !_isDragging && IsPointerOverArea(eventData))
		{
			DoMoveLeft();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (moveLeftOnHover && !_isDragging)
		{
			DoMoveBack();
		}
	}

	private bool IsPointerOverArea(PointerEventData eventData)
	{
		if (dragArea == null || eventData == null)
		{
			return true;
		}
		return RectTransformUtility.RectangleContainsScreenPoint(dragArea, eventData.position, eventData.enterEventCamera);
	}

	public void OnPointerMove(PointerEventData eventData)
	{
		if (moveLeftOnHover && !_isDragging)
		{
			bool flag = IsPointerOverArea(eventData);
			if (_isLeft && !flag)
			{
				DoMoveBack();
			}
			else if (!_isLeft & flag)
			{
				DoMoveLeft();
			}
		}
	}

	private void DoMoveLeft()
	{
		if (!_isLeft)
		{
			_isLeft = true;
			moveLeftTarget.DOLocalMoveX(_initialMoveLeftXPosition - moveLeftAmount, moveLeftDuration).SetEase(moveLeftEase).SetUpdate(isIndependentUpdate: true)
				.SetLink(base.gameObject);
		}
	}

	private void DoMoveBack()
	{
		if (_isLeft)
		{
			_isLeft = false;
			moveLeftTarget.DOLocalMoveX(_initialMoveLeftXPosition, moveLeftDuration).SetEase(moveLeftEase).SetUpdate(isIndependentUpdate: true)
				.SetLink(base.gameObject);
		}
	}
}
