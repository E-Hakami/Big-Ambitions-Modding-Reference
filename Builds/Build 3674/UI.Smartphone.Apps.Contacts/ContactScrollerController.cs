using System;
using System.Collections.Generic;
using System.Linq;
using BaTable;
using BigAmbitions.Characters;
using EnhancedUI.EnhancedScroller;
using Entities;
using Helpers;
using Localizor;
using UI.Apps.Contacts;
using UnityEngine;

namespace UI.Smartphone.Apps.Contacts;

public class ContactScrollerController : BaTable<ContactCellView, ContactModel>
{
	public void LoadList(Contact[] contacts, bool scrollToSelected, EmployeeContactIconSettings settings)
	{
		data.Clear();
		Dictionary<string, EmployeeInstance> employeeByName = BuildEmployeeByNameLookup();
		foreach (Contact contact in contacts)
		{
			try
			{
				LoadContact(contact, settings, employeeByName);
			}
			catch (Exception exception)
			{
				Debug.LogWarning("Failed to load contact '" + (contact?.id ?? "Unknown") + "'.");
				Debug.LogException(exception);
			}
		}
		scroller.ReloadData();
		UpdateSelectedCurrentContact(scrollToSelected);
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 160f;
	}

	public void UpdateSelectedCurrentContact(bool scrollToSelected = false)
	{
		Contact selectedContact = InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.selectedContact;
		if (selectedContact == null)
		{
			return;
		}
		int num = data.IndexOf(data.FirstOrDefault((ContactModel x) => x.contact == selectedContact));
		if (num != -1)
		{
			ContactCellView row = scroller.GetCellViewAtDataIndex(num) as ContactCellView;
			SelectRow(row, GetDataId(data[num]));
			if (scrollToSelected)
			{
				scroller.JumpToDataIndex(num);
			}
		}
	}

	protected override string GetDataId(ContactModel contactModel)
	{
		return contactModel.contact.id;
	}

	private static Dictionary<string, EmployeeInstance> BuildEmployeeByNameLookup()
	{
		Dictionary<string, EmployeeInstance> dictionary = new Dictionary<string, EmployeeInstance>(EmployeeHelper.EmployeeInstancesDictionary.Count);
		foreach (EmployeeInstance value in EmployeeHelper.EmployeeInstancesDictionary.Values)
		{
			if (value != null && !dictionary.ContainsKey(value.characterData.name))
			{
				dictionary[value.characterData.name] = value;
			}
		}
		return dictionary;
	}

	private void LoadContact(Contact contact, EmployeeContactIconSettings settings, Dictionary<string, EmployeeInstance> employeeByName)
	{
		ContactModel contactModel = new ContactModel
		{
			contact = contact
		};
		ContactPreset preset = ContactsHelper.GetPreset(contact.id);
		bool flag = preset != null;
		if (contact.IsEmployeeContact && settings != null)
		{
			SetEmployeeIcon(contactModel, contact, settings, employeeByName);
		}
		else
		{
			contactModel.icon = (flag ? preset.icon : ContactsApp.GetContactIcon(contact));
		}
		string text = (flag ? preset.nameLocalizationKey : contact.id);
		if (LocalizorManager.IsLocalizedKey(text))
		{
			contactModel.nameLocalizationKey = text;
		}
		else
		{
			contactModel.contactName = text;
		}
		contactModel.descriptionLocalizationKey = (flag ? preset.descriptionLocalizationKey : contact.description);
		contactModel.numberOfUnreadMessages = contact.NumberOfUnreadMessages;
		contactModel.isPermanent = flag && preset.isPermanent;
		data.Add(contactModel);
	}

	private static void SetEmployeeIcon(ContactModel model, Contact contact, EmployeeContactIconSettings settings, Dictionary<string, EmployeeInstance> employeeByName)
	{
		if (!ContactsHelper.TryGetFirstNameInitial(contact.id, out var initial))
		{
			model.icon = ContactsApp.GetContactIcon(contact);
			return;
		}
		employeeByName.TryGetValue(contact.id, out var value);
		Gender? gender = value?.characterData.gender;
		ContactIconData contactIconData = settings.Resolve(initial, gender);
		model.icon = contactIconData.Sprite;
		model.iconColor = contactIconData.Tint;
		model.isLetterIcon = true;
	}
}
