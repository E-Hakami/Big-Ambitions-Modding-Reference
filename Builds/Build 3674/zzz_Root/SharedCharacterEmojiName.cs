using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedCharacterEmojiName : SharedVariable<CharacterEmojiName>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedCharacterEmojiName(CharacterEmojiName value)
	{
		return new SharedCharacterEmojiName
		{
			mValue = value
		};
	}
}
