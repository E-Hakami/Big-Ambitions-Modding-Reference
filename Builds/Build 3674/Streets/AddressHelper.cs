using System.Collections.Generic;
using BigAmbitions.Items;
using HGAttributes;

namespace Streets;

public static class AddressHelper
{
	public const string AddressableLabel = "StreetData";

	public const string StreetNamesKey = "StreetNames";

	private static readonly Dictionary<string, StreetData> StreetDataDictionary = new Dictionary<string, StreetData>();

	[AutocompleteProvider("StreetNames")]
	public static IEnumerable<string> StreetDataNames => StreetDataDictionary.Keys;

	public static void OnStreetDataLoaded(IList<StreetData> streetData)
	{
		StreetDataDictionary.Clear();
		foreach (StreetData streetDatum in streetData)
		{
			StreetDataDictionary.Add(streetDatum.streetName, streetDatum);
		}
	}

	public static bool IsUndefined(this Address address)
	{
		if (!(address == null))
		{
			return string.IsNullOrEmpty(address.streetName);
		}
		return true;
	}

	public static void UpdateCurrentAddress(Address newAddress)
	{
		if (newAddress == null)
		{
			newAddress = new Address(null, 0);
		}
		SaveGameManager.Current.CurrentStreetName = newAddress.streetName;
		SaveGameManager.Current.CurrentStreetNumber = newAddress.streetNumber;
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle != null)
		{
			InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.SetStreetData(newAddress.streetName, newAddress.streetNumber);
		}
	}

	public static string ToFormattedString(this Address address)
	{
		return $"{address.streetNumber} {address.GetStreetNameLocalized()}";
	}

	public static string GetStreetNameLocalized(string streetName)
	{
		if (string.IsNullOrEmpty(streetName))
		{
			return string.Empty;
		}
		if (!StreetDataDictionary.TryGetValue(streetName, out var value))
		{
			return streetName;
		}
		if (!string.IsNullOrEmpty(value.streetLocalization))
		{
			return value.streetLocalization;
		}
		return streetName;
	}

	public static string GetStreetNameLocalized(this Address address)
	{
		return GetStreetNameLocalized(address.streetName);
	}

	public static string ToAnalyticsString(this Address address)
	{
		return $"{address.streetNumber} {address.streetName}";
	}

	public static StreetData GetStreetData(string streetName)
	{
		StreetDataDictionary.TryGetValue(streetName, out var value);
		return value;
	}

	public static string GetStreetNameByAbbreviation(string abbreviation)
	{
		foreach (StreetData value in StreetDataDictionary.Values)
		{
			if (value.helpSystemAbbreviation == abbreviation)
			{
				return value.streetName;
			}
		}
		return null;
	}
}
