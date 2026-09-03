using UI.Guiders;
using UI.PurchaseVehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial;

public class TutorialPointer : MonoBehaviour
{
	public TutorialPointerData data;

	[HideInInspector]
	public DirectionGuiderType guiderType;

	[SerializeField]
	private Image backgroundImage;

	[SerializeField]
	private SpriteRenderer backgroundSpriteRenderer;

	private bool _isPointerEnabled;

	private void Start()
	{
		data.Init();
	}

	private void OnDestroy()
	{
		data.Dispose();
	}

	private void Update()
	{
		if (GameManager.isCitySceneBeingUnloaded)
		{
			return;
		}
		if (PurchaseVehicleUI.IsShowcaseAnimationRunning)
		{
			if (_isPointerEnabled)
			{
				Hide();
			}
		}
		else if (data.ShouldBeEnabled())
		{
			data.Relocate(this);
			if (!_isPointerEnabled)
			{
				data.OnShow(this);
				Show();
			}
		}
		else if (_isPointerEnabled)
		{
			data.OnHide();
			Hide();
		}
	}

	public void Show()
	{
		_isPointerEnabled = true;
		if (base.gameObject != null)
		{
			ChangeVisuals(visible: true);
		}
	}

	public void Hide()
	{
		_isPointerEnabled = false;
		ChangeVisuals(visible: false);
	}

	private void ChangeVisuals(bool visible)
	{
		if (!(base.gameObject == null))
		{
			if (backgroundImage != null)
			{
				backgroundImage.gameObject.SetActive(visible);
			}
			if (backgroundSpriteRenderer != null)
			{
				backgroundSpriteRenderer.gameObject.SetActive(visible);
			}
		}
	}

	public void SetGuiderType(DirectionGuiderType type)
	{
		if (!(InstanceBehavior<GuidersManager>.Instance == null))
		{
			guiderType = type;
			Color guiderColor = GuidersManager.GetGuiderColor(type);
			if (backgroundSpriteRenderer != null)
			{
				backgroundSpriteRenderer.color = guiderColor;
			}
			if (backgroundImage != null)
			{
				backgroundImage.color = guiderColor;
			}
		}
	}
}
