using System;
using System.Collections.Generic;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.SmartphoneUI;

public class SmartPhoneFrame : MonoBehaviour
{
	[Serializable]
	public class FrameData
	{
		public string itemName;

		public Sprite frameSprite;

		public Vector2 offsetMin;

		public Vector2 offsetMax;

		public Sprite maskSprite;

		public string titleKey;
	}

	[SerializeField]
	private List<FrameData> frameData;

	[SerializeField]
	private Image frameImage;

	[SerializeField]
	private Image maskImage;

	[SerializeField]
	private RectTransform offsetReverser;

	[SerializeField]
	private TextLocalizationComponent titleText;

	public void UpdateFrame()
	{
		string text = SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance?.itemName;
		foreach (FrameData frameDatum in frameData)
		{
			if (!(frameDatum.itemName != text))
			{
				ApplyFrame(frameDatum);
				return;
			}
		}
		if (frameData.Count > 0)
		{
			ApplyFrame(frameData[0]);
		}
	}

	private void ApplyFrame(FrameData data)
	{
		frameImage.sprite = data.frameSprite;
		frameImage.rectTransform.offsetMin = data.offsetMin;
		frameImage.rectTransform.offsetMax = data.offsetMax;
		maskImage.enabled = data.maskSprite;
		maskImage.sprite = data.maskSprite;
		maskImage.rectTransform.offsetMin = data.offsetMin;
		maskImage.rectTransform.offsetMax = data.offsetMax;
		offsetReverser.offsetMin = -data.offsetMin;
		offsetReverser.offsetMax = -data.offsetMax;
		titleText.Key = data.titleKey;
	}
}
