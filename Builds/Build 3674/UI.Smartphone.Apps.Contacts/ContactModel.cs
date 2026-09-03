using Entities;
using UnityEngine;

namespace UI.Smartphone.Apps.Contacts;

public class ContactModel
{
	public Contact contact;

	public string nameLocalizationKey;

	public string contactName;

	public string descriptionLocalizationKey;

	public Sprite icon;

	public Color iconColor = Color.white;

	public bool isLetterIcon;

	public int numberOfUnreadMessages;

	public bool isPermanent;
}
