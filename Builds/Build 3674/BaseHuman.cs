using System;
using System.Collections.Generic;
using BigAmbitions.Characters;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.Items;
using Controllers;
using Dancing;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using NaughtyAttributes;
using UnityEngine;

public class BaseHuman : MonoBehaviour
{
	public const float ForwardTransitionSpeed = 20f;

	public AppearanceSetter appearanceSetter;

	public Animator animator;

	public Transform handContent;

	public Transform rightHand;

	public Transform leftHand;

	public Transform head;

	public Transform upperChest;

	public SkinnedMeshRenderer shadowCaster;

	public Action<bool> onSittingChanged;

	[NonSerialized]
	[ShowNonSerializedField]
	public Transform isSittingOn;

	public static readonly int Forward = Animator.StringToHash("Forward");

	public static readonly int IsMoving = Animator.StringToHash("IsMoving");

	public static readonly int MotionTime = Animator.StringToHash("MotionTime");

	protected static readonly int Zombie = Animator.StringToHash("Zombie");

	protected static readonly int HoldingBox = Animator.StringToHash("HoldingBox");

	private static readonly int StartAnimationState = Animator.StringToHash("Null State");

	private static readonly int Dancing = Animator.StringToHash("Dancing");

	private static readonly int TypeOfDance = Animator.StringToHash("TypeOfDance");

	private static readonly int IsHoldingACoat = Animator.StringToHash("isHoldingAJacket");

	private PermanentAnimationType _currentPermanentAnimationType;

	private Color _umbrellaColor;

	private bool? _hasUmbrella;

	private readonly Dictionary<Transform, List<Renderer>> _attachedRenderers = new Dictionary<Transform, List<Renderer>>();

	private static readonly int MaskColorRedId = Shader.PropertyToID("_MaskColorRed");

	private static readonly int MaskColorGreenId = Shader.PropertyToID("_MaskColorGreen");

	public bool HasUmbrella()
	{
		bool valueOrDefault = _hasUmbrella == true;
		if (!_hasUmbrella.HasValue)
		{
			valueOrDefault = 0.8f.Probability();
			_hasUmbrella = valueOrDefault;
		}
		return _hasUmbrella.Value;
	}

	public virtual void OnEnable()
	{
		if (!(animator == null))
		{
			if ((bool)isSittingOn)
			{
				animator.SetBool((isSittingOn.TryGetComponentInParent<SeatController>(out var component) && component.Item.leanBackSitAnimation) ? PermanentAnimationType.SittingOnCinemaChair : PermanentAnimationType.Sitting);
			}
			appearanceSetter.SetFatnessValueOnAnimator();
		}
	}

	protected virtual void Freeze()
	{
	}

	public virtual void UnFreeze()
	{
		if ((bool)isSittingOn)
		{
			SeatController componentInParent = isSittingOn.GetComponentInParent<SeatController>();
			if ((bool)componentInParent)
			{
				componentInParent.OnSittingChanged(isSittingOn, isSitting: false);
			}
			isSittingOn = null;
			onSittingChanged?.Invoke(obj: false);
		}
		Reset();
	}

	public virtual void Reset()
	{
		animator.SetBool(_currentPermanentAnimationType, state: false);
		animator.SetBool(PermanentAnimationType.SittingOnCinemaChair, state: false);
		animator.ResetTrigger(AnimationType.UsingCashRegister.ToStringFast());
	}

	public virtual void LayOnBed(EntityController bed, Transform sleepPositionTransform)
	{
		animator.SetBool(PermanentAnimationType.Laying);
		_currentPermanentAnimationType = PermanentAnimationType.Laying;
	}

	public virtual void SitOnChair(Transform chair, PermanentAnimationType animationType = PermanentAnimationType.Sitting)
	{
		if ((bool)isSittingOn)
		{
			SeatController componentInParent = isSittingOn.GetComponentInParent<SeatController>();
			if ((bool)componentInParent)
			{
				componentInParent.OnSittingChanged(isSittingOn, isSitting: false);
			}
			onSittingChanged?.Invoke(obj: false);
		}
		isSittingOn = chair;
		animator.SetBool(animationType);
		_currentPermanentAnimationType = animationType;
		if (chair != null)
		{
			SeatController componentInParent = chair.GetComponentInParent<SeatController>();
			if ((bool)componentInParent)
			{
				componentInParent.OnSittingChanged(chair, isSitting: true);
				animator.SetBool(PermanentAnimationType.SittingOnCinemaChair, componentInParent.Item.leanBackSitAnimation);
			}
			base.transform.eulerAngles = chair.transform.eulerAngles;
			base.transform.position = chair.transform.position;
			onSittingChanged?.Invoke(obj: true);
		}
	}

