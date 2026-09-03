using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Components;
using UI.Elements;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Extensions;

public static class GenericExtensions
{
	private static readonly string[] ShortCurrencySuffixKeys = new string[4] { "currency_suffix_thousand", "currency_suffix_million", "currency_suffix_billion", "currency_suffix_trillion" };

	private static Process[] Processes;

	private static float LastProcessFetchTime;

	private static CultureInfo CultureInfo => CultureHelper.CultureInfo;

	public static bool InRange<T>(this T value, T from, T to) where T : IComparable<T>
	{
		if (value.CompareTo(from) >= 0)
		{
			return value.CompareTo(to) <= 0;
		}
		return false;
	}

	public static string ToCurrencyFormat(this float val)
	{
		return val.ToString("C", CultureInfo);
	}

	public static string AddCurrencySymbol(this string val)
	{
		return CultureInfo.NumberFormat.CurrencySymbol + val;
	}

	public static bool FromShortCurrencyFormat(this string val, out float value)
	{
		bool result = TryParseShortCurrencyFormat(val, CultureInfo, out var value2);
		value = (float)value2;
		return result;
	}

	public static bool FromShortCurrencyFormatInvariant(this string val, out decimal value)
	{
		return TryParseShortCurrencyFormat(val.Replace(CultureInfo.NumberFormat.CurrencySymbol, "").Trim(), CultureInfo.InvariantCulture, out value);
	}

	private static bool TryParseShortCurrencyFormat(string val, CultureInfo cultureInfo, out decimal value)
	{
		val = val.Replace(cultureInfo.NumberFormat.CurrencySymbol, "").Trim();
		decimal num = 1m;
		for (int i = 0; i < ShortCurrencySuffixKeys.Length; i++)
		{
			string localization = ShortCurrencySuffixKeys[i].GetLocalization();
			if (string.IsNullOrEmpty(localization))
			{
				continue;
			}
			int num2 = val.IndexOf(localization, StringComparison.OrdinalIgnoreCase);
			if (num2 >= 0)
			{
				val = val.Remove(num2, localization.Length);
				for (int j = 0; j <= i; j++)
				{
					num *= 1000m;
				}
				break;
			}
		}
		bool result = decimal.TryParse(val.Trim(), NumberStyles.Currency, cultureInfo, out value);
		value *= num;
		return result;
	}

	public static string ToShortCurrencyFormat(this float val, bool abbreviated = false)
	{
		return ((double)val).ToShortCurrencyFormat(abbreviated);
	}

	public static string ToShortCurrencyFormat(this int val, bool abbreviated = false)
	{
		return ((double)val).ToShortCurrencyFormat(abbreviated);
	}

	public static string ToShortCurrencyFormat(this double val, bool abbreviated = false)
	{
		bool flag = val < 0.0;
		if (abbreviated)
		{
			double num = Math.Abs(val);
			int num2 = -1;
			if (num > 9999.0)
			{
				while (num >= 1000.0 && num2 < ShortCurrencySuffixKeys.Length - 1)
				{
					num /= 1000.0;
					num2++;
				}
				num = Math.Floor(num * 10.0) / 10.0;
			}
			string text = ((num2 >= 0) ? ShortCurrencySuffixKeys[num2].GetLocalization() : "");
			return ((num2 >= 0 && !(num >= 100.0)) ? ((double)Math.Sign(val) * num).ToString("C1", CultureInfo) : (flag ? Math.Ceiling((double)Math.Sign(val) * num).ToString("C0", CultureInfo) : Math.Floor((double)Math.Sign(val) * num).ToString("C0", CultureInfo))) + text;
		}
		val = Math.Truncate(val);
		return val.ToString("C0", CultureInfo);
	}

	public static string ToFormattedNumber(this float val)
	{
		return val.ToString("N0", CultureInfo);
	}

	public static string ToFormattedNumber(this int val)
	{
		return val.ToString("N0", CultureInfo);
	}

	public static TextMeshProUGUI GetLabelByName(this Transform t, string name)
	{
		return t.Find(name).GetComponent<TextMeshProUGUI>();
	}

