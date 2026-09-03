using UnityEngine;

namespace Character.Customization;

[CreateAssetMenu(fileName = "PlasticSurgeryPricing", menuName = "BigAmbitions/PlasticSurgeryPricing")]
public class PlasticSurgeryPricing : ScriptableObject
{
	public float eyeColorPrice = 1000f;

	public float skinColorPrice = 1000f;

	public float bodyValuesPrice = 1000f;

	public float eyesVariantPrice = 1000f;

	public float mouthVariantPrice = 1000f;

	public float noseVariantPrice = 1000f;

	public float headVariantPrice = 1000f;
}
