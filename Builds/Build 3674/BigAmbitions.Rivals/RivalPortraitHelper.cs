using System;
using System.Collections.Generic;
using Character.Customization;
using UnityEngine;

namespace BigAmbitions.Rivals;

public static class RivalPortraitHelper
{
	private static readonly Dictionary<string, Sprite> PortraitsDict = new Dictionary<string, Sprite>();

	public static Sprite GetPortrait(string rivalId)
	{
		return PortraitsDict.GetValueOrDefault(rivalId);
	}

	public static void CreatePortrait(RivalData rivalData, int backgroundIndex, Action<Sprite> callback = null)
	{
		PortraitGenerator.Create(rivalData.gender, rivalData.GetRivalAgeInYears(), rivalData.id, backgroundIndex, null, null, delegate(Sprite sprite)
		{
			PortraitsDict.Add(rivalData.id, sprite);
			callback?.Invoke(sprite);
		});
	}
}