	public static Image GetImageByName(this Transform t, string name)
	{
		return t.Find(name).GetComponent<Image>();
	}

	public static TextLocalizationComponent GetLanguageChangeEventByName(this Transform t, string name)
	{
		return t.Find(name).GetComponent<TextLocalizationComponent>();
	}

	public static TMP_InputField GetTmpInputByName(this Transform t, string name)
	{
		return t.Find(name).GetComponent<TMP_InputField>();
	}

	public static UI.Components.InputField GetInputByName(this Transform t, string name)
	{
		return t.Find(name).GetComponent<UI.Components.InputField>();
	}

	public static UI.Elements.Dropdown GetDropDownByName(this Transform t, string name)
	{
		return t.Find(name).GetComponent<UI.Elements.Dropdown>();
	}

	public static Button GetButtonByName(this Transform t, string name)
	{
		return t.Find(name).GetComponent<Button>();
	}

	public static float Map(this float value, float from0, float from1, float to0, float to1)
	{
		return (value - from0) / (to0 - from0) * (to1 - from1) + from1;
	}

	public static void DrawPlane(Vector3 position, Vector3 normal)
	{
		Vector3 vector = ((!(normal.normalized != Vector3.forward)) ? (Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude) : (Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude));
		Vector3 vector2 = position + vector;
		Vector3 vector3 = position - vector;
		vector = Quaternion.AngleAxis(90f, normal) * vector;
		Vector3 vector4 = position + vector;
		Vector3 vector5 = position - vector;
		UnityEngine.Debug.DrawLine(vector2, vector3, Color.green);
		UnityEngine.Debug.DrawLine(vector4, vector5, Color.green);
		UnityEngine.Debug.DrawLine(vector2, vector4, Color.green);
		UnityEngine.Debug.DrawLine(vector4, vector3, Color.green);
		UnityEngine.Debug.DrawLine(vector3, vector5, Color.green);
		UnityEngine.Debug.DrawLine(vector5, vector2, Color.green);
		UnityEngine.Debug.DrawRay(position, normal, Color.red);
	}

	public static Vector3 WorldToScreenPointProjected(this Camera cam, Vector3 worldPos)
	{
		Vector3 result = cam.WorldToScreenPoint(worldPos);
		if (result.z < 0f)
		{
			result *= -1f;
		}
		return result;
	}

	public static string GenerateSlug(this string phrase)
	{
		string input = phrase.RemoveAccent().ToLower();
		input = Regex.Replace(input, "[^a-z0-9\\s-]", "");
		input = Regex.Replace(input, "\\s+", " ").Trim();
		input = input.Substring(0, (input.Length <= 45) ? input.Length : 45).Trim();
		return Regex.Replace(input, "\\s", "-");
	}

	public static string RemoveAccent(this string txt)
	{
		byte[] bytes = Encoding.GetEncoding("Cyrillic").GetBytes(txt);
		return Encoding.ASCII.GetString(bytes);
	}

