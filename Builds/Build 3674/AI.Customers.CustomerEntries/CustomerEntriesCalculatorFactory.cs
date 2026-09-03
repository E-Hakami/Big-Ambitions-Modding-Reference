namespace AI.Customers.CustomerEntries;

public static class CustomerEntriesCalculatorFactory
{
	private static readonly CustomerEntriesCalculator CustomerEntriesCalculator = new CustomerEntriesCalculator();

	private static readonly CustomerEntriesCalculatorRetail CustomerEntriesCalculatorRetail = new CustomerEntriesCalculatorRetail();

	private static readonly CustomerEntriesCalculatorOffice CustomerEntriesCalculatorOffice = new CustomerEntriesCalculatorOffice();

	private static readonly CustomerEntriesCalculatorCasino CustomerEntriesCalculatorCasino = new CustomerEntriesCalculatorCasino();

	private static readonly CustomerEntriesCalculatorCinemaTheater CustomerEntriesCalculatorCinemaTheater = new CustomerEntriesCalculatorCinemaTheater();

	public static CustomerEntriesCalculator GetCustomerEntriesCalculator(BuildingRegistration registration)
	{
		CustomerEntriesCalculator customerEntriesCalculator;
		if (registration.businessTypeName == "ba:businesstype_casino")
		{
			customerEntriesCalculator = CustomerEntriesCalculatorCasino;
		}
		else
		{
			switch (registration.businessTypeName)
			{
			case "ba:businesstype_furniturestore":
			case "ba:businesstype_appliancestore":
			case "ba:businesstype_coffeeshop":
				customerEntriesCalculator = CustomerEntriesCalculatorRetail;
				break;
			default:
			{
				CustomerEntriesCalculator customerEntriesCalculator2;
				switch (registration.GetBuildingType())
				{
				case "ba:buildingtype_retail":
					customerEntriesCalculator2 = CustomerEntriesCalculatorRetail;
					break;
				case "ba:buildingtype_cinema":
				case "ba:buildingtype_theater":
					customerEntriesCalculator2 = CustomerEntriesCalculatorCinemaTheater;
					break;
				case "ba:buildingtype_office":
					customerEntriesCalculator2 = CustomerEntriesCalculatorOffice;
					break;
				default:
					customerEntriesCalculator2 = CustomerEntriesCalculator;
					break;
				}
				customerEntriesCalculator = customerEntriesCalculator2;
				break;
			}
			}
		}
		customerEntriesCalculator.Init(registration);
		return customerEntriesCalculator;
	}
}
