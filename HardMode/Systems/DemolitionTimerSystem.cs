using Game;
using Game.Common;
using Game.Prefabs;
using Game.Tools;

using HardMode.Domain;

using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace HardMode.Systems
{
	internal partial class DemolitionTimerSystem : GameSystemBase
	{
		public const int UPDATE_INTERVAL = 512;

		private const string k_BuildingCollapsePrefab = "Building Collapse";
		private EntityQuery m_DemolitionTimerQuery;
		private Entity m_BuildingCollapseEntity;
		private EndFrameBarrier m_EndFrameBarrier;

		public Entity BuildingCollapseEntity => m_BuildingCollapseEntity;

		protected override void OnCreate()
		{
			base.OnCreate();

			var m_EventQuery = SystemAPI.QueryBuilder().WithAll<EventData>().Build();
			var nativeArray = m_EventQuery.ToEntityArray(Allocator.TempJob);
			var m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

			for (var i = 0; i < nativeArray.Length; i++)
			{
				var entity = nativeArray[i];
				var prefab = m_PrefabSystem.GetPrefab<EventPrefab>(entity);

				if (prefab.name == k_BuildingCollapsePrefab)
				{
					m_BuildingCollapseEntity = entity;
					break;
				}
			}

			m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
			m_DemolitionTimerQuery = SystemAPI.QueryBuilder().WithAll<DemolitionTimer>().WithNone<Deleted, Temp>().Build();

			RequireForUpdate(m_DemolitionTimerQuery);
		}

		protected override void OnUpdate()
		{
			var job = default(DemolitionTimerTickJob);
			job.m_DemolitionTimerTypeHandle = SystemAPI.GetComponentTypeHandle<DemolitionTimer>();
			job.m_EntityTypeHandle = SystemAPI.GetEntityTypeHandle();
			job.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();

			Dependency = JobChunkExtensions.ScheduleParallel(job, m_DemolitionTimerQuery, Dependency);

			m_EndFrameBarrier.AddJobHandleForProducer(Dependency);
		}

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return UPDATE_INTERVAL;
		}

		[BurstCompile]
		private struct DemolitionTimerTickJob : IJobChunk
		{
			internal EntityTypeHandle m_EntityTypeHandle;
			internal ComponentTypeHandle<DemolitionTimer> m_DemolitionTimerTypeHandle;
			internal EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var timers = chunk.GetNativeArray(ref m_DemolitionTimerTypeHandle);
				var entities = chunk.GetNativeArray(m_EntityTypeHandle);

				for (var i = 0; i < timers.Length; i++)
				{
					var timer = timers[i];

					timer.mTimer -= UPDATE_INTERVAL;

					timers[i] = timer;

					if (timer.mTimer <= 0)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entities[i], default(Deleted));
					}
				}
			}
		}
	}
}
