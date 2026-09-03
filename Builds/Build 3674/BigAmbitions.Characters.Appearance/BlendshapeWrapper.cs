using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BigAmbitions.Characters.Appearance;

public class BlendshapeWrapper : MonoBehaviour
{
	[SerializeField]
	private AppearanceSetter appearanceSetter;

	[SerializeField]
	private List<FacialBlendshape> blendshapes = new List<FacialBlendshape>();

	private List<string> _changedBlendshapes = new List<string>();

	public FacialBlendshape GetByName(string blendshapeName)
	{
		return blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == blendshapeName);
	}

	private List<FacialBlendshape> GetByCategory(AppearanceElementType category)
	{
		return blendshapes.Where((FacialBlendshape bs) => bs.category == category).ToList();
	}

	private void Start()
	{
		if (!appearanceSetter || appearanceSetter.data?.blendshapes == null)
		{
			return;
		}
		foreach (FacialBlendshape blendshape in appearanceSetter.data.blendshapes)
		{
			FacialBlendshape byName = GetByName(blendshape.name);
			if (byName != null)
			{
				byName.value = blendshape.value;
			}
			else
			{
				blendshapes.Add(blendshape.Copy());
			}
		}
	}

	private void SyncBlendshapes()
	{
		if (!appearanceSetter || appearanceSetter.data == null)
		{
			return;
		}
		CharacterData data = appearanceSetter.data;
		if (data.blendshapes == null)
		{
			data.blendshapes = new List<FacialBlendshape>();
		}
		foreach (string changedBlendshapeName in _changedBlendshapes)
		{
			FacialBlendshape byName = GetByName(changedBlendshapeName);
			FacialBlendshape facialBlendshape = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == changedBlendshapeName);
			if (facialBlendshape != null)
			{
				facialBlendshape.value = byName.value;
			}
			else
			{
				appearanceSetter.data.blendshapes.Add(byName.Copy());
			}
		}
		_changedBlendshapes.Clear();
	}

	private void SetBlendshapeValue(string blendshapeName, float value)
	{
		FacialBlendshape byName = GetByName(blendshapeName);
		if (byName == null || Mathf.Approximately(byName.value, value))
		{
			return;
		}
		byName.value = value;
		_changedBlendshapes.Add(byName.name);
		if (byName.type != FacialBlendshape.BlendshapeType.Toggle)
		{
			return;
		}
		foreach (FacialBlendshape item in GetByCategory(byName.category))
		{
			if (!(item.name == blendshapeName) && item.type == FacialBlendshape.BlendshapeType.Toggle && !Mathf.Approximately(item.value, 0f))
			{
				item.value = 0f;
				_changedBlendshapes.Add(item.name);
			}
		}
	}

	private void ResetCategoryToggles(AppearanceElementType category)
	{
		foreach (FacialBlendshape item in GetByCategory(category))
		{
			if (item.type == FacialBlendshape.BlendshapeType.Toggle)
			{
				item.value = 0f;
				_changedBlendshapes.Add(item.name);
			}
		}
	}

	private void ResetCategory(AppearanceElementType category)
	{
		foreach (FacialBlendshape item in GetByCategory(category))
		{
			item.value = 0f;
			_changedBlendshapes.Add(item.name);
		}
	}

	public void HandleAppearanceBlendshapeVariant(AppearanceBlendshapeVariant variant)
	{
		if (variant.isDefault)
		{
			if (variant.category == AppearanceElementType.Head)
			{
				ResetHeadCategories();
			}
			else
			{
				ResetCategoryToggles(variant.category);
			}
			SyncBlendshapes();
			return;
		}
		foreach (FacialBlendshapeData blendshape in variant.blendshapes)
		{
			SetBlendshapeValue(blendshape.name, blendshape.value);
		}
		SyncBlendshapes();
	}

	private void ResetHeadCategories()
	{
		ResetCategory(AppearanceElementType.Head);
		ResetCategory(AppearanceElementType.Mouth);
		ResetCategory(AppearanceElementType.Eyes);
		ResetCategory(AppearanceElementType.Nose);
	}

	public void ResetAllBlendShapes()
	{
		foreach (AppearanceElementType value in Enum.GetValues(typeof(AppearanceElementType)))
		{
			ResetCategory(value);
			ResetCategoryToggles(value);
		}
		SyncBlendshapes();
	}
}
