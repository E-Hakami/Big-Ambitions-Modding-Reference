using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.Contacts;

public class TaxesMessageLine : MonoBehaviour
{
	private const string BoldPrefix = "<b>";

	private const string BoldSuffix = "</b>";

	[SerializeField]
	private TextLocalizationComponent leftLabel;

	[SerializeField]
	private TextMeshProUGUI rightLabel;

	public void SetPlain(string label, string value)
	{
		Clear();
		if (LocalizorManager.IsLocalizedKey(label))
		{
			leftLabel.SetData(label.Localize());
		}
		else
		{
			leftLabel.SetValue(label, clearKey: true);
		}
		rightLabel.text = value;
	}

	public void SetLocalized(string labelKey, string value)
	{
		Clear();
		leftLabel.SetData(labelKey.Localize());
		rightLabel.text = value;
	}

	public void SetBoldLocalized(string labelKey)
	{
		SetLocalized(labelKey, string.Empty);
		leftLabel.Prefix = "<b>";
		leftLabel.Suffix = "</b>";
	}

	public void Clear()
	{
		leftLabel.Prefix = string.Empty;
		leftLabel.Suffix = string.Empty;
		leftLabel.Format = string.Empty;
		leftLabel.Arguments = null;
		leftLabel.SetValue(string.Empty, clearKey: true);
		rightLabel.text = string.Empty;
	}
}
