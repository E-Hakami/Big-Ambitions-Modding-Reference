using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace UI.Apps.Contacts;

[CreateAssetMenu(fileName = "ContactPreset", menuName = "BigAmbitions/ContactPreset")]
public class ContactPreset : ScriptableObject
{
	public string id;

	public Sprite icon;

	public ContactCategoryName category;

	public string nameLocalizationKey;

	public string descriptionLocalizationKey;

	public bool isPermanent;

	public bool hasBillboard;
}