	public bool TryGetAttachedRenderersToTransform(Transform t, out List<Renderer> renderers)
	{
		return _attachedRenderers.TryGetValue(t, out renderers);
	}

	public void AddHandObject(string handObjectName, bool isRightHand = true, List<CustomColor> customColors = null, bool isPlayer = false)
	{
		if (string.IsNullOrEmpty(handObjectName))
		{
			return;
		}
		Transform transform = PrefabHelper.CreatePrefab(handObjectName).transform;
		Renderer component = transform.GetComponent<Renderer>();
		IReadOnlyList<Renderer> shadowCasters = transform.GetComponent<ItemController>()?.GetShadowCasters();
		AttachObjectToHand(new AttachedObjectData
		{
			objectTransform = transform,
			renderer = component,
			shadowCasters = shadowCasters,
			position = transform.position,
			rotation = transform.rotation
		}, isRightHand);
		if (handObjectName == "PhoneOff" || handObjectName == "PhoneOn")
		{
			component.material.SetColor(MaskColorRedId, UnityEngine.Random.ColorHSV());
		}
		else if (handObjectName.Equals("Umbrella", StringComparison.OrdinalIgnoreCase))
		{
			if (!isPlayer)
			{
				if (_umbrellaColor == default(Color))
				{
					_umbrellaColor = InstanceBehavior<GlobalReferences>.Instance.pedestrianUmbrellasColors.GetRandom();
				}
				component.material.SetColor(MaskColorGreenId, _umbrellaColor);
			}
			else if (customColors != null)
			{
				SetColorToAddedObject(customColors, transform);
			}
		}
		else if (handObjectName == "Mop")
		{
			transform.GetComponent<MopController>().TogglePhysics(physicsEnabled: false);
		}
		appearanceSetter.visualsUpdated?.Invoke();
	}

	public void AttachObjectToHand(AttachedObjectData attachedObjectData, bool isRightHand)
	{
		Transform parent = (isRightHand ? rightHand : leftHand);
		AttachObject(attachedObjectData, parent);
	}

	private void AttachObject(AttachedObjectData attachedObjectData, Transform parent)
	{
		attachedObjectData.objectTransform.SetParent(parent);
		attachedObjectData.objectTransform.localPosition = attachedObjectData.position;
		attachedObjectData.objectTransform.localRotation = attachedObjectData.rotation;
		attachedObjectData.objectTransform.gameObject.SetLayerRecursively(LayerHelper.HumanLayerIndex);
		if (_attachedRenderers.ContainsKey(parent))
		{
			_attachedRenderers[parent].Add(attachedObjectData.renderer);
			if (attachedObjectData.shadowCasters != null)
			{
				_attachedRenderers[parent].AddRange(attachedObjectData.shadowCasters);
			}
			return;
		}
		List<Renderer> list = new List<Renderer> { attachedObjectData.renderer };
		if (attachedObjectData.shadowCasters != null)
		{
			list.AddRange(attachedObjectData.shadowCasters);
		}
		_attachedRenderers.Add(parent, list);
	}

	public void AttachObjectToUpperChest(AttachedObjectData attachedObjectData)
	{
		AttachObject(attachedObjectData, upperChest);
	}

	public void AddHeadObject(string headObjectName, Vector3 position = default(Vector3), List<CustomColor> customColors = null)
	{
		if (!string.IsNullOrEmpty(headObjectName))
		{
			Transform transform = PrefabHelper.CreatePrefab(headObjectName).transform;
			Renderer component = transform.GetComponent<Renderer>();
			if (position == default(Vector3))
			{
				position = transform.position;
			}
			Quaternion rotation = transform.rotation;
			AttachObject(new AttachedObjectData
			{
				objectTransform = transform,
				renderer = component,
				position = position,
				rotation = rotation
			}, head);
			if (customColors != null)
			{
				SetColorToAddedObject(customColors, transform);
			}
			appearanceSetter.visualsUpdated?.Invoke();
		}
	}

