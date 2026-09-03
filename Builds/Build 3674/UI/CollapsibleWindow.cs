using System.Collections;
using DG.Tweening;
using Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI;

public class CollapsibleWindow : MonoBehaviour
{
	[Header("Animation")]
	[SerializeField]
	private float collapseAnimationDuration;

	[SerializeField]
	private float unCollapseAnimationDuration;

	public Vector3 collapsedPosition;

	public Vector3 unCollapsedPosition;

	public Vector3 collapsedRotation;

	public Vector3 unCollapsedRotation;

	[Header("Button")]
	[SerializeField]
	private GameObject collapseButton;

	[SerializeField]
	private int collapsedButtonRotation;

	[SerializeField]
	private int unCollapsedButtonRotation;

	[Header("Hover animation")]
	[SerializeField]
	private HoverFunction hoverFunction;

	[SerializeField]
	private float hoverAnimationDuration;

	public Vector3 hoverPosition;

	public UnityEvent<bool> onStateChange = new UnityEvent<bool>();

	private bool _collapsed;

	private bool _onAnimation;

	private RectTransform _rectTransform;

	private Image _buttonIcon;

	private RectTransform _buttonRectTransform;

	public bool IsCollapsed => _collapsed;

	private void Awake()
	{
		_rectTransform = base.transform.GetComponent<RectTransform>();
		_buttonIcon = collapseButton.transform.GetImageByName("Icon");
		_buttonRectTransform = _buttonIcon.GetComponent<RectTransform>();
		collapseButton.GetComponent<Button>().onClick.AddListener(ChangeState);
		hoverFunction.onHover.AddListener(OnHover);
	}

	private void ChangeState()
	{
		ChangeState(!_collapsed);
	}

	public void ChangeState(bool newState, bool instant = false)
	{
		if (!_onAnimation && _collapsed != newState)
		{
			_collapsed = newState;
			onStateChange.Invoke(_collapsed);
			StartCoroutine(CollapseAnimation(instant));
		}
	}

	private IEnumerator CollapseAnimation(bool instant)
	{
		_onAnimation = true;
		float num = (instant ? 0f : (_collapsed ? collapseAnimationDuration : unCollapseAnimationDuration));
		if (collapsedPosition != unCollapsedPosition)
		{
			Vector3 vector = (_collapsed ? collapsedPosition : unCollapsedPosition);
			_rectTransform.DOAnchorPos(vector, num).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		}
		if (collapsedRotation != unCollapsedRotation)
		{
			Vector3 endValue = (_collapsed ? collapsedRotation : unCollapsedRotation);
			_rectTransform.DORotate(endValue, num).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		}
		yield return new WaitForSecondsRealtime(num);
		int num2 = (_collapsed ? collapsedButtonRotation : unCollapsedButtonRotation);
		_buttonRectTransform.eulerAngles = new Vector3(0f, 0f, num2);
		_onAnimation = false;
	}

	private void OnHover(bool hovering)
	{
		if (!_onAnimation && _collapsed)
		{
			_rectTransform.DOAnchorPos(hovering ? hoverPosition : collapsedPosition, hoverAnimationDuration).SetUpdate(isIndependentUpdate: true);
		}
	}

	private void OnDisable()
	{
		if (_onAnimation)
		{
			StopAllCoroutines();
			_rectTransform.anchoredPosition = (_collapsed ? collapsedPosition : unCollapsedPosition);
			_rectTransform.eulerAngles = (_collapsed ? collapsedRotation : unCollapsedRotation);
			_buttonRectTransform.eulerAngles = new Vector3(0f, 0f, _collapsed ? collapsedButtonRotation : unCollapsedButtonRotation);
			_onAnimation = false;
		}
	}
}
