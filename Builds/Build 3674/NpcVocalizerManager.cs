using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.SoundSystem;
using Extensions;
using Helpers;
using NaughtyAttributes;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

public class NpcVocalizerManager : InstanceBehavior<NpcVocalizerManager>
{
	[Serializable]
	public class NpcVocalisation
	{
		[Serializable]
		public class NpcVocalisationWeights
		{
			[HideInInspector]
			[AllowNesting]
			public string name;

			[SearchableEnum]
			public SoundType type;

			public float weight;

			[Range(0f, 1f)]
			public float skipPossibility;
		}

		[HideInInspector]
		[AllowNesting]
		public string name;

		public Gender gender;

		public NpcVocalisationWeights[] types;
	}

	public NpcVocalisation[] npcVocalisations;

	private readonly Dictionary<Gender, Tuple<NpcVocalisation.NpcVocalisationWeights[], List<float>>> _npcVocalisations = new Dictionary<Gender, Tuple<NpcVocalisation.NpcVocalisationWeights[], List<float>>>();

	[Tooltip("Time Between NPC Vocalisations in Seconds")]
	[MinMaxSlider(1f, 360f)]
	public Vector2 randomTimeDelay = new Vector2(30f, 60f);

	private Collider[] _npcCollider = new Collider[30];

	public float vocalisationRange = 15f;

	private void OnValidate()
	{
		NpcVocalisation[] array = npcVocalisations;
		foreach (NpcVocalisation obj in array)
		{
			obj.name = obj.gender.ToStringFast();
			NpcVocalisation.NpcVocalisationWeights[] types = obj.types;
			foreach (NpcVocalisation.NpcVocalisationWeights obj2 in types)
			{
				obj2.name = obj2.type.ToStringFast();
			}
		}
	}

	public IEnumerator Start()
	{
		NpcVocalisation[] array = npcVocalisations;
		foreach (NpcVocalisation npcVocalisation in array)
		{
			List<float> item = npcVocalisation.types.Select((NpcVocalisation.NpcVocalisationWeights x) => x.weight).ToList();
			_npcVocalisations.Add(npcVocalisation.gender, new Tuple<NpcVocalisation.NpcVocalisationWeights[], List<float>>(npcVocalisation.types, item));
		}
		yield return null;
		while (true)
		{
			int num = Physics.OverlapSphereNonAlloc(InstanceBehavior<GameManager>.Instance.playerController.transform.position, vocalisationRange, _npcCollider, LayerHelper.humanLayerMask);
			if (num > 1)
			{
				ThirdPersonCharacter thirdPersonCharacter = SelectRandom(num);
				if ((bool)thirdPersonCharacter)
				{
					var (array3, weights) = _npcVocalisations[thirdPersonCharacter.appearanceSetter.data.gender];
					NpcVocalisation.NpcVocalisationWeights npcVocalisationWeights = array3[RngHelper.GetRandomWeightedIndex(weights)];
					if (npcVocalisationWeights.skipPossibility < UnityEngine.Random.Range(0f, 1f))
					{
						InstanceBehavior<SfxManager>.Instance.PlayAudio(npcVocalisationWeights.type, thirdPersonCharacter.transform.position);
					}
				}
			}
			yield return new WaitForSeconds(randomTimeDelay.RandomValue());
		}
	}

	private ThirdPersonCharacter SelectRandom(int count)
	{
		for (int i = 0; i < count; i++)
		{
			ThirdPersonCharacter component = _npcCollider[UnityEngine.Random.Range(0, count)].GetComponent<ThirdPersonCharacter>();
			if (!(component == null) && !component.isPlayer)
			{
				return component;
			}
		}
		return null;
	}
}
