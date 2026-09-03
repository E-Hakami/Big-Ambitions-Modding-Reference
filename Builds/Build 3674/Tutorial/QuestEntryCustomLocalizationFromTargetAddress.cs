using Localizor.LanguageChangeEvent;
using Streets;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/CustomLocalization/FromTargetAddress")]
public class QuestEntryCustomLocalizationFromTargetAddress : QuestEntryCustomLocalization
{
	[SerializeField]
	private AddressTarget target;

	public override LanguageChangeEventDataHolder GetLocalization(string localizeKey)
	{
		return new LanguageChangeEventDataHolder
		{
			Key = localizeKey,
			Arguments = new
			{
				targetAddress = target.GetAddress()?.ToFormattedString()
			}
		};
	}

	public override void Init()
	{
	}

	public override void Dispose()
	{
	}
}
