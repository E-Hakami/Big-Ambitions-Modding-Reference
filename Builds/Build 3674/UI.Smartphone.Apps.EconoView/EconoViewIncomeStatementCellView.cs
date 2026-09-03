using System;
using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewIncomeStatementCellView : EnhancedScrollerCellView
{
	[BoxGroup("Values")]
	[SerializeField]
	private TMP_Text nameLabel;

	[BoxGroup("Values")]
	[SerializeField]
	private TextLocalizationComponent nameLocalizationComponent;

	[BoxGroup("Values")]
	[SerializeField]
	private TMP_Text value1;

	[BoxGroup("Values")]
	[SerializeField]
	private TMP_Text value2;

	[BoxGroup("Values")]
	[SerializeField]
	private TMP_Text value3;

	[BoxGroup("Values")]
	[SerializeField]
	private TMP_Text value4;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Image backgroundImage;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite defaultSprite;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite successSprite;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite warningSprite;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite dangerSprite;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite totalPositiveSprite;

	[BoxGroup("Sprites")]
	[SerializeField]
	private Sprite totalNegativeSprite;

	[BoxGroup("Sprites")]
	[SerializeField]
	private GameObject totalsIconObj;

	[BoxGroup("Indent")]
	[SerializeField]
	private RectTransform firstColumnRect;

	[BoxGroup("Indent")]
	[SerializeField]
	private float indentValue;

	private readonly List<EconoViewIncomeStatementRowRelationship> _rowRelationships = new List<EconoViewIncomeStatementRowRelationship>();

	private Action _clickAction;

	private float _originalWidth;

	private bool _hasOriginalWidth;

	public void SetData(EconoViewIncomeStatementModel data, Action clickAction)
	{
		base.gameObject.name = data.name;
		SetupRow(data.rowName, data.rowType, data.values, data.autoSetValue1Color, clickAction);
	}

	private void SetupRow(string rowName, EconoViewRowType type, List<float> values, bool autoSetValue1Color = true, Action clickAction = null)
	{
		_clickAction = clickAction;
		CacheOriginalWidth();
		if (values == null)
		{
			values = new List<float> { 0f, 0f, 0f, 0f };
		}
		if (LocalizorManager.IsLocalizedKey(rowName))
		{
			nameLocalizationComponent.Key = rowName;
		}
		else
		{
			nameLocalizationComponent.Key = null;
			nameLabel.SetText(rowName);
		}
		value1.SetText(values[0].ToCurrencyFormat());
		value2.SetText(values[1].ToCurrencyFormat());
		value3.SetText(values[2].ToCurrencyFormat());
		value4.SetText(values[3].ToCurrencyFormat());
		if (type == EconoViewRowType.Total)
		{
			value1.color = Colors.White;
		}
		else if (autoSetValue1Color)
		{
			value1.color = ((values[0] >= 0f) ? Colors.Green : Colors.Red);
		}
		backgroundImage.sprite = GetRowTypeBackground(type, values[0]);
		firstColumnRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _originalWidth);
		if (ShouldIndent(type))
		{
			firstColumnRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _originalWidth - indentValue);
		}
		totalsIconObj.SetActive(type == EconoViewRowType.Total);
	}

	public void ToggleChildren()
	{
		for (int i = 0; i < _rowRelationships.Count; i++)
		{
			EconoViewIncomeStatementRowRelationship econoViewIncomeStatementRowRelationship = _rowRelationships[i];
			if (!(econoViewIncomeStatementRowRelationship.parent != this) && !(econoViewIncomeStatementRowRelationship.child == null))
			{
				econoViewIncomeStatementRowRelationship.child.gameObject.SetActive(!econoViewIncomeStatementRowRelationship.child.gameObject.activeSelf);
			}
		}
		_clickAction?.Invoke();
	}

	public void SetParent(EconoViewIncomeStatementCellView parent)
	{
		if (!(parent == null))
		{
			parent.AddRowRelationship(parent, this);
		}
	}

	public void ClearRowRelationships()
	{
		_rowRelationships.Clear();
	}

	private void AddRowRelationship(EconoViewIncomeStatementCellView parent, EconoViewIncomeStatementCellView child)
	{
		for (int i = 0; i < _rowRelationships.Count; i++)
		{
			EconoViewIncomeStatementRowRelationship econoViewIncomeStatementRowRelationship = _rowRelationships[i];
			if (econoViewIncomeStatementRowRelationship.parent == parent && econoViewIncomeStatementRowRelationship.child == child)
			{
				return;
			}
		}
		_rowRelationships.Add(new EconoViewIncomeStatementRowRelationship
		{
			parent = parent,
			child = child
		});
	}

	private void CacheOriginalWidth()
	{
		if (!_hasOriginalWidth)
		{
			_originalWidth = firstColumnRect.rect.width;
			_hasOriginalWidth = true;
		}
	}

	private Sprite GetRowTypeBackground(EconoViewRowType type, float value)
	{
		return type switch
		{
			EconoViewRowType.Danger => dangerSprite, 
			EconoViewRowType.Warning => warningSprite, 
			EconoViewRowType.Success => successSprite, 
			EconoViewRowType.Default => defaultSprite, 
			EconoViewRowType.Total => (value >= 0f) ? totalPositiveSprite : totalNegativeSprite, 
			_ => throw new ArgumentOutOfRangeException("type", type, null), 
		};
	}

	private static bool ShouldIndent(EconoViewRowType type)
	{
		if (type != EconoViewRowType.Danger && type != EconoViewRowType.Success)
		{
			return type == EconoViewRowType.Warning;
		}
		return true;
	}
}
