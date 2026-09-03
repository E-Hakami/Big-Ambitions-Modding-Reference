using System;
using System.Collections.Generic;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Neighborhoods;
using Entities;

namespace BigAmbitions.SaveSystem.Legacy;

public static class LegacyHelper
{
	private static class MapperCache<TMapper> where TMapper : ILegacyMapper
	{
		public static readonly ILegacyMapper Mapper = ResolveMapper(typeof(TMapper));
	}

	private const string DuplicatedItemInstanceTypeName = "BigAmbitions.Items.BigAmbitions.Items.ItemInstance, BigAmbitions.Items, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null.Items, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

	private static readonly object InitLock = new object();

	private static readonly IReadOnlyDictionary<string, Type> LegacyTypeAliases = new Dictionary<string, Type>(StringComparer.Ordinal)
	{
		{
			"BigAmbitions.Characters.CharacterData, BigAmbitions.Characters",
			typeof(CharacterData)
		},
		{
			"Character.Appearance.AppearanceElementData, Assembly-CSharp",
			typeof(AppearanceElementData)
		},
		{
			"Character.Customization.Data, Assembly-CSharp",
			typeof(AppearanceElementData)
		},
		{
			"Character.CharacterData, Assembly-CSharp",
			typeof(CharacterData)
		},
		{
			"Entities.CellPosition, Assembly-CSharp",
			typeof(CellPosition)
		},
		{
			"Entities.DeliveryPlan, Assembly-CSharp",
			typeof(LogisticsManagerPlanDestination)
		},
		{
			"Entities.DeliveryPlan, BigAmbitions",
			typeof(LogisticsManagerPlanDestination)
		},
		{
			"Entities.NeighbourhoodStats, Assembly-CSharp",
			typeof(NeighbourhoodStats)
		},
		{
			"Entities.Skill, Assembly-CSharp",
			typeof(Skill)
		},
		{
			"Entities.StockTarget, Assembly-CSharp",
			typeof(ItemAmountTarget)
		},
		{
			"Entities.StockTarget, BigAmbitions",
			typeof(ItemAmountTarget)
		},
		{
			"Entities.Timestamp, Assembly-CSharp",
			typeof(Timestamp)
		},
		{
			"Entities.Timestamp, BigAmbitions",
			typeof(Timestamp)
		},
		{
			"BigAmbitions.Items.BigAmbitions.Items.ItemInstance, BigAmbitions.Items, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null.Items, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
			typeof(ItemInstance)
		},
		{
			"ItemInstance, Assembly-CSharp",
			typeof(ItemInstance)
		},
		{
			"ItemInstance, BigAmbitions",
			typeof(ItemInstance)
		},
		{
			"ItemInstance, BigAmbitions.Items",
			typeof(ItemInstance)
		},
		{
			"ItemInstance+AttachableChild, Assembly-CSharp",
			typeof(AttachableChild)
		},
		{
			"ItemInstance+AttachableChild, BigAmbitions",
			typeof(AttachableChild)
		},
		{
			"ItemInstance+CustomColor, Assembly-CSharp",
			typeof(CustomColor)
		},
		{
			"ItemInstance+CustomColor, BigAmbitions",
			typeof(CustomColor)
		},
		{
			"ItemInstance+ShelfItem, Assembly-CSharp",
			typeof(ShelfItem)
		},
		{
			"ItemInstance+ShelfItem, BigAmbitions",
			typeof(ShelfItem)
		}
	};

