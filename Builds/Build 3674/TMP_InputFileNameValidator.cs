using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

[CreateAssetMenu]
public class TMP_InputFileNameValidator : TMP_InputValidator
{
	private static readonly char[] IllegalFileNameChars = Path.GetInvalidFileNameChars();

	private static readonly char[] IllegalFilePathChars = Path.GetInvalidPathChars();

	public override char Validate(ref string text, ref int pos, char ch)
	{
		if (IllegalFileNameChars.Contains(ch) || IllegalFilePathChars.Contains(ch))
		{
			return '\0';
		}
		text = text.Insert(pos, ch.ToString());
		pos++;
		return ch;
	}
}
