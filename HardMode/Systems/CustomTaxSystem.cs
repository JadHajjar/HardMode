using Game;
using Game.Agents;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;

using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace HardMode.Systems
{
	public partial class CustomTaxSystem : GameSystemBase
	{
		public static readonly int kUpdatesPerDay = 32;

		private EntityQuery m_ResidentialTaxPayerGroup;
		private EntityQuery m_CommercialTaxPayerGroup;
		private EntityQuery m_IndustrialTaxPayerGroup;
		private EntityQuery m_TaxParameterGroup;
		private CityStatisticsSystem m_CityStatisticsSystem;
		private SimulationSystem m_SimulationSystem;
		private ResourceSystem m_ResourceSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_CityStatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
			m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
			m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
			m_ResidentialTaxPayerGroup = GetEntityQuery(ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Resources>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.ReadOnly<Household>());
			m_CommercialTaxPayerGroup = GetEntityQuery(ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Resources>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.ReadOnly<ServiceAvailable>());
			m_IndustrialTaxPayerGroup = GetEntityQuery(ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Resources>(), ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Game.Companies.StorageCompany>(), ComponentType.Exclude<ServiceAvailable>());
			m_TaxParameterGroup = GetEntityQuery(ComponentType.ReadOnly<TaxParameterData>());

			RequireForUpdate(m_TaxParameterGroup);
		}

		protected override void OnUpdate()
		{
			var updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
			var prefabs = m_ResourceSystem.GetPrefabs();
			var payTaxJob = default(PayTaxJob);
			payTaxJob.m_EntityType = SystemAPI.GetEntityTypeHandle();
			payTaxJob.m_TaxPayerType = SystemAPI.GetComponentTypeHandle<TaxPayer>(true);
			payTaxJob.m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>();
			payTaxJob.m_ResourceType = SystemAPI.GetBufferTypeHandle<Resources>(false);
			payTaxJob.m_HouseholdCitizens = SystemAPI.GetBufferLookup<HouseholdCitizen>(true);
			payTaxJob.m_Prefabs = SystemAPI.GetComponentLookup<PrefabRef>(true);
			payTaxJob.m_ProcessDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true);
			payTaxJob.m_Workers = SystemAPI.GetComponentLookup<Worker>(true);
			payTaxJob.m_ResourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true);
			payTaxJob.m_ResourcePrefabs = prefabs;
			payTaxJob.m_Type = IncomeSource.TaxResidential;
			payTaxJob.m_UpdateFrameIndex = updateFrame;
			payTaxJob.m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out var deps).AsParallelWriter();
			var jobHandle = JobChunkExtensions.ScheduleParallel(payTaxJob, m_ResidentialTaxPayerGroup, JobHandle.CombineDependencies(Dependency, deps));
			m_CityStatisticsSystem.AddWriter(jobHandle);

			payTaxJob = default;
			payTaxJob.m_EntityType = SystemAPI.GetEntityTypeHandle();
			payTaxJob.m_TaxPayerType = SystemAPI.GetComponentTypeHandle<TaxPayer>(true);
			payTaxJob.m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>();
			payTaxJob.m_ResourceType = SystemAPI.GetBufferTypeHandle<Resources>(false);
			payTaxJob.m_HouseholdCitizens = SystemAPI.GetBufferLookup<HouseholdCitizen>(true);
			payTaxJob.m_Prefabs = SystemAPI.GetComponentLookup<PrefabRef>(true);
			payTaxJob.m_ProcessDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true);
			payTaxJob.m_Workers = SystemAPI.GetComponentLookup<Worker>(true);
			payTaxJob.m_ResourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true);
			payTaxJob.m_ResourcePrefabs = prefabs;
			payTaxJob.m_Type = IncomeSource.TaxCommercial;
			payTaxJob.m_UpdateFrameIndex = updateFrame;
			payTaxJob.m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter();
			var jobHandle2 = JobChunkExtensions.ScheduleParallel(payTaxJob, m_CommercialTaxPayerGroup, JobHandle.CombineDependencies(Dependency, deps));
			m_CityStatisticsSystem.AddWriter(jobHandle2);

			payTaxJob = default;
			payTaxJob.m_EntityType = SystemAPI.GetEntityTypeHandle();
			payTaxJob.m_TaxPayerType = SystemAPI.GetComponentTypeHandle<TaxPayer>(true);
			payTaxJob.m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>();
			payTaxJob.m_ResourceType = SystemAPI.GetBufferTypeHandle<Resources>(false);
			payTaxJob.m_HouseholdCitizens = SystemAPI.GetBufferLookup<HouseholdCitizen>(true);
			payTaxJob.m_Prefabs = SystemAPI.GetComponentLookup<PrefabRef>(true);
			payTaxJob.m_ProcessDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true);
			payTaxJob.m_Workers = SystemAPI.GetComponentLookup<Worker>(true);
			payTaxJob.m_ResourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true);
			payTaxJob.m_ResourcePrefabs = prefabs;
			payTaxJob.m_Type = IncomeSource.TaxIndustrial;
			payTaxJob.m_UpdateFrameIndex = updateFrame;
			payTaxJob.m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter();
			var jobHandle3 = JobChunkExtensions.ScheduleParallel(payTaxJob, m_IndustrialTaxPayerGroup, JobHandle.CombineDependencies(Dependency, deps));
			m_CityStatisticsSystem.AddWriter(jobHandle3);

			Dependency = JobHandle.CombineDependencies(jobHandle, jobHandle2, jobHandle3);
		}

		[BurstCompile]
		private struct PayTaxJob : IJobChunk
		{
			public EntityTypeHandle m_EntityType;

			public ComponentTypeHandle<TaxPayer> m_TaxPayerType;

			public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

			public BufferTypeHandle<Resources> m_ResourceType;

			public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

			public ComponentLookup<Worker> m_Workers;

			public ComponentLookup<PrefabRef> m_Prefabs;

			public ComponentLookup<IndustrialProcessData> m_ProcessDatas;

			public ComponentLookup<ResourceData> m_ResourceDatas;

			public ResourcePrefabs m_ResourcePrefabs;

			public uint m_UpdateFrameIndex;

			public IncomeSource m_Type;

			public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

			private void PayTax(ref TaxPayer taxPayer, Entity entity, DynamicBuffer<Resources> resources, IncomeSource taxType, NativeQueue<StatisticsEvent>.ParallelWriter statisticsEventQueue)
			{
				var tax = TaxSystem.GetTax(taxPayer);
				EconomyUtils.AddResources(Resource.Money, -tax, resources);
				if (tax != 0)
				{
					if (taxType == IncomeSource.TaxResidential)
					{
						statisticsEventQueue.Enqueue(new StatisticsEvent
						{
							m_Statistic = StatisticType.Income,
							m_Change = tax * kUpdatesPerDay / 2,
							m_Parameter = (int)taxType
						});
						var parameter = 0;
						if (m_HouseholdCitizens.HasBuffer(entity))
						{
							var dynamicBuffer = m_HouseholdCitizens[entity];
							for (var i = 0; i < dynamicBuffer.Length; i++)
							{
								var citizen = dynamicBuffer[i].m_Citizen;
								if (m_Workers.HasComponent(citizen))
								{
									parameter = m_Workers[citizen].m_Level;
									break;
								}
							}
						}

						statisticsEventQueue.Enqueue(new StatisticsEvent
						{
							m_Statistic = StatisticType.ResidentialTaxableIncome,
							m_Change = taxPayer.m_UntaxedIncome * kUpdatesPerDay / 2,
							m_Parameter = parameter
						});
					}
					else
					{
						var parameter2 = 0;
						var statisticType = (taxType == IncomeSource.TaxCommercial) ? StatisticType.CommercialTaxableIncome : StatisticType.IndustrialTaxableIncome;
						if (m_Prefabs.HasComponent(entity))
						{
							var prefab = m_Prefabs[entity].m_Prefab;
							if (m_ProcessDatas.HasComponent(prefab))
							{
								var resource = m_ProcessDatas[prefab].m_Output.m_Resource;
								parameter2 = EconomyUtils.GetResourceIndex(resource);
								if (statisticType == StatisticType.IndustrialTaxableIncome && m_ResourceDatas[m_ResourcePrefabs[resource]].m_Weight == 0f)
								{
									taxType = IncomeSource.TaxOffice;
									statisticType = StatisticType.OfficeTaxableIncome;
								}
							}
						}

						statisticsEventQueue.Enqueue(new StatisticsEvent
						{
							m_Statistic = StatisticType.Income,
							m_Change = tax * kUpdatesPerDay / 2,
							m_Parameter = (int)taxType
						});
						statisticsEventQueue.Enqueue(new StatisticsEvent
						{
							m_Statistic = statisticType,
							m_Change = taxPayer.m_UntaxedIncome * kUpdatesPerDay / 2,
							m_Parameter = parameter2
						});
					}
				}

				taxPayer.m_UntaxedIncome = 0;
			}

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
				{
					return;
				}

				var nativeArray = chunk.GetNativeArray(m_EntityType);
				var nativeArray2 = chunk.GetNativeArray(ref m_TaxPayerType);
				var bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceType);
				for (var i = 0; i < nativeArray.Length; i++)
				{
					var taxPayer = nativeArray2[i];
					var resources = bufferAccessor[i];
					if (taxPayer.m_UntaxedIncome != 0)
					{
						PayTax(ref taxPayer, nativeArray[i], resources, m_Type, m_StatisticsEventQueue);
						nativeArray2[i] = taxPayer;
					}
				}
			}
		}
	}
}
