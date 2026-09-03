// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.ModEnumHash
using System;
using System.Security.Cryptography;
using System.Text;

public static class ModEnumHash
{
	private const int ProhibitedRangeEnd = 2000;

	public static int GetSafeHash(string source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		using SHA256 sHA = SHA256.Create();
		byte[] bytes = Encoding.UTF8.GetBytes(source);
		byte[] array = sHA.ComputeHash(bytes);
		int num = array[0] | (array[1] << 8) | (array[2] << 16) | (array[3] << 24);
		if (num >= 0 && num <= 2000)
		{
			num += 2001;
		}
		return num;
	}
}
