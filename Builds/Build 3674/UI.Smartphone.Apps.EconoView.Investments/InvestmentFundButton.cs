using System;
using Entities;
using Localizor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.EconoView.Investments;

public class InvestmentFundButton : MonoBehaviour
{
	[SerializeField]
	private Button button;

	[SerializeField]
	private Image background;

	[SerializeField]
	private Sprite selectedSprite;

	[SerializeField]
	private Sprite unselectedSprite;

	[SerializeField]
	private TMP_Text text;

	private Action<string> _onClick;

	private string _fundName;

	public string FundName => _fundName;

	private void Awake()
	{
		button.onClick.AddListener(OnClick);
	}

	public void Setup(InvestmentFund fund, Action<string> onClick)
	{
		_fundName = fund.name;
		text.SetText(fund.name.GetLocalization());
		_onClick = onClick;
	}

	public void SetSelected(bool isSelected)
	{
		Image image = ((background != null) ? background : button.image);
		if (!(image == null))
		{
			Sprite sprite = (isSelected ? selectedSprite : unselectedSprite);
			if (sprite != null)
			{
				image.sprite = sprite;
			}
		}
	}

	private void OnClick()
	{
		_onClick?.Invoke(_fundName);
	}
}
