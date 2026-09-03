using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using TMPro;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class OverlayBase<T> : MonoBehaviour where T : Enum
{
	[HideInInspector]
	public EntityController linkedController;

	[HideInInspector]
	public EntityController relevantController;

	[HideInInspector]
	public Vector3 customPosition;

	[SerializeField]
	protected TMP_Text headerField;

	protected readonly Dictionary<T, IOverlay> overlayComponents = new Dictionary<T, IOverlay>();

	private T[] _overlayEnumNames;

	private Vector3 _mouseOffset = Vector3.zero;

	private Vector3 _primaryInteractionOffset = Vector3.zero;

	private Camera _cam;

	private ItemAliasUpdateListener _aliasUpdateListener;

	protected virtual void Awake()
	{
		_overlayEnumNames = Enum.GetValues(typeof(T)) as T[];
		_aliasUpdateListener = new ItemAliasUpdateListener(RefreshHeaderName);
	}

	protected virtual void OnDisable()
	{
		_aliasUpdateListener?.Clear();
	}

	public void UpdateOffsets()
	{
		if (!_cam)
		{
			_cam = GameManager.GetMainCamera();
		}
		Vector3 vector = _cam.WorldToScreenPoint(relevantController.transform.position) + relevantController.itemWarningIconOffset;
		_mouseOffset = Input.mousePosition - vector;
		Transform transform = GetHighestPriorityOverlay()?.GetPrimaryInteractable();
		_primaryInteractionOffset = (transform ? (base.transform.position - transform.position) : Vector3.zero);
	}

	public void UpdatePosition()
	{
		if (!_cam)
		{
			_cam = GameManager.GetMainCamera();
		}
		Vector3 position = ((customPosition != default(Vector3)) ? customPosition : relevantController.transform.position);
		Vector3 vector = _cam.WorldToScreenPoint(position) + ((_primaryInteractionOffset == Vector3.zero) ? Vector3.zero : relevantController.itemWarningIconOffset) + _mouseOffset + _primaryInteractionOffset;
		base.transform.position = new Vector3(vector.x, vector.y, 0f);
	}

	public virtual int UpdateOverlay(EntityController entityController, string headerText, bool ctaDisabled = false, bool isBlueprintMode = false)
	{
		SetAliasListener(entityController);
		headerField.SetText(headerText);
		int num = 0;
		T[] overlayEnumNames = _overlayEnumNames;
		foreach (T val in overlayEnumNames)
		{
			IOverlay overlayComponent = GetOverlayComponent(val);
			if (overlayComponent != null && overlayComponent.ShouldTakePriority(entityController) && ShouldShowOverlay(overlayComponent, val))
			{
				num++;
			}
		}
		int num2 = 0;
		overlayEnumNames = _overlayEnumNames;
		foreach (T val2 in overlayEnumNames)
		{
			IOverlay overlayComponent2 = GetOverlayComponent(val2);
			if (!(overlayComponent2 == null))
			{
				bool flag = ShouldShowOverlay(overlayComponent2, val2);
				bool flag2 = ((num <= 0) ? flag : (flag && overlayComponent2.ShouldTakePriority(entityController)));
				if (flag2)
				{
					num2++;
					overlayComponent2.linkedController = entityController;
					overlayComponent2.UpdateOverlay(entityController);
				}
				overlayComponent2.SetActive(flag2);
			}
		}
		if (num <= 0)
		{
			return num2;
		}
		return num;
		bool ShouldShowOverlay(IOverlay overlay, T overlayName)
		{
			if (HasFlag(overlayName) && (overlay.isVisibleInBlueprintMode || !isBlueprintMode) && overlay.ShouldShow(entityController))
			{
				return !((overlay is CtaOverlay) & ctaDisabled);
			}
			return false;
		}
	}

	public void UpdateDynamicComponents(DynamicOverlayUpdateType updateType)
	{
		EntityController item = OverlayHelper.GetRelevantEntity(linkedController).Item1;
		foreach (var (value, overlay2) in overlayComponents)
		{
			if ((overlay2.dynamicUpdateType & updateType) != 0)
			{
				if (!HasFlag(value) || !overlay2.ShouldShow(item))
				{
					overlay2.SetActive(active: false);
					continue;
				}
				overlay2.UpdateOverlay(item);
				overlay2.SetActive(active: true);
			}
		}
	}

	protected virtual IOverlay GetOverlayComponent(T overlayType)
	{
		return null;
	}

	protected virtual bool HasFlag(T value)
	{
		return false;
	}

	protected virtual IOverlay GetHighestPriorityOverlay()
	{
		return null;
	}

	private void SetAliasListener(EntityController entityController)
	{
		_aliasUpdateListener.ListenTo((entityController as ItemController)?.ItemInstance);
	}

	private void RefreshHeaderName()
	{
		if (!(linkedController == null))
		{
			headerField.SetText(OverlayHelper.GetOverlayHeaderText(linkedController));
		}
	}
}
