using System.Linq;
using BigAmbitions.Characters;
using Helpers;
using UnityEngine;

public class DeliveryTruckDriverController : MonoBehaviour
{
	[SerializeField]
	private CarFeatures carFeatures;

	public AppearanceSetter driverAppearanceSetter;

	public Animator driverAnimator;

	public Transform driverSeatPosition;

	private void Start()
	{
		EmployeePreset employeePreset = BusinessTypeHelper.GetData("ba:businesstype_empty").uniforms.First((EmployeePreset x) => x.skill == "ba:skill_deliverydriver");
		driverAppearanceSetter.transform.position = driverSeatPosition.position;
		driverAppearanceSetter.SetRandomAppearance();
		driverAppearanceSetter.UpdateVisuals((driverAppearanceSetter.data.gender == Gender.Male) ? employeePreset.maleElements : employeePreset.femaleElements);
		carFeatures.bodyMeshes = carFeatures.bodyMeshes.Append(driverAppearanceSetter.meshCombiner.resultMergeGameObject.GetComponent<Renderer>()).ToArray();
	}

	private void OnEnable()
	{
		driverAnimator.SetBool(PermanentAnimationType.SitDeliveryTruck);
	}
}
