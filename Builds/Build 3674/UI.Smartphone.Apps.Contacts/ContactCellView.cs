using BaTable;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Contacts;

public class ContactCellView : BaTableCellView<ContactModel>
{
	public ContactIconView iconView;

	public TextLocalizationComponent nameLocalizationComponent;

	public TMP_Text nameText;

	public TextLocalizationComponent descriptionLocalizationComponent;

	public TMP_Text descriptionText;

	public Badge badge;

	public Button deleteButton;

	public Sprite selectedBackground;

	public Sprite unselectedBackground;

	public override void SetData(ContactModel data)
	{
		if (data.nameLocalizationKey != null)
		{
			nameLocalizationComponent.Key = data.nameLocalizationKey;
		}
		else
		{
			nameText.text = data.contactName;
		}
		descriptionLocalizationComponent.Key = data.descriptionLocalizationKey;
		if (data.isLetterIcon)
		{
			iconView.SetLetterIcon(data.icon, data.iconColor);
		}
		else
		{
			iconView.SetSquareIcon(data.icon);
		}
		badge.UpdateBadge(data.numberOfUnreadMessages);
		data.contact.contactCellView = this;
		deleteButton.onClick.RemoveAllListeners();
		if (data.isPermanent)
		{
			deleteButton.gameObject.SetActive(value: false);
			return;
		}
		deleteButton.gameObject.SetActive(value: true);
		deleteButton.onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.RemoveContact(data.contact);
		});
	}

	public override void VisualizeSelected(bool selected)
	{
		if (selected)
		{
			SelectContactButton();
		}
		else
		{
			UnselectCurrentContactButton();
		}
	}

	public void ClearUnreadMessages()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.contactScrollerController.data[dataIndex].numberOfUnreadMessages = 0;
		badge.UpdateBadge(0);
	}

	private void UnselectCurrentContactButton()
	{
		backgroundImage.sprite = unselectedBackground;
		nameText.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
		descriptionText.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
	}

	private void SelectContactButton()
	{
		backgroundImage.sprite = selectedBackground;
		nameText.color = InstanceBehavior<GlobalReferences>.Instance.colors.black;
		descriptionText.color = InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey;
	}
}
