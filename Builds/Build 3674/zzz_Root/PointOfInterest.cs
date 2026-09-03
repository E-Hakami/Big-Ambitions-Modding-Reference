using Extensions;
using Helpers;
using TMPro;
using UI;
using UI.Guiders;
using UnityEngine;
using UnityEngine.UI;

public class PointOfInterest : MonoBehaviour
{
	private const float DecimetersPerMeter = 10f;

	private static int CachedPlayerPositionFrame = -1;

	private static Vector3 CachedPlayerPosition;

	[HideInInspector]
	public Transform target;

	[HideInInspector]
	public Vector3 offset;

	[HideInInspector]
	public bool hidden;

	[HideInInspector]
	public bool isGuider;

	[SerializeField]
	private BuildingIcon buildingIcon;

	[SerializeField]
	private Image ownerIcon;

	[SerializeField]
	private Image rentIcon;

	[SerializeField]
	private Image foodDeliveryIcon;

	[SerializeField]
	private RectTransform pointerRectTransform;

	[SerializeField]
	private GameObject container;

	[SerializeField]
	private Image blobImage;

	[SerializeField]
	private float rotationSpeed = 10f;

	[SerializeField]
	private TMP_Text tmpText;

	[SerializeField]
	private Button button;

	[SerializeField]
	[Tooltip("X: left; Y: right; Z: bottom; W: top")]
	private Vector4 deadZone = Vector4.zero;

	private string _text;

	private DirectionGuiderType _guiderType;

	private int _lastDistanceDecimeters = -1;

	public Address targetAddress { get; set; }

	public bool Permanent { get; private set; }

	public void Initialize()
	{
		base.name = "Poi" + Random.Range(1000, 100000);
		ownerIcon.gameObject.SetActive(value: false);
		rentIcon.gameObject.SetActive(value: false);
		foodDeliveryIcon.gameObject.SetActive(value: false);
		InstanceBehavior<CityManager>.Instance?.UpdatePointOfInterests();
	}

	private void Start()
	{
		if (isGuider)
		{
			if (_guiderType == DirectionGuiderType.Destination)
			{
				button.onClick.AddListener(delegate
				{
					SaveGameManager.Current.customDestination = null;
					GuidersManager.ResetGuider(DirectionGuiderType.Destination);
				});
			}
			else if (_guiderType == DirectionGuiderType.PrivateDriver)
			{
				button.onClick.AddListener(delegate
				{
					InstanceBehavior<UIs>.Instance.smartphoneUI.DismissPrivateDriver();
				});
			}
			else
			{
				button.gameObject.SetActive(value: false);
			}
		}
		else
		{
			container.SetActive(value: false);
		}
	}

	public void SetPermanent(bool isPermanent = true)
	{
		Permanent = isPermanent;
		base.gameObject.SetActive(isPermanent);
		InstanceBehavior<CityManager>.Instance?.UpdatePointOfInterests();
	}

	public bool IsVisibleToPlayersStreetView()
	{
		if (!target)
		{
			return false;
		}
		if (isGuider)
		{
			return true;
		}
		return MathHelper.DistanceSqr(GetCurrentPlayerPosition(), target.position) <= 2500f;
	}

	public void UpdatePosition(Camera cam)
	{
		if (!target)
		{
			return;
		}
		Vector3 vector = cam.WorldToScreenPoint(target.position + offset);
		bool flag = vector.z < 0f;
		vector.z = 0f;
		Quaternion quaternion = Quaternion.identity;
		if (isGuider)
		{
			Vector2 vector2 = new Vector2(Screen.width, Screen.height);
			Vector4 vector3 = new Vector4(deadZone.x * vector2.x, deadZone.y * vector2.y, deadZone.z * vector2.x, deadZone.w * vector2.y);
			Rect rect = new Rect(vector3.z - vector2.x / 2f, vector3.y - vector2.y / 2f, vector2.x - vector3.x - vector3.z, vector2.y - vector3.w - vector3.y);
			if (!rect.Contains(new Vector2(vector.x - vector2.x / 2f, vector.y - vector2.y / 2f)))
			{
				Vector3 to = new Vector3(vector2.x * 0.5f, vector2.y * 0.5f) - vector;
				float num = Vector3.SignedAngle(flag ? Vector3.up : Vector3.down, to, Vector3.forward);
				vector = MathHelper.GetPointOnRectangle(num, rect);
				vector = new Vector3(vector.x + vector2.x / 2f, vector.y + vector2.y / 2f, 0f);
				quaternion = Quaternion.Euler(new Vector3(0f, 0f, 180f + num));
			}
			quaternion = Quaternion.Slerp(pointerRectTransform.rotation, quaternion, Time.unscaledDeltaTime * rotationSpeed);
			if (!CityMap.IsOpen)
			{
				float num2 = Vector3.Distance(target.position + offset, PlayerHelper.GetCityPosition());
				int num3 = (int)(num2 * 10f);
				if (num3 != _lastDistanceDecimeters)
				{
					_lastDistanceDecimeters = num3;
					tmpText.text = _text + " (" + num2.ToFormattedDistance() + ")";
				}
			}
		}
		if (pointerRectTransform.rotation != quaternion)
		{
			pointerRectTransform.rotation = quaternion;
		}
		if (base.transform.position != vector)
		{
			base.transform.position = vector;
		}
	}

	private Vector3 GetCurrentPlayerPosition()
	{
		int frameCount = Time.frameCount;
		if (CachedPlayerPositionFrame == frameCount)
		{
			return CachedPlayerPosition;
		}
		CachedPlayerPosition = (SubwaySystem.IsRiding ? SubwaySystem.CurrentPosition : InstanceBehavior<GameManager>.Instance.playerController.transform.position);
		CachedPlayerPositionFrame = frameCount;
		return CachedPlayerPosition;
	}

	public void SetHidden(bool hide)
	{
		hidden = hide;
		base.gameObject.SetActive(!hide);
	}

	public void SetText(string text)
	{
		_text = text;
		tmpText.text = _text;
		_lastDistanceDecimeters = -1;
	}

	public void SetIcon(Sprite icon, Color backgroundColor)
	{
		buildingIcon.SetIcon(icon);
		SetBackground(backgroundColor);
	}

	public void SetBackground(Color color)
	{
		blobImage.color = color;
	}

	public void SetGuider(DirectionGuiderType guiderType)
	{
		_guiderType = guiderType;
		isGuider = true;
		SetPermanent();
	}

	public void SetContainerActive(bool isActive)
	{
		if (container.gameObject.activeSelf != isActive)
		{
			container.SetActive(isActive);
		}
	}

	public void SetOwnerStatus(bool isOwner)
	{
		ownerIcon.gameObject.SetActive(isOwner);
	}

	public void SetRentStatus(bool isRenter)
	{
		rentIcon.gameObject.SetActive(isRenter);
	}

	public void SetFoodDeliveryStatus(bool hasOffer)
	{
		foodDeliveryIcon.gameObject.SetActive(hasOffer);
	}

	public void SetIconRotation(float angle)
	{
		buildingIcon.SetIconRotation(angle);
	}

	public void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			InstanceBehavior<CityManager>.Instance?.cityMap?.pois.Remove(this);
			InstanceBehavior<CityManager>.Instance?.UpdatePointOfInterests();
		}
	}
}
