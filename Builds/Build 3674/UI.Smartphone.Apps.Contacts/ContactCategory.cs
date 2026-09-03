using UnityEngine;

namespace UI.Smartphone.Apps.Contacts;

[CreateAssetMenu(fileName = "ContactCategory", menuName = "BigAmbitions/Apps/Contacts/Category")]
public class ContactCategory : ScriptableObject
{
	public ContactCategoryName categoryName;

	public Sprite icon;
}
