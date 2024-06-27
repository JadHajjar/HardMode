using Colossal.Serialization.Entities;

using Game;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Simulation;
using Game.Tools;

using HardMode.Utility;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace HardMode.Systems
{
	internal partial class CondemnedDemolitionSystem : GameSystemBase
	{
		private BulldozeCostSystem m_BulldozeCostSystem;
		private BulldozeDemolitionSystem m_BulldozeDemolitionSystem;
		private CitySystem m_CitySystem;
		private EndFrameBarrier m_EndFrameBarrier;
		private EntityQuery m_CondemnedQuery;

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 512;
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			World.GetOrCreateSystemManaged<CondemnedBuildingSystem>().Enabled = false;

			m_BulldozeCostSystem = World.GetOrCreateSystemManaged<BulldozeCostSystem>();
			m_BulldozeDemolitionSystem = World.GetOrCreateSystemManaged<BulldozeDemolitionSystem>();
			m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
			m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
			m_CondemnedQuery = SystemAPI.QueryBuilder().WithAll<Condemned, Building, UpdateFrame>().WithNone<Destroyed, Deleted, Temp>().Build();

			RequireForUpdate(m_CondemnedQuery);
		}

		protected override void OnGameLoaded(Context serializationContext)
		{
			base.OnGameLoaded(serializationContext);

			World.GetOrCreateSystemManaged<CondemnedBuildingSystem>().Enabled = false;
		}

		protected override void OnUpdate()
		{
			var commandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
			var entities = m_CondemnedQuery.ToEntityArray(Allocator.Temp);
			var m_City = m_CitySystem.City;

			for (var i = 0; i < entities.Length; i++)
			{
				var entity = entities[i];

				if (EntityManager.HasComponent<UnderConstruction>(entity))
				{
					commandBuffer.AddComponent<Deleted>(entity);
				}
				else if (GetRandomNumber(ref entities, entity) == 0)
				{
					DemolitionUtility.TriggerControlledDemolition(EntityManager, entity, commandBuffer, m_City, m_BulldozeCostSystem, false);

					commandBuffer.AddComponent<Deleted>(entity);
				}
			}
		}

		private static int GetRandomNumber(ref NativeArray<Entity> entities, Entity entity)
		{
			var randomSeed = RandomSeed.Next().GetRandom(entity.Index * 10000);

			return Mod.Settings.EconomyDifficulty switch
			{
				Domain.EconomyDifficulty.Easy => randomSeed.NextInt(math.clamp(entities.Length / 4, 1, 6)),
				Domain.EconomyDifficulty.Medium => randomSeed.NextInt(math.clamp(entities.Length / 2, 2, 9)),
				Domain.EconomyDifficulty.Hard => randomSeed.NextInt(math.clamp(entities.Length * 3 / 4, 4, 14)),
				Domain.EconomyDifficulty.GoodLuck => randomSeed.NextInt(math.clamp(entities.Length, 6, 20)),
				_ => 0,
			};
		}
	}
}
