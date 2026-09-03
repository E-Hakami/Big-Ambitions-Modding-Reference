using System;
using System.Reflection;
using System.Text;

namespace UI.MainMenu;

public static class CustomGamePresetsParser
{
	public static string GetDataAsString<T>(T data)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 1; i < typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public).Length; i++)
		{
			FieldInfo obj = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[i];
			string name = obj.Name;
			string text = obj.GetValue(data)?.ToString() ?? "null";
			stringBuilder.AppendLine(name + ": " + text);
		}
		return stringBuilder.ToString();
	}

	public static T GetStringAsData<T>(string data) where T : class, new()
	{
		T val = new T();
		string[] array = data.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		int num = 0;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string[] array3 = array2[i].Split(new string[1] { ": " }, StringSplitOptions.RemoveEmptyEntries);
			if (array3.Length != 2)
			{
				continue;
			}
			string name = array3[0].Trim();
			string value = array3[1].Trim();
			FieldInfo field = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.Public);
			if (!(field == null))
			{
				try
				{
					object value2 = (field.FieldType.IsEnum ? Enum.Parse(field.FieldType, value) : Convert.ChangeType(value, field.FieldType));
					field.SetValue(val, value2);
				}
				catch (Exception)
				{
					continue;
				}
				num++;
			}
		}
		if (num != 0)
		{
			return val;
		}
		return null;
	}
}
