using System.Collections.Generic;
using System.Linq;
using Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Character.Customization;

public class AppearanceColorPicker : MonoBehaviour
{
	[SerializeField]
	private Transform entryTemplate;

	[SerializeField]
	private GameObject hideWhenEmpty;

	private readonly UnityEvent<Color32> _onColorSelected = new UnityEvent<Color32>();

	private readonly UnityEvent<int> _onSpriteSelected = new UnityEvent<int>();

	private GameObject _currentSelectedOutline;

	public void SetList(IEnumerable<Color32> colors, UnityAction<Color32> selectColor, Color32 selectedColor)
	{
		_onColorSelected.RemoveAllListeners();
		_onColorSelected.AddListener(selectColor);
		entryTemplate.ResetTemplate();
		foreach (Color32 color in colors.ToList())
		{
			Transform transform = Object.Instantiate(entryTemplate, entryTemplate.parent);
			transform.GetImageByName("Sprite").color = color;
			GameObject selectedOutline = transform.Find("Selected").gameObject;
			transform.GetComponent<Button>().onClick.AddListener(delegate
			{
				_currentSelectedOutline.SetActive(value: false);
				_currentSelectedOutline = selectedOutline;
				_currentSelectedOutline.SetActive(value: true);
				_onColorSelected.Invoke(color);
			});
			if (color.Equals(selectedColor))
			{
				_currentSelectedOutline = selectedOutline;
				_currentSelectedOutline.SetActive(value: true);
			}
			transform.gameObject.SetActive(value: true);
		}
		hideWhenEmpty?.SetActive(value: true);
		base.gameObject.SetActive(value: true);
	}

	public void SetList(List<Sprite> sprites, UnityAction<int> selectSprite, int selectedSprite)
	{
		_onSpriteSelected.RemoveAllListeners();
		_onSpriteSelected.AddListener(selectSprite);
		entryTemplate.ResetTemplate();
		for (int i = 0; i < sprites.Count; i++)
		{
			int currentSpriteIndex = i;
			Transform transform = Object.Instantiate(entryTemplate, entryTemplate.parent);
			transform.GetImageByName("Sprite").sprite = sprites[currentSpriteIndex];
			GameObject selectedOutline = transform.Find("Selected").gameObject;
			transform.GetComponent<Button>().onClick.AddListener(delegate
			{
				_currentSelectedOutline.SetActive(value: false);
				_currentSelectedOutline = selectedOutline;
				_currentSelectedOutline.SetActive(value: true);
				_onSpriteSelected.Invoke(currentSpriteIndex);
			});
			if (currentSpriteIndex == selectedSprite)
			{
				_currentSelectedOutline = selectedOutline;
				_currentSelectedOutline.SetActive(value: true);
			}
			transform.gameObject.SetActive(value: true);
		}
		if (hideWhenEmpty != null)
		{
			hideWhenEmpty.SetActive(value: true);
		}
		base.gameObject.SetActive(value: true);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
		if (hideWhenEmpty != null)
		{
			hideWhenEmpty.SetActive(value: false);
		}
	}
}
