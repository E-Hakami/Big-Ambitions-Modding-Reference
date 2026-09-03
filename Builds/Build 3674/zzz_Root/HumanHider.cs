using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

public class HumanHider : MonoBehaviour
{
	[SerializeField]
	protected BaseHuman baseHuman;

	[BoxGroup("Detection")]
	public LayerMask layersToHideFromWhenNear;

	[BoxGroup("Detection")]
	public float lowerHeight;

	[BoxGroup("Detection")]
	public float upperHeight;

	[BoxGroup("Detection")]
	public float detectionDistance = 2f;

	private const float UpdateIntervalSeconds = 0.1f;

	private float _nextUpdateTime;

	private readonly List<Renderer> _renderers = new List<Renderer>();

	private MaterialPropertyBlock _materialPropertyBlock;

	private static readonly int NearVehicle = Shader.PropertyToID("_NearVehicle");

	private bool _isNear;

	private MaterialPropertyBlock MaterialPropertyBlock => _materialPropertyBlock ?? (_materialPropertyBlock = new MaterialPropertyBlock());

	public virtual void Start()
	{
		AppearanceSetter appearanceSetter = baseHuman.appearanceSetter;
		appearanceSetter.visualsUpdated = (Action)Delegate.Combine(appearanceSetter.visualsUpdated, new Action(UpdateRenderers));
		UpdateRenderers();
		InitializeNextUpdateTime();
	}

	private void InitializeNextUpdateTime()
	{
		_nextUpdateTime = Time.time + UnityEngine.Random.Range(0f, 1f);
	}

	private void OnEnable()
	{
		InitializeNextUpdateTime();
	}

	private void UpdateRenderers()
	{
		_renderers.Clear();
		IEnumerable<Renderer> collection = from x in baseHuman.appearanceSetter.GetComponentsInChildren<Renderer>()
			where x.materials.Any((Material y) => y.HasInt(NearVehicle))
			select x;
		_renderers.AddRange(collection);
	}

	public virtual void Update()
	{
		if (!(Time.time < _nextUpdateTime))
		{
			_nextUpdateTime = Time.time + 0.1f;
			Vector3 position = base.transform.position;
			Vector3 end = position;
			position.y += lowerHeight;
			end.y += upperHeight;
			SetIsNearItemToHide(Physics.CheckCapsule(position, end, detectionDistance, layersToHideFromWhenNear, QueryTriggerInteraction.Ignore));
		}
	}

	private void SetIsNearItemToHide(bool near)
	{
		if (_isNear == near)
		{
			return;
		}
		_isNear = near;
		foreach (Renderer renderer in _renderers)
		{
			if (!(renderer == null))
			{
				renderer.GetPropertyBlock(MaterialPropertyBlock);
				MaterialPropertyBlock.SetInt(NearVehicle, near ? 1 : 0);
				renderer.SetPropertyBlock(MaterialPropertyBlock);
			}
		}
		if (baseHuman.TryGetAttachedRenderersToTransform(baseHuman.rightHand, out var renderers))
		{
			foreach (Renderer item in renderers)
			{
				item.enabled = !near;
			}
		}
		if (!baseHuman.TryGetAttachedRenderersToTransform(baseHuman.leftHand, out var renderers2))
		{
			return;
		}
		foreach (Renderer item2 in renderers2)
		{
			item2.enabled = !near;
		}
	}

	public virtual void OnDrawGizmosSelected()
	{
		Vector3 position = base.transform.position;
		Vector3 center = position;
		position.y += lowerHeight;
		center.y += upperHeight;
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(center, detectionDistance);
		Gizmos.DrawWireSphere(position, detectionDistance);
		Gizmos.color = Color.white;
	}
}