	public static T GetMemberValue<T>(this object src, string propName)
	{
		MemberInfo memberInfo = src.GetType().GetMember(propName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();
		if (memberInfo == null)
		{
			return default(T);
		}
		if ((memberInfo.MemberType & MemberTypes.Field) != 0)
		{
			return (T)((FieldInfo)memberInfo).GetValue(src);
		}
		return (T)((PropertyInfo)memberInfo).GetValue(src);
	}

	public static bool IsProgramOpen(string programName)
	{
		try
		{
			if (Time.time - LastProcessFetchTime > 5f)
			{
				Processes = Process.GetProcesses();
				LastProcessFetchTime = Time.time;
			}
			for (int i = 0; i < Processes.Length; i++)
			{
				if (!Processes[i].HasExited && Processes[i].ProcessName.IndexOf(programName, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static bool PathCanBeCompleted(Vector3 sourcePosition, Vector3 targetPosition)
	{
		NavMeshPath navMeshPath = new NavMeshPath();
		if (NavMesh.CalculatePath(sourcePosition, targetPosition, -1, navMeshPath) && navMeshPath.status == NavMeshPathStatus.PathComplete)
		{
			return Vector3.SqrMagnitude(navMeshPath.corners[^1] - targetPosition) <= 0.25f;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool DlcIsOwned(this SteamAPI.DLC dlc)
	{
		return SteamAPI.DlcIsOwned(dlc);
	}

	public static string CapitalizeFirstChar(this string str)
	{
		string text = str[0].ToString().ToUpper();
		string text2 = str;
		str = text + text2.Substring(1, text2.Length - 1);
		return str;
	}

	public static string Stringify<T>(this IEnumerable<T> collection, bool localize = false)
	{
		if (collection == null)
		{
			return "";
		}
		if (!localize)
		{
			return string.Join(", ", collection);
		}
		return string.Join(", ", collection.Select((T x) => x.ToString().GetLocalization()));
	}

	public static string Listify<T>(this IEnumerable<T> collection, bool localize = false)
	{
		if (collection == null)
		{
			return "";
		}
		T[] array = collection.ToArray();
		if (!localize)
		{
			return string.Join("\n", array.Select((T item) => "- " + item.ToString()));
		}
		string[] array2 = new string[array.Length];
		for (int num = 0; num < array.Length; num++)
		{
			array2[num] = "- " + array[num].ToString().GetLocalization();
		}
		return string.Join("\n", array2);
	}

	public static T Next<T>(this T src) where T : struct, IConvertible
	{
		if (!typeof(T).IsEnum)
		{
			throw new ArgumentException("T must be an enumerated type");
		}
		T[] array = (T[])Enum.GetValues(src.GetType());
		int num = Array.IndexOf(array, src) + 1;
		if (array.Length != num)
		{
			return array[num];
		}
		return array[0];
	}

	public static void SortInt<T>(this List<T> list, Func<T, int> selector, bool ascending = true)
	{
		if (ascending)
		{
			list.Sort((T a, T b) => selector(a).CompareTo(selector(b)));
		}
		else
		{
			list.Sort((T a, T b) => selector(b).CompareTo(selector(a)));
		}
	}

	public static List<T> CopyList<T>(this List<T> source)
	{
		if (source == null)
		{
			return null;
		}
		return new List<T>(source);
	}

	public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
	{
		if (oldIndex != newIndex)
		{
			if (oldIndex >= list.Count || oldIndex < 0)
			{
				throw new ArgumentOutOfRangeException("oldIndex");
			}
			T item = list[oldIndex];
			list.RemoveAt(oldIndex);
			newIndex = Mathf.Clamp(newIndex, 0, list.Count);
			list.Insert(newIndex, item);
		}
	}

	public static void ReduceOrRemove<T>(this Dictionary<T, int> dict, T key, int amount)
	{
		if (dict.TryGetValue(key, out var value) && value > amount)
		{
			dict[key] = value - amount;
		}
		else
		{
			dict.Remove(key);
		}
	}

	public static void SumOrAdd<T>(this Dictionary<T, int> dict, T key, int amount)
	{
		if (dict.TryGetValue(key, out var value))
		{
			dict[key] = value + amount;
		}
		else
		{
			dict.Add(key, amount);
		}
	}

	public static void RemoveFirst<T>(this List<T> list, Predicate<T> match)
	{
		if (list.Count != 0)
		{
			int num = list.FindIndex(match);
			if (num >= 0)
			{
				list.RemoveAt(num);
			}
		}
	}

	public static T GetMax<T>(this IList<T> list, Comparison<T> comparison)
	{
		if (list == null || list.Count == 0)
		{
			return default(T);
		}
		T val = list[0];
		for (int i = 1; i < list.Count; i++)
		{
			if (comparison(list[i], val) > 0)
			{
				val = list[i];
			}
		}
		return val;
	}

	public static List<TResult> MapToList<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (selector == null)
		{
			throw new ArgumentNullException("selector");
		}
		List<TResult> list = ((!(source is ICollection<TSource> collection)) ? ((!(source is IReadOnlyCollection<TSource> readOnlyCollection)) ? new List<TResult>() : new List<TResult>(readOnlyCollection.Count)) : new List<TResult>(collection.Count));
		foreach (TSource item in source)
		{
			list.Add(selector(item));
		}
		return list;
	}

	public static void MapToListAndSum<TSource>(this IList<TSource> source, List<float> result, Func<TSource, float> selector, out float sum)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (result == null)
		{
			throw new ArgumentNullException("result");
		}
		if (selector == null)
		{
			throw new ArgumentNullException("selector");
		}
		if (source == result)
		{
			throw new ArgumentException("Source and result cannot be the same list.", "result");
		}
		result.Clear();
		if (result.Capacity < source.Count)
		{
			result.Capacity = source.Count;
		}
		sum = 0f;
		for (int i = 0; i < source.Count; i++)
		{
			float num = selector(source[i]);
			result.Add(num);
			sum += num;
		}
	}

	public static string FormatBytes(this long byteCount)
	{
		string[] array = new string[7] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
		double num = byteCount;
		int num2 = 0;
		while (num >= 1024.0 && num2 < array.Length - 1)
		{
			num /= 1024.0;
			num2++;
		}
		if (num2 != 0)
		{
			return $"{num:0.##} {array[num2]}";
		}
		return $"{byteCount} {array[num2]}";
	}

	public static void InvokeSafely(this Action callbacks)
	{
		Delegate[] invocationList = callbacks.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			try
			{
				((Action)invocationList[i])();
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogException(exception);
			}
		}
	}

	public static void InvokeSafely<T>(this Action<T> callbacks, T value)
	{
		try
		{
			Delegate[] invocationList = callbacks.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				Action<T> action = (Action<T>)invocationList[i];
				if (action == null)
				{
					UnityEngine.Debug.LogError("Callback is null!");
					continue;
				}
				try
				{
					action(value);
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogError("Failed callback: " + GetCallbackName(action));
					UnityEngine.Debug.LogException(exception);
				}
			}
		}
		catch (Exception exception2)
		{
			UnityEngine.Debug.LogError("InvokeSafely failed before dispatching callbacks.");
			UnityEngine.Debug.LogException(exception2);
		}
	}

	private static string GetCallbackName(Delegate callback)
	{
		try
		{
			MethodInfo method = callback.Method;
			string text = method.DeclaringType?.FullName ?? "<unknown>";
			string text2 = ((!(callback.Target is UnityEngine.Object obj)) ? ((callback.Target != null) ? callback.Target.GetType().FullName : "<static>") : (obj ? obj.name : "<destroyed>"));
			return text + "." + method.Name + " (target: " + text2 + ")";
		}
		catch
		{
			return "<unknown>";
		}
	}

	public static int SumValues<T>(this IEnumerable<T> values, Func<T, int> selector)
	{
		int num = 0;
		foreach (T value in values)
		{
			num += selector(value);
		}
		return num;
	}

	public static float SumValues<T>(this IEnumerable<T> values, Func<T, float> selector)
	{
		float num = 0f;
		foreach (T value in values)
		{
			num += selector(value);
		}
		return num;
	}

	public static double SumValues<T>(this IEnumerable<T> values, Func<T, double> selector)
	{
		double num = 0.0;
		foreach (T value in values)
		{
			num += selector(value);
		}
		return num;
	}

	public static int CountWhere<T>(this IEnumerable<T> values, Func<T, bool> predicate)
	{
		int num = 0;
		foreach (T value in values)
		{
			if (predicate(value))
			{
				num++;
			}
		}
		return num;
	}

	public static void ColorValueLabel(this TMP_Text label, float value, bool invert = false, Color neutralColor = default(Color))
	{
		if (Mathf.Approximately(value, 0f))
		{
			if (neutralColor == default(Color))
			{
				neutralColor = InstanceBehavior<GlobalReferences>.Instance.colors.black;
			}
			label.color = neutralColor;
			return;
		}
		bool flag = value > 0f;
		if (invert)
		{
			flag = !flag;
		}
		label.color = (flag ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.red);
	}

	public static bool InCollection<T>(this T[] collection, T item)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		for (int i = 0; i < collection.Length; i++)
		{
			if (equalityComparer.Equals(collection[i], item))
			{
				return true;
			}
		}
		return false;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Processes = null;
		LastProcessFetchTime = 0f;
	}
}
