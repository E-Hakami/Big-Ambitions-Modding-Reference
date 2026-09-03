using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/Customer/HairdresserCustomerPool", fileName = "HairdresserCustomerPool")]
public class HairdresserCustomerPool : CustomerPool
{
	public override CustomerType GetCustomerType()
	{
		return CustomerType.Hairdresser;
	}

	protected override string GetPrefabName()
	{
		return "Characters/HairdresserCustomer";
	}

	protected override void ActionOnRelease(Customer customer)
	{
		customer.tpc.SetHandContent(null);
		base.ActionOnRelease(customer);
		customer.tpc.Reset();
	}
}
