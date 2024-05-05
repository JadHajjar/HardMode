using Colossal.Entities;

using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;

using HardMode.Domain;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using UnityEngine;

namespace HardMode.Systems
{
	public partial class BulldozeCostSystem : GameSystemBase
	{
		private CitySystem m_CitySystem;
		private ToolSystem m_ToolSystem;
		private BulldozeToolSystem m_BulldozeTool;
		private EntityQuery m_DefinitionQuery;

		public int TotalCost { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
			m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
			m_BulldozeTool = World.GetOrCreateSystemManaged<BulldozeToolSystem>();
			m_DefinitionQuery = SystemAPI.QueryBuilder().WithAll<CreationDefinition>().WithNone<Updated>().Build();

			RequireForUpdate(m_DefinitionQuery);
		}

		protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
		{
			if (!mode.IsGame())
			{
				return;
			}

			if (!EntityManager.HasBuffer<BufferedMoneyResource>(m_CitySystem.City))
			{
				EntityManager.AddBuffer<BufferedMoneyResource>(m_CitySystem.City);
			}
		}

		protected override void OnUpdate()
		{
			if (m_ToolSystem.activeTool != m_BulldozeTool)
			{
				return;
			}

			var totalCost = 0;
			var temps = m_DefinitionQuery.ToComponentDataArray<CreationDefinition>(Allocator.Temp);

			for (var i = 0; i < temps.Length; i++)
			{
				var creationDefinition = temps[i];
				var entity = creationDefinition.m_Original;

				if (!creationDefinition.m_Flags.HasFlag(CreationFlags.Delete))
				{
					continue;
				}

				var cost = GetDemolitionCost(entity);

				totalCost += cost;
			}

			TotalCost = totalCost;
		}

		public int GetDemolitionCost(Entity entity)
		{
			if (Mod.Settings?.BulldozeCostsMoney == false)
			{
				return 0;
			}

			var cost = math.min(2_500_000, GetDemolitionCostImpl(entity));

			if (cost == 0f)
			{
				return 0;
			}

			if (!EntityManager.HasComponent<ResidentialProperty>(entity) && !EntityManager.HasComponent<CommercialProperty>(entity) && !EntityManager.HasComponent<IndustrialProperty>(entity) && !EntityManager.HasComponent<OfficeProperty>(entity))
			{
				cost /= 2f;
			}

			switch (Mod.Settings?.EconomyDifficulty)
			{
				case Domain.EconomyDifficulty.Easy:
					cost /= 3f;
					break;
				case Domain.EconomyDifficulty.Medium:
					cost /= 2f;
					break;
				case Domain.EconomyDifficulty.Hard:
					cost /= 1.5f;
					break;
			}

			return Mathf.RoundToInt(cost / 100f) * 100;
		}

		private float GetDemolitionCostImpl(Entity entity)
		{
			if (EntityManager.HasComponent<UnderConstruction>(entity) || !EntityManager.TryGetComponent<Building>(entity, out var building))
			{
				return 0f;
			}

			var cost = 0f;

			if (EntityManager.HasComponent<Destroyed>(entity))
			{
				if (EntityManager.TryGetComponent<ObjectGeometryData>(EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab, out var objectGeometryData_))
				{
					return objectGeometryData_.m_Size.x * objectGeometryData_.m_Size.z * 4;
				}
			}

			if (EntityManager.TryGetBuffer<InstalledUpgrade>(entity, true, out var installedUpgrades))
			{
				for (var i = 0; i < installedUpgrades.Length; i++)
				{
					cost += GetDemolitionCostImpl(installedUpgrades[i].m_Upgrade);
				}
			}

			if (EntityManager.TryGetComponent<LandValue>(building.m_RoadEdge, out var landValue))
			{
				if (EntityManager.HasComponent<ResidentialProperty>(entity) && EntityManager.TryGetBuffer<Renter>(entity, true, out var buffer))
				{
					var evictionCost = 1500f;
					var count = 0f;

					for (var i = 0; i < buffer.Length; i++)
					{
						if (buffer[i].m_Renter != Entity.Null)
						{
							var household = EntityManager.GetComponentData<Household>(buffer[i].m_Renter);

							if (!household.m_Flags.HasFlag(HouseholdFlags.MovedIn))
							{
								continue;
							}

							var rent = EntityManager.GetComponentData<PropertyRenter>(buffer[i].m_Renter);

							evictionCost = math.clamp(rent.m_Rent, 0, evictionCost);
							count++;
						}
					}

					cost += evictionCost * count;
				}

				cost += landValue.m_LandValue * (4f + ((4f - math.log10(6f * landValue.m_Weight)) * landValue.m_Weight / 8f));
			}

			if (EntityManager.TryGetComponent<ObjectGeometryData>(EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab, out var objectGeometryData))
			{
				cost += (objectGeometryData.m_Size.x * objectGeometryData.m_Size.z * 3) +
							(objectGeometryData.m_Size.x * objectGeometryData.m_Size.y * objectGeometryData.m_Size.z / 6);
			}

			return cost;
		}
	}
}