	private static void SetColorToAddedObject(List<CustomColor> customColors, Transform handObject)
	{
		MaterialPropertyBlock maskColorPropertyBlock = ColorOverlay.MaskColorPropertyBlock;
		Renderer component = handObject.GetComponent<Renderer>();
		foreach (CustomColor customColor in customColors)
		{
			component.GetPropertyBlock(maskColorPropertyBlock);
			maskColorPropertyBlock.SetColor(ColorOverlay.GetMaskId(customColor.channel), customColor.color);
			component.SetPropertyBlock(maskColorPropertyBlock);
		}
	}

	public void ToggleAllAttachedObjects(bool toggle)
	{
		foreach (List<Renderer> value in _attachedRenderers.Values)
		{
			foreach (Renderer item in value)
			{
				item.enabled = toggle;
			}
		}
	}

	protected bool IsDancing()
	{
		return animator.GetBool(Dancing);
	}

	public static string GetHandObjectNameFromPermanentAnimationType(PermanentAnimationType anim)
	{
		return anim switch
		{
			PermanentAnimationType.Drunk => "DrunkBottle", 
			PermanentAnimationType.TalkingPhone => "PhoneOff", 
			PermanentAnimationType.TextingPhone => "PhoneOn", 
			PermanentAnimationType.DealerBlackjackIdle => "DealerPackOfCards", 
			PermanentAnimationType.ReadingABook => "OpenBook", 
			PermanentAnimationType.CleaningIdle => "Mop", 
			PermanentAnimationType.HoldingIceCream => "IceCream", 
			PermanentAnimationType.HoldingAnItem => "GlassOfBeer", 
			_ => null, 
		};
	}

	[Obsolete("This must be fixed, currently always returns true due to fingers being children of the hand transforms")]
	public bool HasHandObject(bool inRightHand = true)
	{
		if (inRightHand)
		{
			return rightHand.childCount > 0;
		}
		return leftHand.childCount > 0;
	}

	public void RemoveHandObject()
	{
		if (_attachedRenderers.TryGetValue(leftHand, out var value) && value.Count > 0)
		{
			UnityEngine.Object.Destroy(value[0].gameObject);
			value.Clear();
		}
		if (_attachedRenderers.TryGetValue(rightHand, out var value2) && value2.Count > 0)
		{
			UnityEngine.Object.Destroy(value2[0].gameObject);
			value2.Clear();
		}
	}

	public void RemoveHeadObject()
	{
		if (_attachedRenderers.TryGetValue(head, out var value) && value.Count > 0)
		{
			UnityEngine.Object.Destroy(value[0].gameObject);
			value.Clear();
		}
	}

	public void RemoveUpperChestObject()
	{
		if (_attachedRenderers.TryGetValue(upperChest, out var value) && value.Count != 0)
		{
			UnityEngine.Object.Destroy(value[0].gameObject);
			value.Clear();
		}
	}

	public void SetDance(DanceType danceType)
	{
		animator.SetBool(Dancing, value: true);
		animator.SetFloat(TypeOfDance, danceType.GetValue());
	}

	public void StopDancing()
	{
		animator.SetBool(Dancing, value: false);
	}

	public void HoldAnItem()
	{
		animator.SetBool(PermanentAnimationType.HoldingAnItem);
	}

	public void StopHoldingAnItem()
	{
		animator.SetBool(PermanentAnimationType.HoldingAnItem, state: false);
	}

	public void HoldACoat()
	{
		animator.SetBool(IsHoldingACoat, value: true);
	}

	public void StopHoldingACoat()
	{
		animator.SetBool(IsHoldingACoat, value: false);
	}

	public void ResetAnimator()
	{
		animator.Play(StartAnimationState, animator.GetLayerIndex("Base Actions"));
		animator.SetBool(IsMoving, value: false);
		animator.SetBool(Zombie, value: false);
	}
}
