using System.Collections.Generic;
using Localizor.LanguageChangeEvent;
using UnityEngine;

public class HelpCategory : MonoBehaviour
{
	public HelpPageLink pageTemplate;

	public TextLocalizationComponent categoryNameLabel;

	public List<HelpPageLink> links = new List<HelpPageLink>();

	[SerializeField]
	private GameObject pagesContainer;

	[SerializeField]
	private GameObject pagesBackground;

	[SerializeField]
	private Transform arrow;

	private bool _isOpen;

	public void ToggleOpenState()
	{
		SetOpenState(!_isOpen);
	}

	public void SetOpenState(bool isOpen)
	{
		_isOpen = isOpen;
		pagesContainer.SetActive(_isOpen);
		pagesBackground.SetActive(_isOpen);
		arrow.eulerAngles = new Vector3(0f, 0f, (!_isOpen) ? 180 : 0);
	}
}