	private static readonly ILegacyMapper[] KnownMappers = new ILegacyMapper[20]
	{
		new BuildingSizeLegacyMap(),
		new BuildingTypeLegacyMap(),
		new BusinessProductSourceLegacyMap(),
		new BusinessRequirementLegacyMap(),
		new BusinessTypeLegacyMap(),
		new CustomerDemandLegacyMap(),
		new FactoryWorkstationTypeLegacyMap(),
		new GameEventLegacyMap(),
		new HappinessModifierLegacyMap(),
		new HeadhunterDealBreakerLegacyMap(),
		new InvestmentFundLegacyMap(),
		new ItemNameLegacyMap(),
		new JobDemandLegacyMap(),
		new MessageTypeLegacyMap(),
		new NeighborhoodLegacyMap(),
		new SkillLegacyMap(),
		new StreetLegacyMap(),
		new TowDestinationLegacyMap(),
		new TransactionLegacyMap(),
		new VehicleTypeNameLegacyMap()
	};

	private static Dictionary<Type, ILegacyMapper> MappersByConcreteType;

	private static IReadOnlyDictionary<string, ILegacyMapper> MigrationsByKey;

	private static IReadOnlyCollection<string> LegacyEnumTypeNames;

	private static void Init()
	{
		if (MappersByConcreteType != null)
		{
			return;
		}
		lock (InitLock)
		{
			if (MappersByConcreteType == null)
			{
				MappersByConcreteType = BuildMappersByConcreteType();
				MigrationsByKey = BuildMigrationsByKey(KnownMappers);
				LegacyEnumTypeNames = BuildLegacyEnumTypeNames(KnownMappers);
			}
		}
	}

	public static string Map<TMapper>(int legacy, bool logErrors = true) where TMapper : ILegacyMapper
	{
		Init();
		ILegacyMapper mapper = MapperCache<TMapper>.Mapper;
		if (mapper == null)
		{
			throw new InvalidOperationException("No mapper found for " + typeof(TMapper).FullName);
		}
		return LegacyMapperRegistry.Map(mapper, legacy, logErrors);
	}

	public static IReadOnlyDictionary<string, ILegacyMapper> GetMigrations()
	{
		Init();
		return MigrationsByKey;
	}

	public static IReadOnlyCollection<string> GetLegacyEnumTypeNames()
	{
		Init();
		return LegacyEnumTypeNames;
	}

	public static IReadOnlyDictionary<string, Type> GetLegacyTypeAliases()
	{
		return LegacyTypeAliases;
	}

	private static ILegacyMapper ResolveMapper(Type requestedType)
	{
		if (MappersByConcreteType.TryGetValue(requestedType, out var value))
		{
			return value;
		}
		foreach (ILegacyMapper value2 in MappersByConcreteType.Values)
		{
			if (requestedType.IsAssignableFrom(value2.GetType()))
			{
				return value2;
			}
		}
		return null;
	}

	private static Dictionary<Type, ILegacyMapper> BuildMappersByConcreteType()
	{
		Dictionary<Type, ILegacyMapper> dictionary = new Dictionary<Type, ILegacyMapper>(KnownMappers.Length);
		for (int i = 0; i < KnownMappers.Length; i++)
		{
			ILegacyMapper legacyMapper = KnownMappers[i];
			dictionary[legacyMapper.GetType()] = legacyMapper;
		}
		return dictionary;
	}

	private static IReadOnlyDictionary<string, ILegacyMapper> BuildMigrationsByKey(IReadOnlyList<ILegacyMapper> mappers)
	{
		Dictionary<string, ILegacyMapper> dictionary = new Dictionary<string, ILegacyMapper>(256);
		for (int i = 0; i < mappers.Count; i++)
		{
			ILegacyMapper legacyMapper = mappers[i];
			foreach (string key in legacyMapper.Keys)
			{
				dictionary[key] = legacyMapper;
			}
		}
		return dictionary;
	}

	private static IReadOnlyCollection<string> BuildLegacyEnumTypeNames(IReadOnlyList<ILegacyMapper> mappers)
	{
		HashSet<string> hashSet = new HashSet<string>(16);
		for (int i = 0; i < mappers.Count; i++)
		{
			foreach (string legacyEnumTypeName in mappers[i].LegacyEnumTypeNames)
			{
				hashSet.Add(legacyEnumTypeName);
			}
		}
		return hashSet;
	}
}
