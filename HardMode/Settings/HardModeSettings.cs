using Colossal.IO.AssetDatabase;

using Game.Modding;
using Game.Settings;

using HardMode.Domain;

namespace HardMode.Settings
{
	[FileLocation(nameof(HardMode))]
	[SettingsUIGroupOrder(GAMEPLAY_GROUP)]
	[SettingsUIShowGroupName(GAMEPLAY_GROUP)]
	public class HardModeSettings : ModSetting
	{
		public const string GAMEPLAY_GROUP = nameof(GAMEPLAY_GROUP);
		private readonly Mod _mod;

		[SettingsUISection(GAMEPLAY_GROUP)]
		public EconomyDifficulty EconomyDifficulty { get; set; } = EconomyDifficulty.Medium;

		[SettingsUISection(GAMEPLAY_GROUP)]
		public bool BulldozeCostsMoney { get; set; } = true;

		[SettingsUISection(GAMEPLAY_GROUP)]
		public bool BulldozeCausesDemolition { get; set; } = true;

		[SettingsUIHidden]
		public bool DefaultBlock { get; set; }

		public HardModeSettings(Mod mod) : base(mod)
		{
			_mod = mod;
		}

		public override void SetDefaults()
		{
			EconomyDifficulty = EconomyDifficulty.Medium;
		}

		public override void Apply()
		{
			base.Apply();

			_mod.RefreshSystems();
		}
	}
}