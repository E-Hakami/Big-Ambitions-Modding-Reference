using System;
using System.Collections.Generic;
using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateCompletedQuestForNewTutorial : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.completedSideQuestEntries = new List<string>();
		gameInstance.activeSideQuestEntries = new List<string>();
		if (gameInstance.CompletedQuestEntries.Contains("9782C42A-3050-46C4-8319-5F838F59D539"))
		{
			Diploma diploma = gameInstance.PlayerDiplomas.SingleOrDefault((Diploma x) => x.name == DiplomaName.Headquarters);
			if (diploma != null)
			{
				diploma.minutesStudied = 1920;
				diploma.completed = true;
			}
			else
			{
				gameInstance.PlayerDiplomas.Add(new Diploma
				{
					name = DiplomaName.Headquarters,
					minutesStudied = 1920,
					completed = true
				});
			}
		}
		if (gameInstance.CompletedQuestEntries.Contains("CEDADB49-4133-4DF2-B415-B8738D9B261D"))
		{
			Diploma diploma2 = gameInstance.PlayerDiplomas.SingleOrDefault((Diploma x) => x.name == DiplomaName.Headquarters);
			if (diploma2 != null)
			{
				diploma2.minutesStudied = 1920;
				diploma2.completed = true;
			}
			else
			{
				gameInstance.PlayerDiplomas.Add(new Diploma
				{
					name = DiplomaName.OfficeBusinesses,
					minutesStudied = 2160,
					completed = true
				});
			}
		}
		string[] array = gameInstance.CompletedQuestEntries.ToArray();
		gameInstance.CompletedQuestEntries.Clear();
		for (int num = 0; num < array.Length; num++)
		{
			string[] parsedQuestEntryId = GetParsedQuestEntryId(array[num]);
			foreach (string item in parsedQuestEntryId)
			{
				gameInstance.CompletedQuestEntries.Add(item);
			}
		}
	}

	private string[] GetParsedQuestEntryId(string oldQuestEntryId)
	{
		return oldQuestEntryId switch
		{
			"720b45a6-987c-40fc-a330-37f5f867c8a5" => new string[4] { "tutorial_quest_rent_your_apartment_objective_1", "tutorial_quest_rent_your_apartment_objective_2", "tutorial_quest_get_some_sleep_objective_2", "tutorial_quest_get_some_sleep_objective_3" }, 
			"4BB6D48F-906D-4B42-B8EA-7FA8E858D99F" => new string[1] { "tutorial_quest_get_some_sleep_objective_4" }, 
			"531d0beb-9d55-4fa5-aa84-9de565bd1560" => new string[1] { "tutorial_quest_get_some_food_objective_2" }, 
			"4C7B47A9-7F66-42D0-9926-198E159F9FA6" => new string[1] { "tutorial_quest_get_some_food_objective_3" }, 
			"C13C2B82-5438-4554-88AB-6145BB6FCC2A" => new string[1] { "tutorial_quest_get_some_food_objective_1" }, 
			"BEC01EAC-9D86-4F8E-A2FA-64269FC2FD0A" => new string[1] { "tutorial_quest_get_some_food_objective_4" }, 
			"1EC3B828-CFD1-43D4-8717-538B371FFDE8" => new string[1] { "tutorial_quest_get_some_food_objective_5" }, 
			"47AD87C3-A5C7-4117-9E39-C9AA5CEF8B73" => new string[2] { "tutorial_quest_find_a_job_objective_1", "tutorial_quest_find_a_job_objective_2" }, 
			"211E1B04-9190-464B-BC72-DD28FD8A8AA9" => new string[1] { "tutorial_quest_find_a_job_objective_3" }, 
			"452C745E-F86A-4F53-8B7E-053F3CB96CD4" => new string[1] { "tutorial_quest_establish_first_business_objective_1" }, 
			"3BFD00B0-6E08-4F53-97A0-73013AEFE151" => new string[1] { "tutorial_quest_establish_first_business_objective_2" }, 
			"3E32DA05-83B2-47F3-BE92-CA23D3607577" => new string[1] { "tutorial_quest_establish_first_business_objective_3" }, 
			"bZl1Bv5c60GjgjsEwQDvUQ==" => new string[1] { "tutorial_quest_moving_in_objective_1" }, 
			"9C0D3EC2-8689-4441-9A2A-3A4284010A42" => new string[1] { "tutorial_quest_moving_in_objective_2" }, 
			"BFC72C78-A771-4DAF-8930-5D235ADBE33B" => new string[1] { "tutorial_quest_moving_in_objective_3" }, 
			"44F21BFF-F6C9-438B-AB7C-675E3B548116" => new string[1] { "tutorial_quest_stocking_up_objective_1" }, 
			"1429FCFE-3C7A-4767-8D78-F0712614CE55" => new string[1] { "tutorial_quest_stocking_up_objective_2" }, 
			"C79607A5-FA1E-4B65-B8FD-08554867BA61" => new string[1] { "tutorial_quest_open_the_store_objective_1" }, 
			"51436544-EEF1-438A-9AFD-D4B5CEC29A39" => new string[1] { "tutorial_quest_open_the_store_objective_2" }, 
			"BC9280B1-1948-4AE2-A649-19C14D50693B" => new string[1] { "tutorial_quest_open_the_store_objective_3" }, 
			"73EE8A48-71AB-4791-8936-F8837D89E7F7" => new string[2] { "tutorial_checkpoint_first_objective", "tutorial_quest_recruiting_objective_1" }, 
			"DF761610-44ED-4875-8B8F-7F651CD470E4" => new string[1] { "tutorial_quest_recruiting_objective_2" }, 
			"AF0AEAB3-2CD7-438A-817F-47E383DD28A2" => new string[1] { "tutorial_quest_recruiting_objective_3" }, 
			"AF0AEAB3-2CD7-438A-817F-47E383DD28A1" => new string[1] { "tutorial_quest_recruiting_objective_4" }, 
			"31BA5868-B198-4406-BFEB-EE37008CEA87" => new string[1] { "tutorial_quest_cleaning_objective_1" }, 
			"17B1D207-B191-4060-B0F4-47BF227E8A7E" => new string[1] { "tutorial_quest_cleaning_objective_2" }, 
			"9329FCF3-0C4B-4D12-A5A0-5DBDEDFDDE63" => new string[1] { "tutorial_quest_cleaning_objective_3" }, 
			"E9AB6724-2C36-4910-B6FB-9C1A36992DC5" => new string[1] { "tutorial_quest_expanding_product_line_objective_1" }, 
			"1725F5E8-C733-44A2-87B0-B7F4D82B6521" => new string[1] { "tutorial_quest_expanding_product_line_objective_2" }, 
			"29F8D56D-426E-4276-BEA9-E1B6A5E4BAA2" => new string[1] { "tutorial_quest_expanding_product_line_objective_3" }, 
			"278A9EDE-2903-44AA-AAC6-8585FB409706" => new string[2] { "tutorial_checkpoint_second_objective", "tutorial_quest_multiple_stores_objective_1" }, 
			"75A5C995-8C6D-4DEE-9C71-6B85C9FFB933" => new string[1] { "tutorial_quest_another_business_objective_1" }, 
			"4531C6CD-7B88-4CB4-885C-EFE7FCB036CE" => new string[2] { "tutorial_quest_another_business_objective_2", "tutorial_quest_another_business_objective_3" }, 
			"ea1803ec2e4a427193d87b36ade9ab04" => new string[4] { "tutorial_quest_setup_store_objective_1", "tutorial_quest_setup_store_objective_2", "tutorial_quest_setup_store_objective_3", "tutorial_quest_setup_store_objective_4" }, 
			"2RptwJ7vE+LqVzRt3laA==" => new string[1] { "tutorial_quest_setup_store_objective_5" }, 
			"0A7734A5-789C-43D0-917D-6E61F54C165C" => new string[1] { "tutorial_quest_ensure_profit_objective_1" }, 
			"90F91EA4-71D7-49E2-A1FB-50A8B48F43E5" => new string[1] { "tutorial_quest_marketing_objective_1" }, 
			"748DD487-C682-43F1-B117-FF69351B20D0" => new string[1] { "tutorial_quest_pretty_interior_objective_2" }, 
			"e3fScZMGHUiOX5+bph1zAQ==" => new string[2] { "tutorial_quest_pretty_interior_objective_3", "tutorial_quest_pretty_interior_objective_4" }, 
			"A00BB514-4ABA-48BD-B6BA-0DA088987AB9" => new string[1] { "tutorial_quest_deliveries_objective_1" }, 
			"84afSwzv0+m1yFMtNBsgw==" => new string[1] { "tutorial_quest_deliveries_objective_2" }, 
			"2EDFFDFD-872E-4EC3-9400-E83944AAAC4F" => new string[1] { "tutorial_quest_get_some_sleep_objective_1" }, 
			"6171FD4B-0AC5-40A2-882A-13640A287AA9" => new string[1] { "tutorial_quest_employee_upgrades_objective_1" }, 
			"7DEAFJuke0qTvUcmOkkMjw==" => new string[1] { "tutorial_quest_employee_upgrades_objective_2" }, 
			"CD6BBF02-A6B6-4E3E-A3C4-F7D02FED051A" => new string[2] { "tutorial_quest_employee_upgrades_objective_3", "tutorial_quest_employee_upgrades_objective_4" }, 
			"55E01B5E-3774-4344-8FEA-F2FD3A364FD0" => new string[1] { "tutorial_quest_pretty_interior_objective_1" }, 
			"7a891640bc694029b4e078e171680c50" => new string[1] { "tutorial_quest_money_cant_buy_happiness_objective_1" }, 
			"99fdc34f54894dd391a4d321cd72e31c" => new string[1] { "tutorial_quest_money_cant_buy_happiness_objective_2" }, 
			"886a336ecb6c454fae9147123df4b69e" => new string[2] { "tutorial_quest_money_cant_buy_happiness_objective_3", "tutorial_checkpoint_third_objective" }, 
			"29BFE092-AC46-4845-8962-E0875CA4A39B" => new string[1] { "tutorial_quest_first_hq_objective_2" }, 
			"9782C42A-3050-46C4-8319-5F838F59D539" => new string[10] { "tutorial_quest_first_hq_objective_1", "tutorial_quest_first_hq_objective_3", "tutorial_quest_building_wealth_objective_1", "tutorial_quest_building_wealth_objective_2", "tutorial_quest_using_blueprints_objective_1", "tutorial_quest_using_blueprints_objective_2", "tutorial_quest_using_blueprints_objective_3", "tutorial_quest_using_blueprints_objective_4", "tutorial_quest_profit_all_three_objective_1", "tutorial_quest_profit_all_three_objective_2" }, 
			"41847B20-A801-4B89-89AB-62D4D12631A8" => new string[1] { "tutorial_quest_purchasing_agent_objective_1" }, 
			"21D2F7C9-DA1D-499F-9872-75E47FCE782A" => new string[1] { "tutorial_quest_purchasing_agent_objective_2" }, 
			"F9535D48-25A6-4155-89DA-801BDE4EF182" => new string[1] { "tutorial_quest_purchasing_agent_objective_3" }, 
			"BAF0B0DC-FA7D-48E1-9574-720F6702AABB" => new string[1] { "tutorial_quest_purchasing_agent_objective_4" }, 
			"0DDC79AA-F771-4DC9-BE87-459934292217" => new string[2] { "tutorial_quest_rent_warehouse_objective_1", "tutorial_quest_rent_warehouse_objective_2" }, 
			"C01ABDE4-1899-47BC-86D0-E8071E7311B8" => new string[1] { "tutorial_quest_rent_warehouse_objective_3" }, 
			"632D5F63-FDB2-466A-9EE9-2505DE15434B" => new string[1] { "tutorial_quest_rent_warehouse_objective_4" }, 
			"BCDF2F36-9665-4350-A8F0-618832570D16" => new string[1] { "tutorial_quest_first_import_contract_objective_1" }, 
			"C3786397-7F20-4ED3-953B-55A40A22C761" => new string[1] { "tutorial_quest_first_import_contract_objective_2" }, 
			"6925E7C9-311E-4D56-A2DB-34172E59C4B4" => new string[1] { "tutorial_quest_first_import_contract_objective_3" }, 
			"44786935-DC83-4EDB-A230-E64E082EC5FC" => new string[1] { "tutorial_quest_warehouse_driver_objective_1" }, 
			"32A60194-C900-4751-BD0C-A1DC8B6B8E5E" => new string[1] { "tutorial_quest_warehouse_driver_objective_2" }, 
			"034B7003-3DCA-4DAA-A5DE-34C1DE180C17" => new string[1] { "tutorial_quest_warehouse_driver_objective_3" }, 
			"BC49EE2A-5707-4AB3-B805-893984D22591" => new string[1] { "tutorial_quest_warehouse_driver_objective_4" }, 
			"1866D457-A085-4410-9F33-8C8140821960" => new string[1] { "tutorial_quest_logistics_network_objective_1" }, 
			"356F0CF0-F9CD-4D9D-A5F5-8299E7CC2034" => new string[2] { "tutorial_quest_logistics_network_objective_2", "tutorial_quest_logistics_network_objective_3" }, 
			"62D19F86-E091-403D-97C2-834527068D50" => new string[3] { "tutorial_quest_logistics_network_objective_4", "tutorial_quest_logistics_network_objective_5", "tutorial_checkpoint_fourth_objective" }, 
			"CEDADB49-4133-4DF2-B415-B8738D9B261D" => new string[1] { "tutorial_quest_office_business_objective_1" }, 
			"00EB935C-80D0-4DC5-B0FB-7DA02DF45FBD" => new string[1] { "tutorial_quest_office_business_objective_2" }, 
			"960419D9-D51D-482A-822F-E8408CA0D405" => new string[1] { "tutorial_quest_office_business_objective_3" }, 
			"23E5A47A-F5CA-4195-B5EC-42AF80E95952" => new string[1] { "tutorial_quest_office_business_objective_4" }, 
			"E554F5FA-44FE-4FEC-91A1-28DD2E1D53D7" => new string[1] { "tutorial_quest_casino_objective_1" }, 
			"8DA6A112-0031-4C11-BF06-A418C5924374" => new string[1] { "tutorial_quest_casino_objective_2" }, 
			"36721DA8-9913-46D0-BD8E-88C914E1ADB2" => new string[1] { "tutorial_quest_hr_manager_objective_1" }, 
			"BE6C715B-5B06-4F51-BBA2-76DEB0C1A2EF" => new string[1] { "tutorial_quest_hr_manager_objective_2" }, 
			"5124F559-DCE9-4EBE-8EA2-D5D6E5776CC0" => new string[3] { "tutorial_quest_hr_manager_objective_3", "tutorial_quest_hr_manager_objective_4", "tutorial_quest_hr_manager_objective_5" }, 
			"1B9BAA95-F89A-49E0-9141-D9750212C64D" => new string[3] { "tutorial_quest_investments_objective_1", "tutorial_quest_buyout_business_objective_1", "tutorial_quest_buyout_business_objective_2" }, 
			"25EB5DD5-1A72-4ACC-A40A-EE74B9D98156" => new string[1] { "tutorial_quest_million_dollar_investment_objective_1" }, 
			"AC579CFC-DFF0-4B02-B303-4E6196C41C3E" => new string[3] { "tutorial_quest_headhunter_objective_1", "tutorial_quest_headhunter_objective_2", "tutorial_quest_headhunter_objective_3" }, 
			_ => Array.Empty<string>(), 
		};
	}
}
