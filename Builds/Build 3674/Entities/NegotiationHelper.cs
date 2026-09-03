using AI.Employees.SalaryNegotiation;
using Characters.EmojiSystem;
using UnityEngine;

namespace Entities;

public static class NegotiationHelper
{
	public static (Sprite, Sprite) GetMoodIcon(Negotiator negotiator)
	{
		float moodPercentageNormalized = negotiator.MoodPercentageNormalized;
		CharacterEmojiName characterEmojiName;
		if (moodPercentageNormalized >= 0.33f)
		{
			if (!(moodPercentageNormalized < 0.75f))
			{
				goto IL_002b;
			}
			characterEmojiName = CharacterEmojiName.CommonYellowNeutral;
		}
		else
		{
			if (!(moodPercentageNormalized < 0.33f))
			{
				goto IL_002b;
			}
			characterEmojiName = CharacterEmojiName.CommonRedAngry;
		}
		goto IL_002e;
		IL_002b:
		characterEmojiName = CharacterEmojiName.CommonGreenHappy;
		goto IL_002e;
		IL_002e:
		CharacterEmojiName emoji = characterEmojiName;
		if (negotiator.GetOptions().Length == 2)
		{
			emoji = CharacterEmojiName.CommonRedAngry;
		}
		CharacterEmoji characterEmojiByName = CharacterEmojiSystem.GetCharacterEmojiByName(emoji);
		return (characterEmojiByName.background, characterEmojiByName.icon);
	}

	public static (Sprite, Sprite) GetMoodIcon(CandidateSalaryNegotiator negotiator)
	{
		float moodPercentageNormalized = negotiator.MoodPercentageNormalized;
		CharacterEmojiName emoji;
		if (moodPercentageNormalized >= 0.33f)
		{
			if (!(moodPercentageNormalized < 0.75f))
			{
				goto IL_002b;
			}
			emoji = CharacterEmojiName.CommonYellowNeutral;
		}
		else
		{
			if (!(moodPercentageNormalized < 0.33f))
			{
				goto IL_002b;
			}
			emoji = CharacterEmojiName.CommonRedAngry;
		}
		goto IL_002e;
		IL_002b:
		emoji = CharacterEmojiName.CommonGreenHappy;
		goto IL_002e;
		IL_002e:
		CharacterEmoji characterEmojiByName = CharacterEmojiSystem.GetCharacterEmojiByName(emoji);
		return (characterEmojiByName.background, characterEmojiByName.icon);
	}
}
