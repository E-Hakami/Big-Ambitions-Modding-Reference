using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/Customer/GymCustomerPool", fileName = "GymCustomerPool")]
public class GymCustomerPool : CustomerPool
{
	public override CustomerType GetCustomerType()
	{
		return CustomerType.Gym;
	}

	protected override string GetPrefabName()
	{
		return "Characters/GymCustomer";
	}

	protected override void ActionOnRelease(Customer customer)
	{
		customer.tpc.SetHandContent(null);
		customer.tpc.RemoveHandObject();
		customer.tpc.Reset();
		base.ActionOnRelease(customer);
	}
}
