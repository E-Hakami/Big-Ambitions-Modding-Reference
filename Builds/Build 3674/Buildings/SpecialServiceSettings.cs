using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Buildings;

[CreateAssetMenu(menuName = "BigAmbitions/SpecialService/SpecialServiceSettings")]
public class SpecialServiceSettings : ScriptableObject
{
	public NpcSpawnSettings[] npcSpawners;

	private Dictionary<string, NpcSpawnSettings> _npcSpawnersDictionary;

	public Dictionary<string, NpcSpawnSettings> NpcSpawnersDictionary
	{
		get
		{
			if (npcSpawners == null)
			{
				return null;
			}
			return _npcSpawnersDictionary ?? (_npcSpawnersDictionary = npcSpawners.ToDictionary((NpcSpawnSettings x) => x.spawnItem));
		}
	}
}
