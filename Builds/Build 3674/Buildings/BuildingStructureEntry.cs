using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Buildings;

[Serializable]
public sealed class BuildingStructureEntry
{
	[SerializeField]
	[HideInInspector]
	private int version;

	[SerializeField]
	private AssetReferenceGameObject prefabReference = new AssetReferenceGameObject("");

	[SerializeField]
	private BuildingPreloadMode preloadMode;

	[SerializeField]
	[Range(0f, 100f)]
	private int preloadPriority;

	public Vector3 localPosition;

	public Quaternion localRotation = Quaternion.identity;

	public Vector3 localScale = Vector3.one;

	public int Version => version;

	public AssetReferenceGameObject PrefabReference => prefabReference;

	public BuildingPreloadMode PreloadMode => preloadMode;

	public int PreloadPriority => preloadPriority;
}
