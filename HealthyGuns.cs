using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Rewritten from scratch and maintained to present by VisEntities
 * Previous maintenance and contributions by Wulf and Arainrr
 * Originally created by Evano
 */

namespace Oxide.Plugins
{
    [Info("Healthy Guns", "VisEntities", "4.0.0")]
    [Description("Restores full condition to weapons spawned in loot crates and barrels.")]
    public class HealthyGuns : RustPlugin
    {
        #region Fields

        private static HealthyGuns _plugin;

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            CoroutineUtil.StopAllCoroutines();
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            CoroutineUtil.StartCoroutine(Guid.NewGuid().ToString(), RepairAllContainersCoroutine());
        }

        private void OnLootSpawn(LootContainer container)
        {
            if (container != null
                && container.OwnerID == 0
                && (container.SpawnType == LootContainer.spawnType.ROADSIDE || container.SpawnType == LootContainer.spawnType.TOWN))
            {
                NextTick(() =>
                {
                    RepairContainerContents(container);
                });
            }
        }
        
        #endregion Oxide Hooks

        #region Functions

        private IEnumerator RepairAllContainersCoroutine()
        {
            foreach (LootContainer container in BaseNetworkable.serverEntities.OfType<LootContainer>())
            {
                if (container != null
                    && container.OwnerID == 0
                    && (container.SpawnType == LootContainer.spawnType.ROADSIDE || container.SpawnType == LootContainer.spawnType.TOWN))
                {
                    RepairContainerContents(container);
                }

                yield return CoroutineEx.waitForSeconds(0.1f);
            }
        }

        private void RepairContainerContents(LootContainer container)
        {
            if (container.inventory.itemList.Count <= 0)
                return;

            foreach (Item item in container.inventory.itemList)
            {
                if (item.hasCondition && item.condition != item.info.condition.max && ItemOfCategory(item, ItemCategory.Weapon))
                    item.condition = item.info.condition.max;
            }
        }

        private bool ItemOfCategory(Item item, ItemCategory category)
        {
            return item.info.category == category;
        }

        #endregion Functions

        #region Coroutine Util

        private static class CoroutineUtil
        {
            private static readonly Dictionary<string, Coroutine> _activeCoroutines = new Dictionary<string, Coroutine>();
            
            public static void StartCoroutine(string coroutineName, IEnumerator coroutineFunction)
            {
                StopCoroutine(coroutineName);

                Coroutine coroutine = ServerMgr.Instance.StartCoroutine(coroutineFunction);
                _activeCoroutines[coroutineName] = coroutine;
            }

            public static void StopCoroutine(string coroutineName)
            {
                if (_activeCoroutines.TryGetValue(coroutineName, out Coroutine coroutine))
                {
                    if (coroutine != null)
                        ServerMgr.Instance.StopCoroutine(coroutine);

                    _activeCoroutines.Remove(coroutineName);
                }
            }

            public static void StopAllCoroutines()
            {
                foreach (string coroutineName in _activeCoroutines.Keys.ToArray())
                {
                    StopCoroutine(coroutineName);
                }
            }
        }

        #endregion Coroutine Util
    }
}