using NaughtyAttributes;
using UnityEngine;

public class ScooterSpawnerGenerator : MonoBehaviour
{
	public GameObject spawnerPrefab;

	public Vector2 size = Vector2.zero;

	public Vector2 distance = Vector2.one;

	[MinMaxSlider(0f, 360f)]
	public Vector2 rotationRange = Vector2.zero;

	public Vector2 offset = Vector2.one * 0.5f;

	public static bool editBounds_EDITOR;

	[Button("Edit Bounds", EButtonEnableMode.Always)]
	public void EditBounds()
	{
		editBounds_EDITOR = !editBounds_EDITOR;
	}
}
