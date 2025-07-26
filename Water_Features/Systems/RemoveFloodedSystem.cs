// <copyright file="RemoveFloodedSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Water_Features.Systems
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Notifications;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Water_Features.Components;

    /// <summary>
    /// A system that will remove the flooded component.
    /// </summary>
    public partial class RemoveFloodedSystem : GameSystemBase
    {
        private EntityQuery m_FloodedQuery;
        private EntityQuery m_IconQuery;
        private ILog m_Log;
        private EndFrameBarrier m_Barrier;
        private PrefabID m_WaterDamageID = new PrefabID(nameof(NotificationIconPrefab), "Water Damage");
        private PrefabSystem m_PrefabSystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = WaterFeaturesMod.Instance.Log;
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_FloodedQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Events.Flooded>()
                .WithNone<Game.Common.Deleted, Overridden, Temp, Destroyed>()
                .Build();

            m_IconQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Common.Owner, Icon, PrefabRef>()
                .WithNone<Game.Common.Deleted>()
                .Build();

            m_Log.Info($"[{nameof(RemoveFloodedSystem)}] {nameof(OnCreate)}");
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (!m_PrefabSystem.TryGetPrefab(m_WaterDamageID, out PrefabBase waterDamagePrefabBase) ||
                    waterDamagePrefabBase is null ||
                    waterDamagePrefabBase is not NotificationIconPrefab ||
                    !m_PrefabSystem.TryGetEntity(waterDamagePrefabBase, out Entity waterDamagePrefabEntity))
            {
                return;
            }

            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            if (!m_FloodedQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_FloodedQuery.ToEntityArray(Allocator.Temp);
                m_Log.Debug($"{nameof(RemoveFloodedSystem)}:{nameof(OnUpdate)} Flooded entities.length = {entities.Length}.");
                buffer.RemoveComponent<Game.Events.Flooded>(entities);
                m_Log.Info($"{nameof(RemoveFloodedSystem)}:{nameof(OnUpdate)} Removed Flooded components.");
                buffer.RemoveComponent<Damaged>(entities);
                m_Log.Info($"{nameof(RemoveFloodedSystem)}:{nameof(OnUpdate)} Removed Damaged components.");

                m_Log.Debug($"{nameof(RemoveFloodedSystem)}:{nameof(OnUpdate)} Flooded entities.length = {entities.Length}.");
                for (int i = 0; i < entities.Length;   i++)
                {
                    if (EntityManager.TryGetBuffer(entities[i], isReadOnly: true, out DynamicBuffer<IconElement> originalIconElements) &&
                        originalIconElements.Length > 0)
                    {
                        if (originalIconElements[0].m_Icon == waterDamagePrefabEntity)
                        {
                            m_Log.Debug($"{nameof(RemoveFloodedSystem)}:{nameof(OnUpdate)} Removed Icon Element buffer = {entities[i].Index}:{entities[i].Version}.");
                            buffer.RemoveComponent<IconElement>(entities[i]);
                            continue;
                        }

                        DynamicBuffer<IconElement> newElements = buffer.SetBuffer<IconElement>(entities[i]);
                        for (int j = 0; j < originalIconElements.Length; j++)
                        {
                            if (originalIconElements[j].m_Icon == waterDamagePrefabEntity)
                            {
                                continue;
                            }

                            newElements.Add(originalIconElements[j]);
                        }
                    }
                }

                m_Log.Info($"{nameof(RemoveFloodedSystem)}:{nameof(OnUpdate)} Removed IconElement components.");
            }

            if (!m_IconQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_IconQuery.ToEntityArray(Allocator.Temp);

                m_Log.Info($"{nameof(RemoveFloodedSystem)}:{nameof(OnUpdate)} m_IconQuery entities.length = {entities.Length}.");
                for (int i = 0; i < entities.Length; i++)
                {
                    if (EntityManager.TryGetComponent(entities[i], out PrefabRef prefabRef) &&
                        prefabRef.m_Prefab == waterDamagePrefabEntity)
                    {
                        buffer.AddComponent<Deleted>(entities[i]);

                        m_Log.Debug($"{nameof(RemoveFloodedSystem)}:{nameof(OnUpdate)} Removed Icon Entity = {entities[i].Index}:{entities[i].Version}.");
                    }
                }

                m_Log.Info($"{nameof(RemoveFloodedSystem)}:{nameof(OnUpdate)} Removed water damage Icon entities.");
            }

            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (WaterFeaturesMod.Instance.Settings.WaterCausesDamage == false)
            {
                Enabled = true;
            }
        }
    }
}
