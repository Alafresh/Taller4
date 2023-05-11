using System.Linq;
using System.Collections.Generic;
using System;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Upgrades;
using RTSEngine.EntityComponent;
using UnityEngine;

namespace RTSEngine.Save.Game.Service
{
    [Serializable]
    public struct UpgradeElementSaveData
    {
        public string sourceCode;
        public string targetCode;
    }

    [Serializable]
    public struct FactionSlotEntityUpgradeSaveData
    {
        // Index: Upgrade Element 
        public UpgradeElementSaveData[] upgradeElements;
    }

    [Serializable]
    public struct EntityComponentUpgradeSaveData
    {
        public UpgradeElementSaveData[] upgradeElements;
    }
    [Serializable]
    public struct FactionSlotEntityComponentUpgradeSaveData
    {
        public string[] sourceEntityCodes;
        // Index: matches the above source entity code, to store all entity component upgrades for that entity type
        public EntityComponentUpgradeSaveData[] entityComponentUpgrades;
    }
    [Serializable]
    public struct SourceOnlyEntityComponentUpgradeSaveData
    {
        public int entityKey;
        public UpgradeElementSaveData[] upgradeElements;
    }


    [Serializable]
    public class EntityUpgradeManagerSaveData : PreRunGameServiceSaveDataBase<IEntityUpgradeManager> 
    {
        // Index: FactionID
        [SerializeField]
        private FactionSlotEntityUpgradeSaveData[] factionSlotUpgradeElements;

        public EntityUpgradeManagerSaveData(IEntityUpgradeManager component)
        {
            factionSlotUpgradeElements = component.Elements
                .Select(sourceFactionSlotUpgrades => new FactionSlotEntityUpgradeSaveData
                {
                    upgradeElements = sourceFactionSlotUpgrades.Select(sourceUpgradeElement => new UpgradeElementSaveData
                        {
                            sourceCode = sourceUpgradeElement.sourceCode,
                            targetCode = sourceUpgradeElement.target.Code
                        }).ToArray()
                })
                .ToArray();
        }

        public override void OnPreEntitySpawnLoad(IGameManager gameMgr)
        {
            IInputManager inputMgr = gameMgr.GetService<IInputManager>();

            var targetElements = factionSlotUpgradeElements
                .Select(factionSlotUpgrades => factionSlotUpgrades.upgradeElements
                    .Select(upgradeElement =>
                     {
                         inputMgr.TryGetEntityPrefabWithCode(upgradeElement.targetCode, out IEntity targetUpgradePrefab);
                         return new UpgradeElement<IEntity>
                         {
                             sourceCode = upgradeElement.sourceCode,
                             target = targetUpgradePrefab
                         };
                     }));

            component.ResetUpgrades(targetElements);
        }
    }

    [Serializable]
    public class EntityComponentUpgradeManagerSaveData : PreRunGameServiceSaveDataBase<IEntityComponentUpgradeManager> 
    {
        // Index: FactionID
        [SerializeField]
        private FactionSlotEntityComponentUpgradeSaveData[] factionSlotUpgradeElements;
        [SerializeField]
        private SourceOnlyEntityComponentUpgradeSaveData[] sourceOnlyUpgradeElements;

        public EntityComponentUpgradeManagerSaveData(IEntityComponentUpgradeManager component)
        {
            factionSlotUpgradeElements = component.Elements
                .Select(sourceFactionSlotUpgradesDic => new FactionSlotEntityComponentUpgradeSaveData
                {
                    sourceEntityCodes = sourceFactionSlotUpgradesDic.Keys.ToArray(),
                    entityComponentUpgrades = sourceFactionSlotUpgradesDic.Values.Select(
                            nextDicValues => new EntityComponentUpgradeSaveData
                            {
                                upgradeElements = nextDicValues.Select(sourceUpgrade => new UpgradeElementSaveData
                                {
                                    sourceCode = sourceUpgrade.sourceCode,
                                    targetCode = sourceUpgrade.target.Code
                                }).ToArray()
                            }).ToArray()
                })
                .ToArray();

            sourceOnlyUpgradeElements = component.SourceOnlyElements
                .Keys
                .Select(entity => new SourceOnlyEntityComponentUpgradeSaveData
                {
                    entityKey = entity.Key,
                    upgradeElements = component.SourceOnlyElements[entity]
                        .Select(sourceUpgradeElement => new UpgradeElementSaveData
                        {
                            sourceCode = sourceUpgradeElement.sourceCode,
                            targetCode = sourceUpgradeElement.target.Code
                        })
                        .ToArray()
                }).ToArray();
        }

        public override void OnPreEntitySpawnLoad(IGameManager gameMgr)
        {
            IInputManager inputMgr = gameMgr.GetService<IInputManager>();

            var nextElements = factionSlotUpgradeElements
                .Select(factionSlotUpgrades =>
                {
                    var nextDic = new Dictionary<string, IEnumerable<UpgradeElement<IEntityComponent>>>();

                    for (int i = 0; i < factionSlotUpgrades.sourceEntityCodes.Length; i++)
                    {
                        string entityCode = factionSlotUpgrades.sourceEntityCodes[i];
                        inputMgr.TryGetEntityPrefabWithCode(entityCode, out IEntity sourceEntity);

                        IEnumerable<UpgradeElement<IEntityComponent>> componentUpgrades = factionSlotUpgrades.entityComponentUpgrades[i].upgradeElements
                            .Select(upgradeElement => new UpgradeElement<IEntityComponent>
                            {
                                sourceCode = upgradeElement.sourceCode,
                                target = sourceEntity.gameObject.GetComponentInChildren<EntityComponentUpgrade>()
                                .AllUpgrades
                                .FirstOrDefault(upgradeElemSource => upgradeElemSource.UpgradeTarget.Code == upgradeElement.targetCode)
                                .UpgradeTarget

                            });

                        nextDic.Add(entityCode, componentUpgrades);
                    }

                    return nextDic;
                })
                .ToList();

            var nextSourceOnlyElements = sourceOnlyUpgradeElements
                .Select(element =>
                {
                    inputMgr.TryGetEntityInstanceWithKey(element.entityKey, out IEntity sourceEntity);
                    var kvp = new KeyValuePair<IEntity, IEnumerable<UpgradeElement<IEntityComponent>>>(
                        sourceEntity,
                        element.upgradeElements.Select(upgradeElement => new UpgradeElement<IEntityComponent>
                        {
                            sourceCode = upgradeElement.sourceCode,
                            target = sourceEntity.gameObject.GetComponentInChildren<EntityComponentUpgrade>()
                                .AllUpgrades
                                .FirstOrDefault(upgradeElemSource => upgradeElemSource.UpgradeTarget.Code == upgradeElement.targetCode)
                                .UpgradeTarget
                        }).AsEnumerable());
                    return kvp;
                })
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            component.ResetUpgrades(nextElements, nextSourceOnlyElements);
        }
    }
}
