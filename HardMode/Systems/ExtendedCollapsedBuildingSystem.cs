using Colossal.Collections;
using Colossal.Serialization.Entities;

using Game;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;

using HardMode.Domain;
using HardMode.Utility;

using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using ServiceUpgrade = Game.Buildings.ServiceUpgrade;

namespace HardMode.Systems
{
	public partial class ExtendedCollapsedBuildingSystem : GameSystemBase
	{
		[BurstCompile]
		private struct CollapsedBuildingJob : IJobChunk
		{
			[ReadOnly]
			public EntityTypeHandle m_EntityType;

			[ReadOnly]
			public ComponentTypeHandle<RescueTarget> m_RescueTargetType;

			[ReadOnly]
			public ComponentTypeHandle<ServiceUpgrade> m_ServiceUpgradeType;

			[ReadOnly]
			public ComponentTypeHandle<Owner> m_OwnerType;

			[ReadOnly]
			public ComponentTypeHandle<Extension> m_ExtensionType;

			[ReadOnly]
			public ComponentTypeHandle<Attached> m_AttachedType;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

			[ReadOnly]
			public ComponentTypeHandle<ControlledDemolition> m_ControledDemolition;

			public ComponentTypeHandle<Destroyed> m_DestroyedType;

			[ReadOnly]
			public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

			[ReadOnly]
			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			[ReadOnly]
			public ComponentLookup<BuildingData> m_PrefabBuildingData;

			[ReadOnly]
			public EntityArchetype m_RescueRequestArchetype;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				var nativeArray = chunk.GetNativeArray(m_EntityType);
				var nativeArray2 = chunk.GetNativeArray(ref m_DestroyedType);
				var nativeArray3 = chunk.GetNativeArray(ref m_RescueTargetType);
				var nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
				var autoControlled = chunk.Has(ref m_ServiceUpgradeType) && !chunk.Has(ref m_OwnerType);
				var isControlled = autoControlled || chunk.Has(ref m_ControledDemolition);
				var flag = chunk.Has(ref m_AttachedType);
				var flag2 = !isControlled && (chunk.Has(ref m_ServiceUpgradeType) || chunk.Has(ref m_ExtensionType));

				if (nativeArray3.Length != 0)
				{
					for (var i = 0; i < nativeArray2.Length; i++)
					{
						var destroyed = nativeArray2[i];
						var entity = nativeArray[i];
						if (destroyed.m_Cleared < 1f)
						{
							var rescueTarget = nativeArray3[i];
							RequestRescueIfNeeded(unfilteredChunkIndex, entity, rescueTarget);
						}
						else
						{
							m_CommandBuffer.RemoveComponent<RescueTarget>(unfilteredChunkIndex, entity);
						}
					}
				}
				else
				{
					for (var j = 0; j < nativeArray2.Length; j++)
					{
						ref var reference = ref nativeArray2.ElementAt(j);
						var prefabRef = nativeArray4[j];
						var flag3 = false;
						if (m_PrefabBuildingData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
						{
							flag3 = (componentData.m_Flags & Game.Prefabs.BuildingFlags.RequireRoad) != 0;
						}

						if (reference.m_Cleared < 0f)
						{
							var e = nativeArray[j];
							reference.m_Cleared += 1.0666667f;
							if (reference.m_Cleared >= 0f)
							{
								reference.m_Cleared = math.select(1f, 0f, flag3);
								m_CommandBuffer.RemoveComponent<InterpolatedTransform>(unfilteredChunkIndex, e);
								m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(Updated));
							}
						}
						else if (isControlled || (reference.m_Cleared < 1f && !flag2))
						{
							if (isControlled)
							{
								m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[j], new DemolitionTimer { mTimer = DemolitionUtility.DemolitionTimerValue + (autoControlled ? 1 : 0) });
							}
							else if (flag3)
							{
								var entity2 = nativeArray[j];
								var rescueTarget2 = default(RescueTarget);
								RequestRescueIfNeeded(unfilteredChunkIndex, entity2, rescueTarget2);
								m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, rescueTarget2);
							}
							else
							{
								reference.m_Cleared = 1f;
							}
						}
					}
				}

