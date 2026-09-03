using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ButtonEffects : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler
{
	private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);

	[FormerlySerializedAs("button")]
	[SerializeField]
	private Selectable target;

	[FormerlySerializedAs("soundType")]
	[SerializeField]
	private UiSound mouseDownSound;

	[SerializeField]
	private bool scaleOnHover;

	[SerializeField]
	[ShowIf("scaleOnHover")]
	private float hoverScale = 1.05f;

	[SerializeField]
	[ShowIf("scaleOnHover")]
	private Transform scaleTarget;

	[SerializeField]
	private bool soundOnHover;

	[SerializeField]
	[ShowIf("soundOnHover")]
	private UiSound hoverSound = UiSound.Hover;

	[SerializeField]
	private bool underlineOnHover;

	[SerializeField]
	[ShowIf("underlineOnHover")]
	private TextMeshProUGUI textHoverTarget;

	private FontStyles? _initialFontStyle;

	[Header("Toggle")]
	[SerializeField]
	[ShowIf("IsToggle")]
	private Graphic colorChangingGraphic;

	[SerializeField]
	[ShowIf("IsToggle")]
	private Color toggleOnColor = Color.black;

	[SerializeField]
	[ShowIf("IsToggle")]
	private GameObject toggleOnObject;

	private bool IsToggle => target is Toggle;

	private void Awake()
	{
		if (!IsToggle)
		{
			return;
		}
		Toggle toggle = (Toggle)target;
		toggle.onValueChanged.AddListener(delegate(bool isOn)
		{
			if (colorChangingGraphic != null)
			{
				colorChangingGraphic.color = (isOn ? toggleOnColor : Color.white);
			}
			if (toggleOnObject != null)
			{
				toggleOnObject.SetActive(isOn);
			}
		});
		if (colorChangingGraphic != null)
		{
			colorChangingGraphic.color = (toggle.isOn ? toggleOnColor : Color.white);
		}
		if (toggleOnObject != null)
		{
			toggleOnObject.SetActive(toggle.isOn);
		}
	}

	private void OnValidate()
	{
		if (scaleOnHover)
		{
			Transform transform = scaleTarget;
			if (transform == null)
			{
				transform = base.transform;
			}
			if (transform.GetComponent<RectTransform>().pivot != CenterPivot)
			{
				Debug.LogError("ButtonEffects: ScaleOnHover enabled on " + base.gameObject.name + ", but pivot is not set to 0.5/0.5", base.gameObject);
			}
		}
	}

	private void OnDisable()
	{
		if (scaleOnHover && scaleTarget != null)
		{
			scaleTarget.localScale = Vector3.one;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (target != null && !target.interactable)
		{
			return;
		}
		if (soundOnHover && GameManager.IsInFocus)
		{
			UiSoundHelper.Play(hoverSound);
		}
		if (underlineOnHover && (bool)textHoverTarget)
		{
			FontStyles valueOrDefault = _initialFontStyle.GetValueOrDefault();
			if (!_initialFontStyle.HasValue)
			{
				valueOrDefault = textHoverTarget.fontStyle;
				_initialFontStyle = valueOrDefault;
			}
			textHoverTarget.fontStyle = _initialFontStyle.Value | FontStyles.Underline;
		}
		if (scaleOnHover)
		{
			if (scaleTarget == null)
			{
				scaleTarget = base.transform;
			}
			scaleTarget.DOScale(hoverScale, 0.1f).SetEase(Ease.OutBounce).SetUpdate(isIndependentUpdate: true)
				.SetLink(base.gameObject);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!(target != null) || target.interactable)
		{
			if (underlineOnHover && _initialFontStyle.HasValue)
			{
				textHoverTarget.fontStyle = _initialFontStyle.Value;
			}
			if (scaleOnHover)
			{
				scaleTarget.DOScale(1f, 0.1f).SetEase(Ease.OutBounce).SetUpdate(isIndependentUpdate: true)
					.SetLink(base.gameObject);
			}
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if ((bool)target && target.interactable)
		{
			UiSoundHelper.Play(mouseDownSound);
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		EventSystem.current.SetSelectedGameObject(null);
	}
}
