using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class ViewBlockingEntity : EntityController
{
	private static MaterialPropertyBlock MaterialPropertyBlock;

	private static readonly int RevealID = Shader.PropertyToID("_Reveal");

	[FormerlySerializedAs("_renderersToHide")]
	public List<Renderer> renderersToHide;

	[FormerlySerializedAs("_renderersToFade")]
	public List<Renderer> renderersToFade;

	public List<GameObject> objectsToDisable;

	public float timeToCameraBlock = 0.4f;

	[SerializeField]
	private float fadeSpeed = 10f;

	[HideInInspector]
	public float timeSinceLastCameraBlock;

	[HideInInspector]
	public bool temporarilyDisableCameraBlock;

	public List<ViewBlockingEntityPart> cityBuildingParts;

	private float _currentRevealValue = 1f;

	protected bool isInCameraBlockMode;

	public bool IsActiveInViewBlockingObjectManager { get; set; }

	public int LastViewBlockingObjectManagerFrame { get; set; }

	private static MaterialPropertyBlock MaterialPropertyBlockGetter => MaterialPropertyBlock ?? (MaterialPropertyBlock = new MaterialPropertyBlock());

	protected override int DefaultLayer => LayerHelper.RoadsLayerIndex;

	public bool IsInCameraBlockMode => isInCameraBlockMode;

	public override void Start()
	{
		base.Start();
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
		GlobalEvents.onCityMapClosed = (Action)Delegate.Combine(GlobalEvents.onCityMapClosed, new Action(OnCityMapClosed));
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
	}

	private void OnCityMapToggle(bool isOn)
	{
		if (isOn)
		{
			SetCameraBlockMode(isOn: false);
			SetFadeSate(1f);
		}
	}

	private void OnEnterBuilding(Address _)
	{
		SetFadeSate();
	}

	private void OnCityMapClosed()
	{
		SetFadeSate();
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			ViewBlockingObjectManager.UnregisterEntity(this);
			GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Remove(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
			GlobalEvents.onCityMapClosed = (Action)Delegate.Remove(GlobalEvents.onCityMapClosed, new Action(OnCityMapClosed));
			GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
			base.OnDestroy();
		}
	}

	public bool DoUpdate()
	{
		if (!base.enabled)
		{
			return false;
		}
		if (timeSinceLastCameraBlock >= 0f && isInCameraBlockMode)
		{
			timeSinceLastCameraBlock += Time.unscaledDeltaTime;
			if (timeSinceLastCameraBlock > timeToCameraBlock)
			{
				timeSinceLastCameraBlock = -1f;
				SetCameraBlockMode(isOn: false);
			}
		}
		float num = (isInCameraBlockMode ? 0f : 1f);
		float num2 = num - _currentRevealValue;
		if (num2 * num2 > Mathf.Epsilon * Mathf.Epsilon && AnyRendererVisible())
		{
			_currentRevealValue = Mathf.Lerp(_currentRevealValue, num, Time.unscaledDeltaTime * fadeSpeed);
			SetFadeSate();
			return true;
		}
		if (_currentRevealValue != num)
		{
			_currentRevealValue = num;
			SetFadeSate();
		}
		return isInCameraBlockMode;
	}

	public void SetFadeSate(float overridenHideStateValue = -1f)
	{
		if (overridenHideStateValue > -1f)
		{
			_currentRevealValue = overridenHideStateValue;
		}
		foreach (Renderer item in renderersToFade)
		{
			item.GetPropertyBlock(MaterialPropertyBlockGetter);
			MaterialPropertyBlockGetter.SetFloat(RevealID, _currentRevealValue);
			item.SetPropertyBlock(MaterialPropertyBlockGetter);
		}
	}

	private bool AnyRendererVisible()
	{
		for (int i = 0; i < renderersToFade.Count; i++)
		{
			if (renderersToFade[i].isVisible)
			{
				return true;
			}
		}
		return false;
	}

	public void CameraBlock()
	{
		if (base.enabled)
		{
			timeSinceLastCameraBlock = 0f;
			if (!isInCameraBlockMode)
			{
				SetCameraBlockMode(isOn: true);
			}
			ViewBlockingObjectManager.MarkEntityActive(this);
		}
	}

	public virtual bool SetCameraBlockMode(bool isOn)
	{
		if (temporarilyDisableCameraBlock || isInCameraBlockMode == isOn)
		{
			return false;
		}
		isInCameraBlockMode = isOn;
		OnBlockModeChanged(isOn);
		return true;
	}

	protected virtual void OnBlockModeChanged(bool isOn)
	{
		if (renderersToHide.Count > 0)
		{
			foreach (Renderer item in renderersToHide)
			{
				if (!(item == null) && !item.CompareTag("BuildingLOD"))
				{
					SetRendererToHideVisibility(item, isOn);
				}
			}
		}
		if (objectsToDisable.Count != 0)
		{
			foreach (GameObject item2 in objectsToDisable)
			{
				item2.SetActive(!isOn);
			}
		}
		foreach (ViewBlockingEntityPart item3 in cityBuildingParts.Where((ViewBlockingEntityPart x) => !x.CompareTag("BuildingBottomPlane")))
		{
			item3.gameObject.layer = ((!isOn) ? LayerHelper.BuildingLayerIndex : 0);
		}
	}

	public static void SetRendererToHideVisibility(Renderer rendererToHide, bool shouldHide)
	{
		if (rendererToHide.shadowCastingMode == ShadowCastingMode.Off)
		{
			rendererToHide.enabled = !shouldHide;
		}
		else
		{
			rendererToHide.shadowCastingMode = ((!shouldHide) ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly);
		}
	}

	[ContextMenu("Set Renderers To Fade")]
	public void SetRenderersToFade()
	{
		renderersToFade = renderers.ToList();
	}

	[ContextMenu("Set Renderers To Hide")]
	public void SetRenderersToHide()
	{
		renderersToHide = renderers.Where((Renderer x) => x.shadowCastingMode != ShadowCastingMode.ShadowsOnly).ToList();
	}
}
