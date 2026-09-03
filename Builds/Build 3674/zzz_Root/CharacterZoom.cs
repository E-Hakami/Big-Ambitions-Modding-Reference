using BigAmbitions.Characters.Appearance;
using BigAmbitions.InputSystem;
using Character.Customization;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterZoom : MonoBehaviour
{
	[SerializeField]
	private float timeForZooming;

	[SerializeField]
	private GameObject rotateLeftButton;

	[SerializeField]
	private GameObject rotateRightButton;

	[SerializeField]
	private float customZoomMaxSpeed;

	[SerializeField]
	private float zoomDecaySpeed;

	[SerializeField]
	[BoxGroup("Head")]
	private Vector3 headZoomedInCameraPosition;

	[SerializeField]
	[BoxGroup("Torso")]
	private Vector3 torsoZoomedInCameraPosition;

	[SerializeField]
	[BoxGroup("Legs")]
	private Vector3 legsZoomedInCameraPosition;

	[SerializeField]
	[BoxGroup("Shoes")]
	private Vector3 shoesZoomedInCameraPosition;

	[SerializeField]
	private bool zoomedInPositionsAreLocal;

	public UiHoverTarget hoverTarget;

	[BoxGroup("Automatic zoom")]
	public bool disableAutomaticZoom;

	[BoxGroup("Automatic zoom")]
	public Image automaticZoomIcon;

	[SerializeField]
	[BoxGroup("Automatic zoom")]
	private Sprite disabledAutomaticZoomSprite;

	[SerializeField]
	[BoxGroup("Automatic zoom")]
	private Sprite enabledAutomaticZoomSprite;

	private Camera _camera;

	private Vector3 _initialPosition;

	private bool _onZoomTransition;

	private AppearanceElementType _currentElement;

	private float _customZoomSpeed;

	private float _customZoomProgress;

	private float _timeWithoutScrolling;

	private bool _zoomingIn;

	private bool _zoomingOut;

	private void Awake()
	{
		_camera = GetComponent<Camera>();
		_initialPosition = base.transform.position;
		_currentElement = AppearanceElementType.Head;
		if (zoomedInPositionsAreLocal)
		{
			headZoomedInCameraPosition = base.transform.parent.TransformPoint(headZoomedInCameraPosition);
			torsoZoomedInCameraPosition = base.transform.parent.TransformPoint(torsoZoomedInCameraPosition);
			legsZoomedInCameraPosition = base.transform.parent.TransformPoint(legsZoomedInCameraPosition);
			shoesZoomedInCameraPosition = base.transform.parent.TransformPoint(shoesZoomedInCameraPosition);
		}
	}

	private void OnEnable()
	{
		if ((bool)automaticZoomIcon)
		{
			automaticZoomIcon.sprite = (disableAutomaticZoom ? disabledAutomaticZoomSprite : enabledAutomaticZoomSprite);
		}
	}

	private void Update()
	{
		if (!GameManager.IsInFocus)
		{
			return;
		}
		float num = (_zoomingIn ? 1f : (_zoomingOut ? (-1f) : GetZoomControl()));
		if (_customZoomSpeed == 0f && num == 0f)
		{
			return;
		}
		if (_onZoomTransition)
		{
			CancelTransitions();
		}
		if (num == 0f)
		{
			if (_timeWithoutScrolling > Time.unscaledDeltaTime * 5f)
			{
				if (Mathf.Abs(_customZoomSpeed) > 0.1f)
				{
					if (_customZoomSpeed > 0f)
					{
						_customZoomSpeed = Mathf.Max(0f, _customZoomSpeed - Time.unscaledDeltaTime * customZoomMaxSpeed * zoomDecaySpeed);
					}
					else
					{
						_customZoomSpeed = Mathf.Min(0f, _customZoomSpeed + Time.unscaledDeltaTime * customZoomMaxSpeed * zoomDecaySpeed);
					}
				}
				else
				{
					_customZoomSpeed = 0f;
				}
			}
			else
			{
				_timeWithoutScrolling += Time.unscaledDeltaTime;
			}
		}
		else
		{
			_timeWithoutScrolling = 0f;
			if (num > 0f)
			{
				_customZoomSpeed = Mathf.Min(_customZoomSpeed + num * customZoomMaxSpeed, customZoomMaxSpeed);
			}
			else
			{
				_customZoomSpeed = Mathf.Max(_customZoomSpeed + num * customZoomMaxSpeed, 0f - customZoomMaxSpeed);
			}
		}
		Vector3 zoomedInCameraPosition = GetZoomedInCameraPosition(_currentElement);
		if (_customZoomSpeed != 0f)
		{
			if (_customZoomSpeed > 0f)
			{
				base.transform.position = Vector3.MoveTowards(base.transform.position, zoomedInCameraPosition, Time.unscaledDeltaTime * _customZoomSpeed);
			}
			else
			{
				base.transform.position = Vector3.MoveTowards(base.transform.position, _initialPosition, (0f - Time.unscaledDeltaTime) * _customZoomSpeed);
			}
		}
		if ((bool)rotateLeftButton && (bool)rotateRightButton)
		{
			if (Vector3.SqrMagnitude(base.transform.position - _initialPosition) < 0.1f)
			{
				rotateLeftButton.gameObject.SetActive(value: true);
				rotateRightButton.gameObject.SetActive(value: true);
			}
			else
			{
				rotateLeftButton.gameObject.SetActive(value: false);
				rotateRightButton.gameObject.SetActive(value: false);
			}
		}
	}

	private float GetZoomControl()
	{
		if ((bool)hoverTarget)
		{
			if (!hoverTarget.IsHovered)
			{
				return 0f;
			}
		}
		else if ((bool)EventSystem.current && EventSystem.current.IsPointerOverGameObject())
		{
			GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
			if (!currentSelectedGameObject)
			{
				return 0f;
			}
			if (currentSelectedGameObject.name != "ZoomInButton" && currentSelectedGameObject.name != "ZoomOutButton")
			{
				return 0f;
			}
		}
		return Input.mouseScrollDelta.y + PlayerAction.Zoom.Value();
	}

	public void SetZoomIn(bool set)
	{
		_zoomingIn = set;
	}

	public void SetZoomOut(bool set)
	{
		_zoomingOut = set;
	}

	public void ZoomTo(AppearanceElementType element)
	{
		if (!disableAutomaticZoom)
		{
			_customZoomSpeed = 0f;
			_timeWithoutScrolling = 0f;
			CancelTransitions();
			_onZoomTransition = true;
			if ((bool)rotateLeftButton && (bool)rotateRightButton)
			{
				rotateLeftButton.gameObject.SetActive(value: false);
				rotateRightButton.gameObject.SetActive(value: false);
			}
			_currentElement = element;
			Vector3 zoomedInCameraPosition = GetZoomedInCameraPosition(element);
			base.transform.DOMove(zoomedInCameraPosition, timeForZooming).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject)
				.OnComplete(delegate
				{
					_onZoomTransition = false;
				});
		}
	}

	public void ResetZoom()
	{
		if (!disableAutomaticZoom)
		{
			CancelTransitions();
			_currentElement = AppearanceElementType.Head;
			if ((bool)rotateLeftButton && (bool)rotateRightButton)
			{
				rotateLeftButton.gameObject.SetActive(value: true);
				rotateRightButton.gameObject.SetActive(value: true);
			}
			base.transform.DOMove(_initialPosition, timeForZooming).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		}
	}

	public void ToggleAutomaticZoom()
	{
		disableAutomaticZoom = !disableAutomaticZoom;
		if ((bool)automaticZoomIcon)
		{
			automaticZoomIcon.sprite = (disableAutomaticZoom ? disabledAutomaticZoomSprite : enabledAutomaticZoomSprite);
		}
		CancelTransitions();
	}

	private void CancelTransitions()
	{
		base.transform.DOKill();
		_camera.DOKill();
		_onZoomTransition = false;
	}

	private Vector3 GetZoomedInCameraPosition(AppearanceElementType element)
	{
		switch (element)
		{
		case AppearanceElementType.Hair:
		case AppearanceElementType.Head:
		case AppearanceElementType.HeadAccessory:
		case AppearanceElementType.Eyes:
		case AppearanceElementType.Mouth:
		case AppearanceElementType.Nose:
		case AppearanceElementType.Beard:
		case AppearanceElementType.Eyebrows:
			return headZoomedInCameraPosition;
		case AppearanceElementType.Torso:
		case AppearanceElementType.TorsoAccessory:
			return torsoZoomedInCameraPosition;
		case AppearanceElementType.Legs:
		case AppearanceElementType.LegsAccessory:
			return legsZoomedInCameraPosition;
		case AppearanceElementType.Feet:
		case AppearanceElementType.FeetAccessory:
			return shoesZoomedInCameraPosition;
		default:
			return _initialPosition;
		}
	}
}
