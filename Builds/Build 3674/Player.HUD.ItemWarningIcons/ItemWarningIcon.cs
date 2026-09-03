using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemWarningIcons;

public class ItemWarningIcon : MonoBehaviour
{
	[ReadOnly]
	public ItemController linkedItemController;

	[ReadOnly]
	public WarningIconType currentIconType;

	public Image icon;

	public Image background;

	[SerializeField]
	private GameObject pointer;

	[HideInInspector]
	public RectTransform rectTransform;

	public GameObject Pointer => pointer;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}
}
