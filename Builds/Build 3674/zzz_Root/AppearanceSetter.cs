using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using Character;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using MTAssets.SkinnedMeshCombiner;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class AppearanceSetter : MonoBehaviour
{
	private const int ProbabilityOfBeardAndHairColorMatching = 90;

	private static readonly AppearanceTag[] SwimmingAppearanceTags = new AppearanceTag[1] { AppearanceTag.SwimmingPool };

	public static readonly Dictionary<string, int> BlendshapeIndices = new Dictionary<string, int>();

	[SerializeField]
	public bool skipMeshMerging;

	[FormerlySerializedAs("meshLightLayerMask")]
	[FormerlySerializedAs("combinedMeshLightLayerMask")]
	[SerializeField]
	private uint meshRenderingLayerMask;

	[BoxGroup("References")]
	public SkinnedMeshCombiner meshCombiner;

	[SerializeField]
	private Animator animator;

	[BoxGroup("References Female")]
	public Transform femaleModel;

	[BoxGroup("References Female")]
	public SkinnedMeshRenderer femaleHeadMesh;

	[BoxGroup("References Female")]
	public BlendshapeWrapper femaleBlendshapes;

	[BoxGroup("References Female")]
	public Material femaleAgeDependingMaterial;

	[BoxGroup("References Male")]
	public Transform maleModel;

	[BoxGroup("References Male")]
	public SkinnedMeshRenderer maleHeadMesh;

	[BoxGroup("References Male")]
	public BlendshapeWrapper maleBlendshapes;

	[BoxGroup("References Male")]
	public Material maleAgeDependingMaterial;

	[BoxGroup("IK")]
	public RigBuilder rigBuilder;

	[BoxGroup("IK")]
	public Rig leftHandIKRig;

	[BoxGroup("IK")]
	public Transform leftHandIKTarget;

	[BoxGroup("IK")]
	public Rig rightHandIKRig;

	[BoxGroup("IK")]
	public Transform rightHandIKTarget;

	[BoxGroup("IK")]
	public Rig headIKRig;

	[BoxGroup("IK")]
	public Transform headIKTarget;

	private readonly Dictionary<AppearanceElementType, SkinnedMeshRenderer> _selectedMeshRenderers = new Dictionary<AppearanceElementType, SkinnedMeshRenderer>();

	private readonly List<SkinnedMeshRenderer> _selectedMeshRenderersList = new List<SkinnedMeshRenderer>();

	[NonSerialized]
	public CharacterData data = new CharacterData();

	[NonSerialized]
	public Action visualsUpdated;

	private float _fatness;

	private float _slimness;

	private float _muscularity;

	[NonSerialized]
	public SkinnedMeshRenderer lastMergedMeshRenderer;

	private PixelatedCube _pixelatedCube;

	public static readonly int FatId = Animator.StringToHash("Fat");

	private static readonly int MaskColorRedId = Shader.PropertyToID("_MaskColorRed");

	private static readonly int MaskColorSpecularRedId = Shader.PropertyToID("_SpecularColorRed");

	private static readonly int MaskColorGreenId = Shader.PropertyToID("_MaskColorGreen");

	private static readonly int MaskColorSpecularGreenId = Shader.PropertyToID("_SpecularColorGreen");

	private static readonly int MaskColorBlueId = Shader.PropertyToID("_MaskColorBlue");

	private static readonly int MaskColorSpecularBlueId = Shader.PropertyToID("_SpecularColorBlue");

	private static readonly int MaskColorAlphaId = Shader.PropertyToID("_MaskColorAlpha");

	private static readonly int MaskColorSpecularAlphaId = Shader.PropertyToID("_SpecularColorAlpha");

	private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

	private static readonly int IndexMapId = Shader.PropertyToID("_IndexMap");

	private static readonly int SpecularColorId = Shader.PropertyToID("_SpecularColor");

	private static readonly List<Material> Materials = new List<Material>();

	private static AppearanceElementType[] AppearanceElementTypes;

	private Transform CurrentGenderModel
	{
		get
		{
			if (data.gender != Gender.Female)
			{
				return maleModel;
			}
			return femaleModel;
		}
	}

	public static AppearanceElementType[] GetAppearanceElementTypes
	{
		get
		{
			if (AppearanceElementTypes == null)
			{
				AppearanceElementTypes = (AppearanceElementType[])Enum.GetValues(typeof(AppearanceElementType));
				AppearanceElementTypes = AppearanceElementTypes.Where((AppearanceElementType x) => x != AppearanceElementType.Gender && x != AppearanceElementType.Eyes && x != AppearanceElementType.Mouth && x != AppearanceElementType.Nose).ToArray();
			}
			return AppearanceElementTypes;
		}
	}

	public BlendshapeWrapper Blendshapes
	{
		get
		{
			if (data.gender != Gender.Male)
			{
				return femaleBlendshapes;
			}
			return maleBlendshapes;
		}
	}

	public SkinnedMeshRenderer HeadMesh
	{
		get
		{
			if (data.gender != Gender.Male)
			{
				return femaleHeadMesh;
			}
			return maleHeadMesh;
		}
	}

	public Material AgeDependingMaterial
	{
		get
		{
			if (data.gender != Gender.Male)
			{
				return femaleAgeDependingMaterial;
			}
			return maleAgeDependingMaterial;
		}
	}

	public void SetRandomAppearance(AppearanceTag[] tags = null)
	{
		Gender randomEnum = RngHelper.GetRandomEnum<Gender>();
		SetRandomAppearance(randomEnum, tags);
	}

	public void SetRandomAppearance(Gender gender, int ageInDays, int seed)
	{
		SkinColor random = SkinColorHelper.GetRandom(seed);
		data.ageInDays = ageInDays;
		SetRandomAppearance(gender, random, null, seed);
	}

	public void SetRandomAppearance(Gender gender, AppearanceTag[] tags = null)
	{
		SkinColor random = SkinColorHelper.GetRandom();
		SetRandomAppearance(gender, random, tags);
	}

	public void SetRandomAppearance(Gender gender, SkinColor skinColor, AppearanceTag[] tags = null, int seed = -1)
	{
		RandomizeElements(gender, skinColor, tags, seed);
		UpdateVisuals(data.elements);
	}

	public void RandomizeElements(Gender gender, SkinColor skinColor, AppearanceTag[] tags, int seed = -1)
	{
		if (seed == -1)
		{
			seed = UnityEngine.Random.Range(0, int.MaxValue);
		}
		System.Random random = new System.Random(seed);
		data.gender = gender;
		data.color = skinColor.color;
		data.strength = (float)random.Next(0, 100) / 100f;
		data.fatness = (float)random.Next(0, 100) / 100f;
		AppearanceElementType[] getAppearanceElementTypes = GetAppearanceElementTypes;
		foreach (AppearanceElementType elementType in getAppearanceElementTypes)
		{
			RandomizeElement(elementType, tags, randomizeColor: true, excludeCurrentVariant: false, random);
		}
	}

	public void RandomizeClothes(AppearanceTag[] tags = null, int seed = -1)
	{
		if (seed == -1)
		{
			seed = UnityEngine.Random.Range(0, int.MaxValue);
		}
		System.Random random = new System.Random(seed);
		AppearanceElementType[] getAppearanceElementTypes = GetAppearanceElementTypes;
		foreach (AppearanceElementType appearanceElementType in getAppearanceElementTypes)
		{
			if (appearanceElementType != AppearanceElementType.Head && appearanceElementType != AppearanceElementType.Hair)
			{
				RandomizeElement(appearanceElementType, tags, randomizeColor: true, excludeCurrentVariant: false, random);
			}
		}
		UpdateVisuals(data.elements);
	}

	public void SetAppearance(CharacterData newData = null)
	{
		if (newData != null)
		{
			data = newData;
		}
		UpdateVisuals();
	}

	public void UpdateElements(List<AppearanceElementData> elements)
	{
		if (elements == null || elements.Count == 0)
		{
			return;
		}
		data.elements.RemoveAll((AppearanceElementData x) => elements.Exists((AppearanceElementData y) => y.type == x.type));
		data.elements.AddRange(elements.Copy());
		UpdateVisuals();
	}

	public void RandomizeElement(AppearanceElementType elementType, AppearanceTag[] tags, bool randomizeColor = true, bool excludeCurrentVariant = false, System.Random random = null, bool skipColorMatch = false)
	{
		List<AppearanceElementVariant> elementVariants = GetElementVariants(elementType, tags);
		if (elementVariants == null || elementVariants.Count == 0)
		{
			return;
		}
		if (random == null)
		{
			random = new System.Random();
		}
		AppearanceElementData dataElement = data.elements.FirstOrDefault((AppearanceElementData x) => x.type == elementType);
		if (dataElement == null)
		{
			dataElement = new AppearanceElementData
			{
				type = elementType
			};
			data.elements.Add(dataElement);
		}
		if (excludeCurrentVariant)
		{
			elementVariants.RemoveAll((AppearanceElementVariant x) => dataElement.variantId == x.id);
		}
		AppearanceElementVariant appearanceElementVariant = null;
		int num = elementVariants.Sum((AppearanceElementVariant x) => (int)x.probability);
		if (num > 0)
		{
			int num2 = random.Next(num);
			int num3 = 0;
			foreach (AppearanceElementVariant item in elementVariants)
			{
				num3 = (int)(num3 + item.probability);
				if (num3 >= num2)
				{
					appearanceElementVariant = item;
					break;
				}
			}
		}
		if ((object)appearanceElementVariant == null)
		{
			appearanceElementVariant = elementVariants[random.Next(elementVariants.Count)];
		}
		dataElement.variantId = appearanceElementVariant.id;
		if (appearanceElementVariant is AppearanceBlendshapeVariant appearanceBlendshapeVariant)
		{
			Blendshapes.HandleAppearanceBlendshapeVariant(appearanceBlendshapeVariant);
			if (elementType == AppearanceElementType.Head)
			{
				SetHeadElementBlendshape(AppearanceElementType.Eyes, appearanceBlendshapeVariant);
				SetHeadElementBlendshape(AppearanceElementType.Mouth, appearanceBlendshapeVariant);
				SetHeadElementBlendshape(AppearanceElementType.Nose, appearanceBlendshapeVariant);
				SetHeadElementBlendshape(AppearanceElementType.Eyebrows, appearanceBlendshapeVariant);
			}
		}
		if (!randomizeColor)
		{
			return;
		}
		if (!skipColorMatch && (elementType == AppearanceElementType.Eyebrows || (elementType == AppearanceElementType.Beard && UnityEngine.Random.Range(0, 100) < 90)))
		{
			AppearanceElementData hairData = data.elements.FirstOrDefault((AppearanceElementData y) => y.type == AppearanceElementType.Hair);
			AppearanceElementColor hairColor = GetElementVariants(AppearanceElementType.Hair).FirstOrDefault((AppearanceElementVariant x) => x.id == hairData?.variantId)?.colors.FirstOrDefault((AppearanceElementColor x) => x.id == hairData?.colorId);
			dataElement.colorId = ((hairColor == null) ? null : appearanceElementVariant.colors.FirstOrDefault((AppearanceElementColor x) => x.colorIcon == hairColor.colorIcon)?.id);
			if (dataElement.colorId == null)
			{
				RandomizeElementColor(dataElement, appearanceElementVariant, random);
			}
		}
		else
		{
			RandomizeElementColor(dataElement, appearanceElementVariant, random);
		}
	}

	public void RandomizeElementColor(AppearanceElementType elementType, AppearanceTag[] tags, System.Random random = null)
	{
		List<AppearanceElementVariant> elementVariants = GetElementVariants(elementType, tags);
		if (elementVariants != null && elementVariants.Count != 0)
		{
			if (random == null)
			{
				random = new System.Random();
			}
			AppearanceElementData dataElement = GetCurrentDataElement(elementType);
			AppearanceElementVariant selectedVariant = elementVariants.FirstOrDefault((AppearanceElementVariant x) => x.id == dataElement.variantId);
			RandomizeElementColor(dataElement, selectedVariant, random);
		}
	}

	private void RandomizeElementColor(AppearanceElementData dataElement, AppearanceElementVariant selectedVariant, System.Random random)
	{
		if (selectedVariant == null)
		{
			return;
		}
		AppearanceElementColor[] colors = selectedVariant.colors;
		if (colors.Length == 0)
		{
			return;
		}
		AppearanceElementColor appearanceElementColor = null;
		int num = colors.Sum((AppearanceElementColor x) => (int)x.probability);
		if (num > 0)
		{
			int num2 = random.Next(num);
			int num3 = 0;
			AppearanceElementColor[] array = colors;
			foreach (AppearanceElementColor appearanceElementColor2 in array)
			{
				num3 = (int)(num3 + appearanceElementColor2.probability);
				if (num3 >= num2)
				{
					appearanceElementColor = appearanceElementColor2;
					break;
				}
			}
		}
		if (appearanceElementColor == null)
		{
			appearanceElementColor = colors[random.Next(colors.Length)];
		}
		dataElement.colorId = appearanceElementColor.id;
	}

	private AppearanceElementData GetCurrentDataElement(AppearanceElementType elementType)
	{
		AppearanceElementData appearanceElementData = data.elements.FirstOrDefault((AppearanceElementData x) => x.type == elementType);
		if (appearanceElementData == null)
		{
			appearanceElementData = new AppearanceElementData
			{
				type = elementType
			};
			data.elements.Add(appearanceElementData);
		}
		return appearanceElementData;
	}

	public List<AppearanceElementVariant> GetElementVariants(AppearanceElementType elementType, AppearanceTag[] tags = null)
	{
		Transform transform = CurrentGenderModel.Find(elementType.ToStringFast());
		if (transform == null)
		{
			return null;
		}
		List<AppearanceElementVariant> list = transform.GetComponentsInChildren<AppearanceElementVariant>(includeInactive: true).ToList();
		if (tags != null && tags.Length > 0)
		{
			list = list.Where((AppearanceElementVariant x) => x.tags.Any((AppearanceTag t) => t == AppearanceTag.All || tags.Contains(t))).ToList();
		}
		return list;
	}

	public void UpdateVisuals(List<AppearanceElementData> elements = null)
	{
		if (elements != null)
		{
			elements = GetCopyOfElements(elements);
			IEnumerable<AppearanceElementData> collection = data.elements.Where((AppearanceElementData element) => !elements.Exists((AppearanceElementData x) => x.type == element.type));
			elements.AddRange(collection);
		}
		else
		{
			elements = data.elements;
		}
		if (meshCombiner != null && meshCombiner.isMeshesCombined())
		{
			meshCombiner.UndoCombineMeshes(runUnityGc: false, runMonoIl2CppGc: false);
		}
		SetBody();
		_selectedMeshRenderers.Clear();
		_selectedMeshRenderersList.Clear();
		SetElementVariant(AppearanceElementType.Hair, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.Hair));
		SetElementVariant(AppearanceElementType.HairAccessory, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.HairAccessory));
		SetElementVariant(AppearanceElementType.Beard, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.Beard));
		SetElementVariant(AppearanceElementType.Eyebrows, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.Eyebrows));
		SetElementVariant(AppearanceElementType.HeadAccessory, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.HeadAccessory));
		SetElementVariant(AppearanceElementType.Torso, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.Torso));
		SetElementVariant(AppearanceElementType.TorsoAccessory, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.TorsoAccessory));
		SetElementVariant(AppearanceElementType.Legs, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.Legs));
		SetElementVariant(AppearanceElementType.LegsAccessory, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.LegsAccessory));
		SetElementVariant(AppearanceElementType.Feet, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.Feet));
		SetElementVariant(AppearanceElementType.FeetAccessory, elements.FirstOrDefault((AppearanceElementData x) => x.type == AppearanceElementType.FeetAccessory));
		UpdateHeadBlendshapes();
		if (_selectedMeshRenderers.Count == 0)
		{
			Debug.LogWarning($"No elements found for character {data.name}, gender {data.gender}. Randomizing appearance");
			SetRandomAppearance();
			return;
		}
		this.SetAgeInDays(data.ageInDays);
		if (!skipMeshMerging)
		{
			meshCombiner.CombineMeshes();
			lastMergedMeshRenderer = meshCombiner.GetCombinedMeshSkinnedMeshRenderer();
			meshCombiner.resultMergeGameObject.layer = LayerHelper.HumanLayerIndex;
		}
		visualsUpdated?.Invoke();
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if ((bool)lastMergedMeshRenderer)
			{
				lastMergedMeshRenderer.renderingLayerMask = meshRenderingLayerMask;
				lastMergedMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			}
		});
	}

	private static List<AppearanceElementData> GetCopyOfElements(List<AppearanceElementData> elements)
	{
		List<AppearanceElementData> list = new List<AppearanceElementData>(elements.Count);
		foreach (AppearanceElementData element in elements)
		{
			AppearanceElementData appearanceElementData = new AppearanceElementData();
			appearanceElementData.Copy(element);
			list.Add(appearanceElementData);
		}
		return list;
	}

	public List<AppearanceElementData> GetCopyOfCurrentElements()
	{
		return GetCopyOfElements(data.elements);
	}

	public bool IsWearingClothesWithTags(AppearanceTag[] tags)
	{
		if (IsWearingElementWithTags(AppearanceElementType.Torso, tags) && IsWearingElementWithTags(AppearanceElementType.Legs, tags))
		{
			return IsWearingElementWithTags(AppearanceElementType.Feet, tags);
		}
		return false;
	}

	private bool IsWearingElementWithTags(AppearanceElementType type, AppearanceTag[] tags)
	{
		string selectedVariantId = data.elements.FirstOrDefault((AppearanceElementData y) => y.type == type)?.variantId;
		if (string.IsNullOrEmpty(selectedVariantId))
		{
			return true;
		}
		AppearanceElementVariant appearanceElementVariant = GetElementVariants(type).FirstOrDefault((AppearanceElementVariant x) => x.id == selectedVariantId);
		if (!(appearanceElementVariant == null))
		{
			return appearanceElementVariant.tags.Any((AppearanceTag x) => x == AppearanceTag.All || tags.Contains(x));
		}
		return true;
	}

	public void SetBody()
	{
		femaleModel.gameObject.SetActive(data.gender == Gender.Female);
		maleModel.gameObject.SetActive(data.gender == Gender.Male);
		_muscularity = data.strength * 100f;
		_fatness = Mathf.Max(0f, (data.fatness - 0.5f) * 100f * 2f);
		_slimness = Mathf.Max(0f, (0.5f - data.fatness) * 100f * 2f);
		SetFatnessValueOnAnimator();
	}

	public void SetFatnessValueOnAnimator()
	{
		animator.SetFloat(FatId, _fatness / 100f);
	}

	private void SetElementVariant(AppearanceElementType elementType, AppearanceElementData elementData)
	{
		List<AppearanceElementVariant> elementVariants = GetElementVariants(elementType);
		if (elementVariants == null || elementVariants.Count < 1)
		{
			return;
		}
		string selectedColorId = "";
		string text;
		if (elementData == null)
		{
			text = elementVariants[0].id;
			AppearanceElementColor[] colors = elementVariants[0].colors;
			if (colors != null && colors.Length > 0)
			{
				selectedColorId = elementVariants[0].colors[0].id;
			}
		}
		else
		{
			text = elementData.variantId;
			selectedColorId = elementData.colorId;
		}
		string text2 = null;
		foreach (AppearanceElementVariant item in elementVariants)
		{
			if (text == item.id)
			{
				item.transform.gameObject.SetActive(value: true);
				SkinnedMeshRenderer component = item.transform.GetComponent<SkinnedMeshRenderer>();
				if (component == null)
				{
					continue;
				}
				_selectedMeshRenderers.Add(elementType, component);
				_selectedMeshRenderersList.Add(component);
				component.renderingLayerMask = meshRenderingLayerMask;
				if (component.sharedMesh != null && component.sharedMesh.blendShapeCount > 0)
				{
					if (component.sharedMesh.blendShapeCount > 0)
					{
						component.SetBlendShapeWeight(0, _fatness);
					}
					if (component.sharedMesh.blendShapeCount > 1)
					{
						component.SetBlendShapeWeight(1, _slimness);
					}
					if (component.sharedMesh.blendShapeCount > 2)
					{
						component.SetBlendShapeWeight(2, _muscularity);
					}
					if (component.sharedMesh.blendShapeCount > 11 && elementType == AppearanceElementType.Head && data.blendshapes != null)
					{
						foreach (FacialBlendshape blendshape in data.blendshapes)
						{
							if (!BlendshapeIndices.TryGetValue(blendshape.name, out var value))
							{
								value = component.sharedMesh.GetBlendShapeIndex(blendshape.name);
								BlendshapeIndices.Add(blendshape.name, value);
							}
							if (value > 2)
							{
								component.SetBlendShapeWeight(value, blendshape.value);
							}
						}
					}
					else if (elementType == AppearanceElementType.Beard && data.blendshapes != null)
					{
						foreach (FacialBlendshape blendshape2 in data.blendshapes)
						{
							int blendShapeIndex = component.sharedMesh.GetBlendShapeIndex(blendshape2.name);
							if (blendShapeIndex > 2)
							{
								component.SetBlendShapeWeight(blendShapeIndex, blendshape2.value);
							}
						}
					}
				}
				if (item.hasCustomBlendshapeModifier)
				{
					SetElementTypeBlendshape(item.elementTypeThatModifies, item.customBlendshapeModifier, 100);
					text2 = item.customBlendshapeModifier;
				}
				ApplySkinColorToVariantMesh(data.color, item, component);
				if (item.colors.Length == 0)
				{
					continue;
				}
				AppearanceElementColor appearanceElementColor = item.colors.FirstOrDefault((AppearanceElementColor x) => x.id == selectedColorId);
				if (appearanceElementColor == null)
				{
					continue;
				}
				ApplyColorsToVariantMesh(appearanceElementColor, item, component);
				if (elementType == AppearanceElementType.Hair)
				{
					((data.gender == Gender.Male) ? maleHeadMesh : femaleHeadMesh).GetMaterials(Materials);
					if (Materials.Count > 2)
					{
						Materials[2].SetColor(BaseColorId, appearanceElementColor.baseColor);
					}
				}
			}
			else
			{
				if (item.hasCustomBlendshapeModifier && item.customBlendshapeModifier != text2)
				{
					SetElementTypeBlendshape(item.elementTypeThatModifies, item.customBlendshapeModifier, 0);
				}
				item.gameObject.SetActive(value: false);
			}
		}
	}

	private void SetElementTypeBlendshape(AppearanceElementType elementType, string blendshapeName, int blendshapeValue)
	{
		if (_selectedMeshRenderers.TryGetValue(elementType, out var value))
		{
			if (!(value.sharedMesh == null))
			{
				int blendShapeIndex = value.sharedMesh.GetBlendShapeIndex(blendshapeName);
				if (blendShapeIndex != -1)
				{
					value.SetBlendShapeWeight(blendShapeIndex, blendshapeValue);
				}
			}
		}
		else
		{
			Debug.LogError(blendshapeName + " affects " + elementType.ToStringFast() + " but is not assigned yet");
		}
	}

	private void UpdateHeadBlendshapes()
	{
		SkinnedMeshRenderer skinnedMeshRenderer = ((data.gender == Gender.Male) ? maleHeadMesh : femaleHeadMesh);
		if (skinnedMeshRenderer == null)
		{
			return;
		}
		skinnedMeshRenderer.renderingLayerMask = meshRenderingLayerMask;
		skinnedMeshRenderer.SetBlendShapeWeight(0, _fatness);
		skinnedMeshRenderer.SetBlendShapeWeight(1, _slimness);
		skinnedMeshRenderer.SetBlendShapeWeight(2, _muscularity);
		skinnedMeshRenderer.GetMaterials(Materials);
		if (Materials.Count > 1)
		{
			Materials[1].SetColor(MaskColorRedId, data.eyesColor);
		}
		if (skinnedMeshRenderer.sharedMesh.blendShapeCount <= 11 || data.blendshapes == null)
		{
			return;
		}
		foreach (FacialBlendshape blendshape in data.blendshapes)
		{
			if (!BlendshapeIndices.TryGetValue(blendshape.name, out var value))
			{
				value = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendshape.name);
				BlendshapeIndices.Add(blendshape.name, value);
			}
			if (value > 2)
			{
				skinnedMeshRenderer.SetBlendShapeWeight(value, blendshape.value);
			}
		}
	}

	public static void ApplySkinColorToVariantMesh(Color32 color, AppearanceElementVariant variant, SkinnedMeshRenderer variantMesh, bool useSharedMaterials = false)
	{
		if (variant.skinMaterialIndex >= 0)
		{
			if (useSharedMaterials)
			{
				variantMesh.GetSharedMaterials(Materials);
			}
			else
			{
				variantMesh.GetMaterials(Materials);
			}
			if (variant.skinMaterialIndex < -1 || variant.skinMaterialIndex >= Materials.Count)
			{
				Debug.LogError("Variant " + variant.name + " has wrong skin material index", variant.transform);
			}
			else if (Materials[variant.skinMaterialIndex].HasColor(MaskColorRedId))
			{
				Materials[variant.skinMaterialIndex].SetColor(MaskColorRedId, color);
			}
		}
	}

	public static void ApplyColorsToVariantMesh(AppearanceElementColor color, AppearanceElementVariant variant, SkinnedMeshRenderer variantMesh, bool useSharedMaterials = false)
	{
		if (useSharedMaterials)
		{
			variantMesh.GetSharedMaterials(Materials);
		}
		else
		{
			variantMesh.GetMaterials(Materials);
		}
		for (int i = 0; i < Materials.Count; i++)
		{
			if (i == variant.skinMaterialIndex)
			{
				continue;
			}
			Material material = Materials[i];
			if (!material)
			{
				continue;
			}
			if (color.hasTexture)
			{
				material.SetFloat(IndexMapId, color.textureId);
				material.SetColor(SpecularColorId, color.specularColor);
				continue;
			}
			if (color.hasBaseColor && material.HasColor(BaseColorId))
			{
				material.SetColor(BaseColorId, color.baseColor);
			}
			if (color.hasChannelR)
			{
				if (material.HasColor(MaskColorRedId))
				{
					material.SetColor(MaskColorRedId, color.r);
				}
				if (material.HasColor(MaskColorSpecularRedId))
				{
					material.SetColor(MaskColorSpecularRedId, color.specularR);
				}
			}
			if (color.hasChannelG)
			{
				if (material.HasColor(MaskColorGreenId))
				{
					material.SetColor(MaskColorGreenId, color.g);
				}
				if (material.HasColor(MaskColorSpecularGreenId))
				{
					material.SetColor(MaskColorSpecularGreenId, color.specularG);
				}
			}
			if (color.hasChannelB)
			{
				if (material.HasColor(MaskColorBlueId))
				{
					material.SetColor(MaskColorBlueId, color.b);
				}
				if (material.HasColor(MaskColorSpecularBlueId))
				{
					material.SetColor(MaskColorSpecularBlueId, color.specularB);
				}
			}
			if (color.hasChannelA)
			{
				if (material.HasColor(MaskColorAlphaId))
				{
					material.SetColor(MaskColorAlphaId, color.a);
				}
				if (material.HasColor(MaskColorSpecularAlphaId))
				{
					material.SetColor(MaskColorSpecularAlphaId, color.specularA);
				}
			}
		}
	}

	public void SetHeadElementBlendshape(AppearanceElementType elementType, AppearanceBlendshapeVariant headVariant)
	{
		List<AppearanceElementVariant> elementVariants = GetElementVariants(elementType);
		if (elementVariants == null || elementVariants.Count == 0)
		{
			return;
		}
		AppearanceElementVariant appearanceElementVariant = elementVariants.FirstOrDefault((AppearanceElementVariant x) => x is AppearanceBlendshapeVariant appearanceBlendshapeVariant && ((appearanceBlendshapeVariant.isDefault && headVariant.isDefault) || appearanceBlendshapeVariant.blendshapes.Any((FacialBlendshapeData y) => headVariant.blendshapes.Any((FacialBlendshapeData z) => y.name == z.name && Math.Abs(y.value - z.value) < 0.01f))));
		if (!(appearanceElementVariant == null))
		{
			AppearanceElementData appearanceElementData = data.elements.FirstOrDefault((AppearanceElementData x) => x.type == elementType);
			if (appearanceElementData == null)
			{
				appearanceElementData = new AppearanceElementData
				{
					type = elementType
				};
				data.elements.Add(appearanceElementData);
			}
			appearanceElementData.variantId = appearanceElementVariant.id;
		}
	}

	public bool IsBald()
	{
		return GetCurrentDataElement(AppearanceElementType.Hair).colorId == null;
	}

	public void SetNakedAppearance()
	{
		SetNakedElement(AppearanceElementType.HeadAccessory);
		SetNakedElement(AppearanceElementType.HairAccessory);
		SetNakedElement(AppearanceElementType.Torso);
		SetNakedElement(AppearanceElementType.TorsoAccessory);
		SetNakedElement(AppearanceElementType.Legs);
		SetNakedElement(AppearanceElementType.LegsAccessory);
		SetNakedElement(AppearanceElementType.Feet);
		SetNakedElement(AppearanceElementType.FeetAccessory);
		UpdateVisuals();
	}

	public void SetNakedElement(AppearanceElementType appearanceElementType)
	{
		AppearanceElementVariant appearanceElementVariant = GetElementVariants(appearanceElementType).FirstOrDefault((AppearanceElementVariant x) => x.colors == null || x.colors.Length == 0);
		AppearanceElementData appearanceElementData = data.elements.FirstOrDefault((AppearanceElementData x) => x.type == appearanceElementType);
		if (appearanceElementData != null)
		{
			appearanceElementData.variantId = appearanceElementVariant?.id;
		}
	}

	public void SetPixelatedPlaneActive(bool active)
	{
		if (!active)
		{
			if ((bool)_pixelatedCube)
			{
				_pixelatedCube.Disable();
			}
			return;
		}
		if (!_pixelatedCube)
		{
			_pixelatedCube = PrefabHelper.CreatePrefab<PixelatedCube>("PixelatedCube");
			_pixelatedCube.transform.SetParent(base.transform, worldPositionStays: false);
		}
		_pixelatedCube.Enable();
	}

	public List<AppearanceElementData> GetSwimwearElements()
	{
		List<AppearanceElementData> list = new List<AppearanceElementData>();
		AppearanceElementVariant appearanceElementVariant = GetElementVariants(AppearanceElementType.HeadAccessory).FirstOrDefault((AppearanceElementVariant x) => x.colors == null || x.colors.Length == 0);
		if ((bool)appearanceElementVariant)
		{
			list.Add(new AppearanceElementData
			{
				type = AppearanceElementType.HeadAccessory,
				variantId = appearanceElementVariant.id
			});
		}
		AppearanceElementData firstAppearanceElementData = GetFirstAppearanceElementData(AppearanceElementType.Torso, SwimmingAppearanceTags);
		list.Add(firstAppearanceElementData);
		AppearanceElementData firstAppearanceElementData2 = GetFirstAppearanceElementData(AppearanceElementType.Legs, SwimmingAppearanceTags);
		list.Add(firstAppearanceElementData2);
		AppearanceElementData firstAppearanceElementData3 = GetFirstAppearanceElementData(AppearanceElementType.Feet, SwimmingAppearanceTags);
		list.Add(firstAppearanceElementData3);
		return list;
	}

	private AppearanceElementData GetFirstAppearanceElementData(AppearanceElementType appearanceElementType, AppearanceTag[] appearanceTags)
	{
		AppearanceElementVariant appearanceElementVariant = GetElementVariants(appearanceElementType, appearanceTags).FirstOrDefault();
		return new AppearanceElementData
		{
			type = appearanceElementType,
			variantId = appearanceElementVariant?.id,
			colorId = appearanceElementVariant?.colors.FirstOrDefault()?.id
		};
	}

	public IReadOnlyList<SkinnedMeshRenderer> GetSelectedMeshRenderers()
	{
		return _selectedMeshRenderersList;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		BlendshapeIndices.Clear();
	}
}
