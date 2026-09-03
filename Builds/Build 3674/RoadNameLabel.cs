using Localizor.LanguageChangeEvent;
using Streets;
using UnityEngine;

public class RoadNameLabel : MonoBehaviour
{
	[SerializeField]
	private string streetName;

	[SerializeField]
	private TextLocalizationComponent roadNameLabel;

	private void Start()
	{
		roadNameLabel.enabled = false;
		roadNameLabel.Key = string.Empty;
		roadNameLabel.SetValue(Road.Prefix + AddressHelper.GetStreetNameLocalized(streetName) + Road.Suffix);
	}
}
