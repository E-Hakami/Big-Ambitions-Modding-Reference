using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace UnityEngine.UI.Extensions;

[AddComponentMenu("UI/Extensions/Range Slider", 34)]
[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
public class CustomRangeSlider : Selectable, IDragHandler, IEventSystemHandler, IInitializePotentialDragHandler, ICanvasElement
{
	[Serializable]
	public class RangeSliderEvent : UnityEvent<float, float>
	{
	}

	protected enum InteractionState
	{
		Low,
		High,
		Bar,
		None
	}

	[SerializeField]
	private RectTransform m_FillRect;

	[SerializeField]
	private RectTransform m_LowHandleRect;

	[SerializeField]
	private RectTransform m_HighHandleRect;

	[Space]
	[SerializeField]
	private float m_MinValue;

	[SerializeField]
	private float m_MaxValue = 1f;

	[SerializeField]
	private bool m_WholeNumbers;

	[SerializeField]
	protected float m_LowValue;

	[SerializeField]
	protected float m_HighValue;

	[Space]
	[SerializeField]
	private RangeSliderEvent m_OnValueChanged = new RangeSliderEvent();

	protected InteractionState interactionState = InteractionState.None;

	private Image m_FillImage;

	private Transform m_FillTransform;

	private RectTransform m_FillContainerRect;

	private Transform m_HighHandleTransform;

	private RectTransform m_HighHandleContainerRect;

	private Transform m_LowHandleTransform;

	private RectTransform m_LowHandleContainerRect;

	private Vector2 m_LowOffset = Vector2.zero;

	private Vector2 m_HighOffset = Vector2.zero;

	private DrivenRectTransformTracker m_Tracker;

	private bool m_DelayedUpdateVisuals;

	public RectTransform FillRect
	{
		get
		{
			return m_FillRect;
		}
		set
		{
			if (SetClass(ref m_FillRect, value))
			{
				UpdateCachedReferences();
				UpdateVisuals();
			}
		}
	}

	public RectTransform LowHandleRect
	{
		get
		{
			return m_LowHandleRect;
		}
		set
		{
			if (SetClass(ref m_LowHandleRect, value))
			{
				UpdateCachedReferences();
				UpdateVisuals();
			}
		}
	}

	public RectTransform HighHandleRect
	{
		get
		{
			return m_HighHandleRect;
		}
		set
		{
			if (SetClass(ref m_HighHandleRect, value))
			{
				UpdateCachedReferences();
				UpdateVisuals();
			}
		}
	}

	public float MinValue
	{
		get
		{
			return m_MinValue;
		}
		set
		{
			if (SetStruct(ref m_MinValue, value))
			{
				SetLow(m_LowValue);
				SetHigh(m_HighValue);
				UpdateVisuals();
			}
		}
	}

	public float MaxValue
	{
		get
		{
			return m_MaxValue;
		}
		set
		{
			if (SetStruct(ref m_MaxValue, value))
			{
				SetLow(m_LowValue);
				SetHigh(m_HighValue);
				UpdateVisuals();
			}
		}
	}

	public bool WholeNumbers
	{
		get
		{
			return m_WholeNumbers;
		}
		set
		{
			if (SetStruct(ref m_WholeNumbers, value))
			{
				SetLow(m_LowValue);
				SetHigh(m_HighValue);
				UpdateVisuals();
			}
		}
	}

	public virtual float LowValue
	{
		get
		{
			if (WholeNumbers)
			{
				return Mathf.Round(m_LowValue);
			}
			return m_LowValue;
		}
		set
		{
			SetLow(value);
		}
	}

	public float NormalizedLowValue
	{
		get
		{
			if (Mathf.Approximately(MinValue, MaxValue))
			{
				return 0f;
			}
			return Mathf.InverseLerp(MinValue, MaxValue, LowValue);
		}
		set
		{
			LowValue = Mathf.Lerp(MinValue, MaxValue, value);
		}
	}

	public virtual float HighValue
	{
		get
		{
			if (WholeNumbers)
			{
				return Mathf.Round(m_HighValue);
			}
			return m_HighValue;
		}
		set
		{
			SetHigh(value);
		}
	}

	public float NormalizedHighValue
	{
		get
		{
			if (Mathf.Approximately(MinValue, MaxValue))
			{
				return 0f;
			}
			return Mathf.InverseLerp(MinValue, MaxValue, HighValue);
		}
		set
		{
			HighValue = Mathf.Lerp(MinValue, MaxValue, value);
		}
	}

	public RangeSliderEvent OnValueChanged
	{
		get
		{
			return m_OnValueChanged;
		}
		set
		{
			m_OnValueChanged = value;
		}
	}

	private float StepSize
	{
		get
		{
			if (!WholeNumbers)
			{
				return (MaxValue - MinValue) * 0.1f;
			}
			return 1f;
		}
	}

	public virtual void SetValueWithoutNotify(float low, float high)
	{
		SetLow(low, sendCallback: false);
		SetHigh(high, sendCallback: false);
	}

	public virtual void Rebuild(CanvasUpdate executing)
	{
	}

	public virtual void LayoutComplete()
	{
	}

	public virtual void GraphicUpdateComplete()
	{
	}

	public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
	{
		if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
		{
			return false;
		}
		currentValue = newValue;
		return true;
	}

	public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
	{
		if (currentValue.Equals(newValue))
		{
			return false;
		}
		currentValue = newValue;
		return true;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		UpdateCachedReferences();
		SetLow(LowValue, sendCallback: false);
		SetHigh(HighValue, sendCallback: false);
		UpdateVisuals();
	}

	protected override void OnDisable()
	{
		m_Tracker.Clear();
		base.OnDisable();
	}

	protected virtual void Update()
	{
		if (m_DelayedUpdateVisuals)
		{
			m_DelayedUpdateVisuals = false;
			UpdateVisuals();
		}
	}

	protected override void OnDidApplyAnimationProperties()
	{
		base.OnDidApplyAnimationProperties();
	}

	private void UpdateCachedReferences()
	{
		if ((bool)m_FillRect && m_FillRect != (RectTransform)base.transform)
		{
			m_FillTransform = m_FillRect.transform;
			m_FillImage = m_FillRect.GetComponent<Image>();
			if (m_FillTransform.parent != null)
			{
				m_FillContainerRect = m_FillTransform.parent.GetComponent<RectTransform>();
			}
		}
		else
		{
			m_FillRect = null;
			m_FillContainerRect = null;
			m_FillImage = null;
		}
		if ((bool)m_HighHandleRect && m_HighHandleRect != (RectTransform)base.transform)
		{
			m_HighHandleTransform = m_HighHandleRect.transform;
			if (m_HighHandleTransform.parent != null)
			{
				m_HighHandleContainerRect = m_HighHandleTransform.parent.GetComponent<RectTransform>();
			}
		}
		else
		{
			m_HighHandleRect = null;
			m_HighHandleContainerRect = null;
		}
		if ((bool)m_LowHandleRect && m_LowHandleRect != (RectTransform)base.transform)
		{
			m_LowHandleTransform = m_LowHandleRect.transform;
			if (m_LowHandleTransform.parent != null)
			{
				m_LowHandleContainerRect = m_LowHandleTransform.parent.GetComponent<RectTransform>();
			}
		}
		else
		{
			m_LowHandleRect = null;
			m_LowHandleContainerRect = null;
		}
	}

	private void SetLow(float input)
	{
		SetLow(input, sendCallback: true);
	}

	protected virtual void SetLow(float input, bool sendCallback)
	{
		float num = Mathf.Clamp(input, MinValue, HighValue);
		if (WholeNumbers)
		{
			num = Mathf.Round(num);
		}
		if (m_LowValue != num)
		{
			m_LowValue = num;
			UpdateVisuals();
			if (sendCallback)
			{
				UISystemProfilerApi.AddMarker("RangeSlider.lowValue", this);
				m_OnValueChanged.Invoke(num, HighValue);
			}
		}
	}

	private void SetHigh(float input)
	{
		SetHigh(input, sendCallback: true);
	}

	protected virtual void SetHigh(float input, bool sendCallback)
	{
		float num = Mathf.Clamp(input, LowValue, MaxValue);
		if (WholeNumbers)
		{
			num = Mathf.Round(num);
		}
		if (m_HighValue != num)
		{
			m_HighValue = num;
			UpdateVisuals();
			if (sendCallback)
			{
				UISystemProfilerApi.AddMarker("RangeSlider.highValue", this);
				m_OnValueChanged.Invoke(LowValue, num);
			}
		}
	}

	protected override void OnRectTransformDimensionsChange()
	{
		base.OnRectTransformDimensionsChange();
		if (IsActive())
		{
			UpdateVisuals();
		}
	}

	private void UpdateVisuals()
	{
		m_Tracker.Clear();
		if (m_FillContainerRect != null)
		{
			m_Tracker.Add(this, m_FillRect, DrivenTransformProperties.Anchors);
			Vector2 zero = Vector2.zero;
			Vector2 one = Vector2.one;
			zero[0] = NormalizedLowValue;
			one[0] = NormalizedHighValue;
			m_FillRect.anchorMin = zero;
			m_FillRect.anchorMax = one;
		}
		if (m_LowHandleContainerRect != null)
		{
			m_Tracker.Add(this, m_LowHandleRect, DrivenTransformProperties.Anchors);
			Vector2 zero2 = Vector2.zero;
			Vector2 one2 = Vector2.one;
			float value = (one2[0] = NormalizedLowValue);
			zero2[0] = value;
			m_LowHandleRect.anchorMin = zero2;
			m_LowHandleRect.anchorMax = one2;
		}
		if (m_HighHandleContainerRect != null)
		{
			m_Tracker.Add(this, m_HighHandleRect, DrivenTransformProperties.Anchors);
			Vector2 zero3 = Vector2.zero;
			Vector2 one3 = Vector2.one;
			float value = (one3[0] = NormalizedHighValue);
			zero3[0] = value;
			m_HighHandleRect.anchorMin = zero3;
			m_HighHandleRect.anchorMax = one3;
		}
	}

	private void UpdateDrag(PointerEventData eventData, Camera cam)
	{
		switch (interactionState)
		{
		case InteractionState.Low:
			NormalizedLowValue = CalculateDrag(eventData, cam, m_LowHandleContainerRect, m_LowOffset);
			break;
		case InteractionState.High:
			NormalizedHighValue = CalculateDrag(eventData, cam, m_HighHandleContainerRect, m_HighOffset);
			break;
		case InteractionState.Bar:
			CalculateBarDrag(eventData, cam);
			break;
		case InteractionState.None:
			break;
		}
	}

	private float CalculateDrag(PointerEventData eventData, Camera cam, RectTransform containerRect, Vector2 offset)
	{
		RectTransform rectTransform = containerRect ?? m_FillContainerRect;
		if (rectTransform != null && rectTransform.rect.size[0] > 0f)
		{
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, cam, out var localPoint))
			{
				return 0f;
			}
			localPoint -= rectTransform.rect.position;
			return Mathf.Clamp01((localPoint - offset)[0] / rectTransform.rect.size[0]);
		}
		return 0f;
	}

	private void CalculateBarDrag(PointerEventData eventData, Camera cam)
	{
		RectTransform fillContainerRect = m_FillContainerRect;
		if (!(fillContainerRect != null) || !(fillContainerRect.rect.size[0] > 0f) || !RectTransformUtility.ScreenPointToLocalPointInRectangle(fillContainerRect, eventData.position, cam, out var localPoint))
		{
			return;
		}
		localPoint -= fillContainerRect.rect.position;
		if (NormalizedLowValue >= 0f && NormalizedHighValue <= 1f)
		{
			float num = (NormalizedHighValue + NormalizedLowValue) / 2f;
			float num2 = Mathf.Clamp01(localPoint[0] / fillContainerRect.rect.size[0]) - num;
			if (NormalizedLowValue + num2 < 0f)
			{
				num2 = 0f - NormalizedLowValue;
			}
			else if (NormalizedHighValue + num2 > 1f)
			{
				num2 = 1f - NormalizedHighValue;
			}
			NormalizedLowValue += num2;
			NormalizedHighValue += num2;
		}
	}

	private bool MayDrag(PointerEventData eventData)
	{
		if (IsActive() && IsInteractable())
		{
			return eventData.button == PointerEventData.InputButton.Left;
		}
		return false;
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		if (!MayDrag(eventData))
		{
			return;
		}
		m_LowOffset = (m_HighOffset = Vector2.zero);
		Vector2 localPoint;
		if (m_HighHandleRect != null && RectTransformUtility.RectangleContainsScreenPoint(m_HighHandleRect, eventData.position, eventData.enterEventCamera))
		{
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HighHandleRect, eventData.position, eventData.pressEventCamera, out localPoint))
			{
				m_HighOffset = localPoint;
			}
			interactionState = InteractionState.High;
			if (base.transition == Transition.ColorTint)
			{
				base.targetGraphic = m_HighHandleRect.GetComponent<Graphic>();
			}
		}
		else if (m_LowHandleRect != null && RectTransformUtility.RectangleContainsScreenPoint(m_LowHandleRect, eventData.position, eventData.enterEventCamera))
		{
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_LowHandleRect, eventData.position, eventData.pressEventCamera, out localPoint))
			{
				m_LowOffset = localPoint;
			}
			interactionState = InteractionState.Low;
			if (base.transition == Transition.ColorTint)
			{
				base.targetGraphic = m_LowHandleRect.GetComponent<Graphic>();
			}
		}
		else
		{
			UpdateDrag(eventData, eventData.pressEventCamera);
			if (eventData.pointerCurrentRaycast.gameObject == m_FillRect.gameObject)
			{
				interactionState = InteractionState.Bar;
			}
			if (base.transition == Transition.ColorTint)
			{
				base.targetGraphic = m_FillImage;
			}
		}
		base.OnPointerDown(eventData);
	}

	public virtual void OnDrag(PointerEventData eventData)
	{
		if (MayDrag(eventData))
		{
			UpdateDrag(eventData, eventData.pressEventCamera);
		}
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		base.OnPointerUp(eventData);
		interactionState = InteractionState.None;
	}

	public override void OnMove(AxisEventData eventData)
	{
	}

	public virtual void OnInitializePotentialDrag(PointerEventData eventData)
	{
		eventData.useDragThreshold = false;
	}
}
