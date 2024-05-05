using Colossal.IO.AssetDatabase;
using Colossal.Logging;

using Game;
using Game.Modding;
using Game.SceneFlow;

using HardMode.Settings;
using HardMode.Systems;

using HarmonyLib;

using System;

using Unity.Entities;

namespace HardMode
{
	public class Mod : IMod
	{
		private Harmony _harmony;

		public static ILog Log { get; } = LogManager.GetLogger($"{nameof(HardMode)}.{nameof(Mod)}").SetShowsErrorsInUI(showsErrorsInUI: false);
		public static HardModeSettings Settings { get; private set; }

		public void OnLoad(UpdateSystem updateSystem)
		{
			Log.Info("OnLoad");

			Settings = new HardModeSettings(this);
			Settings.RegisterInOptionsUI();

			GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));

			AssetDatabase.global.LoadSettings(nameof(HardMode), Settings, new HardModeSettings(this)
			{
				DefaultBlock = true
			});

			try
			{
				updateSystem.UpdateAt<DemolitionTimerSystem>(SystemUpdatePhase.GameSimulation);
				updateSystem.UpdateAt<MilestoneNoRewardSystem>(SystemUpdatePhase.GameSimulation);
				updateSystem.UpdateAt<BulldozeCostSystem>(SystemUpdatePhase.ToolUpdate);
				updateSystem.UpdateAfter<BulldozeDemolitionSystem>(SystemUpdatePhase.ToolUpdate);
				updateSystem.UpdateAt<CondemnedDemolitionSystem>(SystemUpdatePhase.GameSimulation);
				updateSystem.UpdateAt<ExtendedCollapsedBuildingSystem>(SystemUpdatePhase.GameSimulation);
				updateSystem.UpdateAt<MoneyApplySystem>(SystemUpdatePhase.GameSimulation);
				updateSystem.UpdateAt<BulldozeCostTooltipSystem>(SystemUpdatePhase.UITooltip);
				updateSystem.UpdateAt<CustomBudgetUISystem>(SystemUpdatePhase.UIUpdate);
				updateSystem.UpdateAt<CustomTaxSystem>(SystemUpdatePhase.GameSimulation);

				_harmony = new Harmony($"{nameof(HardMode)}.{nameof(Mod)}");

				_harmony.PatchAll(typeof(Mod).Assembly);
			}
			catch (Exception ex)
			{
				Log.Fatal(ex);
			}
		}

		public void OnDispose()
		{
			_harmony?.UnpatchAll($"{nameof(HardMode)}.{nameof(Mod)}");
		}

		internal void RefreshSystems()
		{
			World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<MilestoneNoRewardSystem>().Enabled = true;
		}
	}
}
