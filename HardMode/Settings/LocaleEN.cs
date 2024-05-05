using Colossal;

using HardMode.Domain;

using System.Collections.Generic;

namespace HardMode.Settings
{
	public class LocaleEN : IDictionarySource
	{
		private readonly HardModeSettings m_Setting;
		public LocaleEN(HardModeSettings setting)
		{
			m_Setting = setting;
		}

		public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
		{
			return new Dictionary<string, string>
			{
				{ m_Setting.GetSettingsLocaleID(), "Hard Mode" },

				{ m_Setting.GetOptionGroupLocaleID(HardModeSettings.GAMEPLAY_GROUP), "Gameplay Options" },

				{ m_Setting.GetOptionLabelLocaleID(nameof(HardModeSettings.EconomyDifficulty)), "Economy Difficulty" },
				{ m_Setting.GetOptionDescLocaleID(nameof(HardModeSettings.EconomyDifficulty)), "Higher difficulty results in decreased subsidies, milestone rewards, income & higher expenses." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(HardModeSettings.BulldozeCostsMoney)), "Bulldozing Building Costs Money" },
				{ m_Setting.GetOptionDescLocaleID(nameof(HardModeSettings.BulldozeCostsMoney)), $"Adds a monthly maintenance fee for demolition buildings. This fee is based on how many residents are getting evicted, how high the land value is, and how large is the building itself." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(HardModeSettings.BulldozeCausesDemolition)), "Bulldoze & De-zoning Causes Demolition" },
				{ m_Setting.GetOptionDescLocaleID(nameof(HardModeSettings.BulldozeCausesDemolition)), $"Bulldozing a building or de-zoning an area will cause the buildings to collapse instead of disappearing, collapsed buildings take time to be cleaned up and affect nearby happiness. You can pay a small fee to immediately raze collapsed buildings." },

				{ m_Setting.GetEnumValueLocaleID(EconomyDifficulty.Easy), "Easy" },
				{ m_Setting.GetEnumValueLocaleID(EconomyDifficulty.Medium), "Medium" },
				{ m_Setting.GetEnumValueLocaleID(EconomyDifficulty.Hard), "Hard" },
				{ m_Setting.GetEnumValueLocaleID(EconomyDifficulty.GoodLuck), "Good Luck" },

				{ "EconomyPanel.BUDGET_SUB_ITEM[ExportedGoods]", "Exported Goods" },
				{ "EconomyPanel.BUDGET_SUB_ITEM[ImportedGoods]", "Imported Goods" },
				{ "EconomyPanel.BUDGET_SUB_ITEM[BuildingDemolitions]", "Building Demolitions" },
				{ "HardMode.BULLDOZECOST_LABEL", "Demolition Cost" },
			};
		}

		public void Unload()
		{

		}
	}
}