				for (var k = 0; k < nativeArray2.Length; k++)
				{
					if (!(nativeArray2[k].m_Cleared >= 1f) || flag2)
					{
						continue;
					}

					var e2 = nativeArray[k];
					var prefabRef2 = nativeArray4[k];
					if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
					{
						if ((componentData2.m_Flags & GeometryFlags.Overridable) != 0 && !flag)
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, e2, default(Deleted));
						}
					}
					else
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e2, default(Deleted));
					}
				}
			}

			private void RequestRescueIfNeeded(int jobIndex, Entity entity, RescueTarget rescueTarget)
			{
				if (!m_FireRescueRequestData.HasComponent(rescueTarget.m_Request))
				{
					var e = m_CommandBuffer.CreateEntity(jobIndex, m_RescueRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new FireRescueRequest(entity, 10f, FireRescueRequestType.Disaster));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
				}
			}
		}

		private const int UPDATE_INTERVAL = 64;

		private EndFrameBarrier m_EndFrameBarrier;

		private EntityQuery m_CollapsedQuery;

		private EntityArchetype m_RescueRequestArchetype;

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return UPDATE_INTERVAL;
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
			m_CollapsedQuery = SystemAPI.QueryBuilder()
				.WithAll<Destroyed>()
				.WithAny<Building, ServiceUpgrade, Extension>()
				.WithNone<DemolitionTimer, Deleted, Temp>()
				.Build();

			RequireForUpdate(m_CollapsedQuery);
		}

		protected override void OnGameLoaded(Context serializationContext)
		{
			base.OnGameLoaded(serializationContext);

			World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CollapsedBuildingSystem>().Enabled = false;
		}

		protected override void OnUpdate()
		{
			var collapsedBuildingJob = default(CollapsedBuildingJob);

			collapsedBuildingJob.m_EntityType = SystemAPI.GetEntityTypeHandle();
			collapsedBuildingJob.m_RescueTargetType = SystemAPI.GetComponentTypeHandle<RescueTarget>(isReadOnly: true);
			collapsedBuildingJob.m_ServiceUpgradeType = SystemAPI.GetComponentTypeHandle<ServiceUpgrade>(isReadOnly: true);
			collapsedBuildingJob.m_OwnerType = SystemAPI.GetComponentTypeHandle<Owner>(isReadOnly: true);
			collapsedBuildingJob.m_ExtensionType = SystemAPI.GetComponentTypeHandle<Extension>(isReadOnly: true);
			collapsedBuildingJob.m_AttachedType = SystemAPI.GetComponentTypeHandle<Attached>(isReadOnly: true);
			collapsedBuildingJob.m_PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			collapsedBuildingJob.m_DestroyedType = SystemAPI.GetComponentTypeHandle<Destroyed>();
			collapsedBuildingJob.m_ControledDemolition = SystemAPI.GetComponentTypeHandle<ControlledDemolition>();
			collapsedBuildingJob.m_FireRescueRequestData = SystemAPI.GetComponentLookup<FireRescueRequest>(isReadOnly: true);
			collapsedBuildingJob.m_PrefabObjectGeometryData = SystemAPI.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			collapsedBuildingJob.m_PrefabBuildingData = SystemAPI.GetComponentLookup<BuildingData>(isReadOnly: true);
			collapsedBuildingJob.m_RescueRequestArchetype = m_RescueRequestArchetype;
			collapsedBuildingJob.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();

			Dependency = JobChunkExtensions.ScheduleParallel(collapsedBuildingJob, m_CollapsedQuery, Dependency);

			m_EndFrameBarrier.AddJobHandleForProducer(Dependency);
		}
	}
}
