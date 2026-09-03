using System.Collections.Generic;
using Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Character.Customization;

public class ElementPicker : MonoBehaviour
{
	[SerializeField]
	private Transform entryTemplate;

	private readonly UnityEvent<int> _onElementSelected = new UnityEvent<int>();

	private GameObject _currentSelectedOutline;

	public void SetList(List<(int, Sprite, bool isSportsClothing)> elements, UnityAction<int> selectElement, int selectedElementIndex)
	{
		_onElementSelected.RemoveAllListeners();
		_onElementSelected.AddListener(selectElement);
		entryTemplate.ResetTemplate();
		foreach (var element in elements)
		{
			Transform transform = Object.Instantiate(entryTemplate, entryTemplate.parent);
			if ((bool)transform.Find("Icon"))
			{
				transform.GetImageByName("Icon").sprite = element.Item2;
			}
			else
			{
				transform.GetComponent<Image>().sprite = element.Item2;
			}
			Transform transform2 = transform.Find("SportsIcon");
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(element.isSportsClothing);
			}
			GameObject selectedOutline = transform.Find("Selected").gameObject;
			transform.GetComponent<Button>().onClick.AddListener(delegate
			{
				_currentSelectedOutline.SetActive(value: false);
				_currentSelectedOutline = selectedOutline;
				_currentSelectedOutline.SetActive(value: true);
				selectElement(element.Item1);
			});
			if (element.Item1 == selectedElementIndex)
			{
				_currentSelectedOutline = selectedOutline;
				_currentSelectedOutline.SetActive(value: true);
			}
			transform.gameObject.SetActive(value: true);
		}
		base.gameObject.SetActive(value: true);
	}
}
