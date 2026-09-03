using System.Collections.Generic;

namespace Services;

public static class ContractItemsForSaleService
{
	private static readonly Dictionary<string, List<string>> ItemNamesByContactId = new Dictionary<string, List<string>>();

	private static readonly Dictionary<Address, List<string>> ItemNamesByAddress = new Dictionary<Address, List<string>>();

	private static readonly Dictionary<string, List<string>> VehicleNamesByContactId = new Dictionary<string, List<string>>();

	private static readonly Dictionary<Address, List<string>> VehicleNamesByAddress = new Dictionary<Address, List<string>>();

	private static readonly Dictionary<Address, string> ContactIdByAddress = new Dictionary<Address, string>();

	public static void SetItemsForContact(string contactId, IEnumerable<string> itemNames)
	{
		SetNamesForContact(ItemNamesByContactId, contactId, itemNames);
	}

	public static void SetItemsForAddress(Address address, IEnumerable<string> itemNames)
	{
		SetNamesForAddress(ItemNamesByAddress, address, itemNames);
	}

	public static bool TryGetItemsForContact(string contactId, out List<string> itemNames)
	{
		return TryGetNamesForContact(ItemNamesByContactId, contactId, out itemNames);
	}

	public static bool TryGetItemsForAddress(Address address, out List<string> itemNames)
	{
		return TryGetNamesForAddress(ItemNamesByAddress, address, out itemNames);
	}

	public static void SetVehiclesForContact(string contactId, IEnumerable<string> vehicleNames)
	{
		SetNamesForContact(VehicleNamesByContactId, contactId, vehicleNames);
	}

	public static void SetVehiclesForAddress(Address address, IEnumerable<string> vehicleNames)
	{
		SetNamesForAddress(VehicleNamesByAddress, address, vehicleNames);
	}

	public static bool TryGetVehiclesForContact(string contactId, out List<string> vehicleNames)
	{
		return TryGetNamesForContact(VehicleNamesByContactId, contactId, out vehicleNames);
	}

	public static bool TryGetVehiclesForAddress(Address address, out List<string> vehicleNames)
	{
		return TryGetNamesForAddress(VehicleNamesByAddress, address, out vehicleNames);
	}

	public static void RemoveContact(string contactId)
	{
		if (!string.IsNullOrEmpty(contactId))
		{
			ItemNamesByContactId.Remove(contactId);
			VehicleNamesByContactId.Remove(contactId);
		}
	}

	public static void RemoveAddress(Address address)
	{
		if (!(address == null))
		{
			ItemNamesByAddress.Remove(address);
			VehicleNamesByAddress.Remove(address);
			ContactIdByAddress.Remove(address);
		}
	}

	public static void SetContactForAddress(Address address, string contactId)
	{
		if (!(address == null) && !string.IsNullOrEmpty(contactId))
		{
			ContactIdByAddress[address] = contactId;
		}
	}

	public static void RemoveContactForAddress(Address address)
	{
		if (!(address == null))
		{
			ContactIdByAddress.Remove(address);
		}
	}

	public static bool TryGetContactIdForAddress(Address address, out string contactId)
	{
		if (address != null)
		{
			return ContactIdByAddress.TryGetValue(address, out contactId);
		}
		contactId = null;
		return false;
	}

	private static void SetNamesForContact(Dictionary<string, List<string>> namesByContactId, string contactId, IEnumerable<string> names)
	{
		if (!string.IsNullOrEmpty(contactId))
		{
			if (names == null)
			{
				namesByContactId.Remove(contactId);
				return;
			}
			List<string> value = CreateNameList(names);
			namesByContactId[contactId] = value;
		}
	}

	private static void SetNamesForAddress(Dictionary<Address, List<string>> namesByAddress, Address address, IEnumerable<string> names)
	{
		if (!(address == null))
		{
			if (names == null)
			{
				namesByAddress.Remove(address);
				return;
			}
			List<string> value = CreateNameList(names);
			namesByAddress[address] = value;
		}
	}

	private static bool TryGetNamesForContact(Dictionary<string, List<string>> namesByContactId, string contactId, out List<string> names)
	{
		if (string.IsNullOrEmpty(contactId) || !namesByContactId.TryGetValue(contactId, out var value))
		{
			names = null;
			return false;
		}
		names = new List<string>(value);
		return true;
	}

	private static bool TryGetNamesForAddress(Dictionary<Address, List<string>> namesByAddress, Address address, out List<string> names)
	{
		if (address == null || !namesByAddress.TryGetValue(address, out var value))
		{
			names = null;
			return false;
		}
		names = new List<string>(value);
		return true;
	}

	private static List<string> CreateNameList(IEnumerable<string> names)
	{
		List<string> list = new List<string>();
		foreach (string name in names)
		{
			if (!string.IsNullOrEmpty(name))
			{
				list.Add(name);
			}
		}
		return list;
	}
}
