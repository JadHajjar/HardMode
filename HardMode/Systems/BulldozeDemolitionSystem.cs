using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Simulation;
using Game.Tools;

using HardMode.Utility;

using Unity.Collections;
using Unity.Entities;

namespace HardMode.Systems
{
	public partial class BulldozeDemolitionSystem : SystemBase
	{
		private ToolOutputBarrier m_ToolOutputBarrier;
		private ToolSystem m_ToolSystem;
		private BulldozeCostSystem m_BulldozeCostSystem;
		private EntityQuery m_DefinitionQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_BulldozeCostSystem = World.GetOrCreateSystemManaged<BulldozeCostSystem>();
			m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
			m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
			m_DefinitionQuery = SystemAPI.QueryBuilder().WithAll<CreationDefinition>().WithNone<Updated>().Build();

			RequireForUpdate(m_DefinitionQuery);
		}

		protected override void OnUpdate()
		{
			if (m_ToolSystem.activeTool.toolID is not "Bulldoze Tool" || m_ToolSystem.activeTool.applyMode != ApplyMode.Apply)
			{
				return;
			}

			var nativeArray = m_DefinitionQuery.ToComponentDataArray<CreationDefinition>(Allocator.Temp);
			var commandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
			var m_City = World.GetOrCreateSystemManaged<CitySystem>().City;

			for (var i = 0; i < nativeArray.Length; i++)
			{
				var definition = nativeArray[i];

				if (definition.m_Flags.HasFlag(CreationFlags.Delete)
					&& definition.m_Original != Entity.Null
					&& EntityManager.HasComponent<Building>(definition.m_Original)
					&& !EntityManager.HasComponent<UnderConstruction>(definition.m_Original)
					&& !EntityManager.HasComponent<Destroyed>(definition.m_Original))
				{
					var entity = definition.m_Original;

					DemolitionUtility.TriggerControlledDemolition(EntityManager, entity, commandBuffer, m_City, m_BulldozeCostSystem, true);

					commandBuffer.AddComponent<Deleted>(entity);
				}
			}
		}
	}
}