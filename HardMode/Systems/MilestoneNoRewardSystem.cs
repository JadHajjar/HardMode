using Colossal.Serialization.Entities;

using Game;
using Game.Prefabs;

using Unity.Collections;
using Unity.Entities;

namespace HardMode.Systems
{
	public partial class MilestoneNoRewardSystem : GameSystemBase
	{
		private EntityQuery m_MilestoneGroup;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_MilestoneGroup = SystemAPI.QueryBuilder().WithAllRW<MilestoneData>().Build();
		}

		protected override void OnUpdate()
		{
			ApplyMilestoneRewards();

			Enabled = false;
		}

		protected override void OnGamePreload(Purpose purpose, GameMode mode)
		{
			if (purpose is not Purpose.LoadGame and not Purpose.NewGame || !mode.HasFlag(GameMode.Game))
			{
				return;
			}

			base.OnGamePreload(purpose, mode);
		}

		protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
		{
			if (purpose is not Purpose.LoadGame and not Purpose.NewGame || !mode.HasFlag(GameMode.Game))
			{
				return;
			}

			ApplyMilestoneRewards();
		}

		private void ApplyMilestoneRewards()
		{
			RequireForUpdate(m_MilestoneGroup);

			var milestones = m_MilestoneGroup.ToEntityArray(Allocator.TempJob);

			for (var i = 0; i < milestones.Length; i++)
			{
				var component = EntityManager.GetComponentData<MilestoneData>(milestones[i]);

				component.m_Reward = (Mod.Settings?.EconomyDifficulty) switch
				{
					Domain.EconomyDifficulty.Easy => component.m_Index % 4 == 0 ? 250_000 : 125_000,
					Domain.EconomyDifficulty.Medium => component.m_Index % 4 == 0 ? 200_000 : 100_000,
					Domain.EconomyDifficulty.Hard => component.m_Index % 4 == 0 ? 100_000 : 50_000,
					_ => 0,
				};

				if (component.m_Index == 20)
				{
					component.m_Reward = 1_000_000;
				}

				EntityManager.SetComponentData(milestones[i], component);
			}
		}
	}
}