using System.Linq;
using Entities;
using Helpers;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Contacts;

[RequireComponent(typeof(Button))]
public class ContactCategoryButton : MonoBehaviour
{
	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private Badge badge;

	[HideInInspector]
	public Image backgroundImage;

	[HideInInspector]
	public ContactCategoryName categoryName;

	private Button _button;

	private ContactsApp _contactsApp;

	private BasicTooltip _tooltip;

	private void Awake()
	{
		backgroundImage = GetComponent<Image>();
		_button = GetComponent<Button>();
		_contactsApp = Object.FindObjectOfType<ContactsApp>();
		_tooltip = GetComponent<BasicTooltip>();
	}

	public void Init(ContactCategory category)
	{
		iconImage.sprite = category.icon;
		_button.onClick.AddListener(delegate
		{
			if (ContactCategorySelection.SelectedCategory.HasValue && ContactCategorySelection.SelectedCategory.Value == category.categoryName)
			{
				_contactsApp.LoadContactsList(null);
			}
			else
			{
				_contactsApp.LoadContactsList(category.categoryName);
			}
		});
		_tooltip.titleKey = category.categoryName.GetLocalizeKey();
		categoryName = category.categoryName;
		UpdateBadge();
	}

	private void OnDestroy()
	{
		_button.onClick.RemoveAllListeners();
	}

	public void UpdateBadge()
	{
		int value = SaveGameManager.Current.Contacts.Where((Contact x) => x.category == categoryName).Sum((Contact x) => x.NumberOfUnreadMessages);
		badge.UpdateBadge(value);
	}
}
