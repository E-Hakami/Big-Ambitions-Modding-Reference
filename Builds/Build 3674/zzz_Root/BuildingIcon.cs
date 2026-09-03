using UnityEngine;
using UnityEngine.UI;

public class BuildingIcon : MonoBehaviour
{
	[SerializeField]
	private Image image;

	public void SetIcon(Sprite sprite)
	{
		image.sprite = sprite;
	}

	public void SetIconRotation(float angle)
	{
		image.transform.rotation = Quaternion.Euler(0f, 0f, angle);
	}
}
