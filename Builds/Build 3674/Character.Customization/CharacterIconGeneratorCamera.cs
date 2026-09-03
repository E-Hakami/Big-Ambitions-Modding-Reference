using System;
using BigAmbitions.Characters;
using NaughtyAttributes;
using UnityEngine;

namespace Character.Customization;

[ExecuteInEditMode]
public class CharacterIconGeneratorCamera : MonoBehaviour
{
	public bool isGenderCamera;

	[ShowIf("isGenderCamera")]
	public BigAmbitions.Characters.Gender gender;

	public string[] transformNames;

	private MeshRenderer _backdrop;

	public Camera Camera { get; private set; }

	private void Awake()
	{
		Camera = GetComponent<Camera>();
		_backdrop = base.transform.GetComponentInChildren<MeshRenderer>();
		if (_backdrop != null)
		{
			_backdrop.enabled = false;
		}
	}

	public void SetBackdropVisible(bool visible)
	{
		if (!(_backdrop == null))
		{
			_backdrop.enabled = visible;
			if (visible)
			{
				ScaleBackdrop();
			}
		}
	}

	public void SetBackdropColor(Color color)
	{
		if (!(_backdrop == null))
		{
			_backdrop.sharedMaterial.color = color;
		}
	}

	private void ScaleBackdrop()
	{
		if (!(_backdrop == null))
		{
			float z = _backdrop.transform.localPosition.z;
			float num = 2f * z * Mathf.Tan(Camera.fieldOfView * 0.5f * (MathF.PI / 180f));
			_backdrop.transform.localScale = new Vector3(num, num, 1f);
		}
	}
}
