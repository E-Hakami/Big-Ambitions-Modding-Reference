using System.Collections.Generic;
using System.Linq;
using Entities;
using Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Smartphone.Apps.Contacts;

public class ContactCategorySelection : MonoBehaviour
{
	[SerializeField]
	private Color selectedColor;

	[SerializeField]
	private Color defaultColor;

	[SerializeField]
	private ContactCategoryButton categoryButtonPrefab;

	[SerializeField]
	private List<ContactCategory> categories;

	private ContactsApp _contactsApp;

	private List<ContactCategoryButton> _categoryButtons;

	private static readonly UnityEvent<ContactCategoryName?> OnCategoryChanged = new UnityEvent<ContactCategoryName?>();

	private static ContactCategoryName? _selectedCategory;

	public static ContactCategoryName? SelectedCategory
	{
		get
		{
			return _selectedCategory;
		}
		set
		{
			_selectedCategory = value;
			OnCategoryChanged?.Invoke(value);
		}
	}

	private void Awake()
	{
		_contactsApp = Object.FindObjectOfType<ContactsApp>();
		_categoryButtons = new List<ContactCategoryButton>();
		OnCategoryChanged.AddListener(UpdateButtonSelection);
	}

	private void Start()
	{
		ContactsApp.onContactAdded.AddListener(SetupCategoryButtons);
		ContactsApp.onContactRemoved.AddListener(SetupCategoryButtons);
		SmartphoneUI.OnUpdatedBadgeCount.AddListener(delegate(AppName app)
		{
			if (app == AppName.Contacts)
			{
				UpdateBadges();
			}
		});
	}

	private void OnDestroy()
	{
		OnCategoryChanged.RemoveAllListeners();
		ContactsApp.onContactAdded.RemoveListener(SetupCategoryButtons);
		ContactsApp.onContactRemoved.RemoveListener(SetupCategoryButtons);
	}

	private void OnEnable()
	{
		SetupCategoryButtons();
	}

	public void UpdateButtonSelection(ContactCategoryName? categoryName)
	{
		ContactCategoryButton contactCategoryButton = _categoryButtons.FirstOrDefault((ContactCategoryButton x) => x.categoryName == categoryName);
		foreach (ContactCategoryButton categoryButton in _categoryButtons)
		{
			categoryButton.backgroundImage.color = defaultColor;
		}
		if (!(contactCategoryButton == null))
		{
			contactCategoryButton.backgroundImage.color = selectedColor;
		}
	}

	private void SetupCategoryButtons()
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return;
		}
		categoryButtonPrefab.transform.ResetTemplate();
		_categoryButtons.Clear();
		List<ContactCategoryName> unusedCategories = categories.Select((ContactCategory c) => c.categoryName).Except(SaveGameManager.Current.Contacts.Select((Contact c) => c.category)).ToList();
		foreach (ContactCategory item in categories.Where((ContactCategory x) => !unusedCategories.Contains(x.categoryName)))
		{
			ContactCategoryButton contactCategoryButton = Object.Instantiate(categoryButtonPrefab, categoryButtonPrefab.transform.parent);
			contactCategoryButton.gameObject.SetActive(value: true);
			contactCategoryButton.Init(item);
			_categoryButtons.Add(contactCategoryButton);
		}
		UpdateButtonSelection(_selectedCategory);
	}

	private void UpdateBadges()
	{
		foreach (ContactCategoryButton categoryButton in _categoryButtons)
		{
			categoryButton.UpdateBadge();
		}
	}
}
