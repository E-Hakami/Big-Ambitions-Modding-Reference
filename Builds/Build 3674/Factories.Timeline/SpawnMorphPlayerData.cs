using System;
using NaughtyAttributes;
using UnityEngine;

namespace Factories.Timeline;

public class SpawnMorphPlayerData : MonoBehaviour
{
	public Action onItemsChanged;

	[ReadOnly]
	public string startItem;

	[ReadOnly]
	public string secondaryStartItem;

	[ReadOnly]
	public string endItem;
}
