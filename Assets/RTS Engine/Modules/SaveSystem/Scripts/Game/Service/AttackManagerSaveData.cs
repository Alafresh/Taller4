using System.Linq;
using System;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.EntityComponent;
using RTSEngine.Model;
using RTSEngine.Attack;

namespace RTSEngine.Save.Game.Service
{
    [Serializable]
    public struct AttackObjectSaveData
    {
        public string code;

        public int sourceEntityKey;
        public string sourceAttackCompCode;
        public int sourceFactionID;
        public int launcherSourceIndex;

        public int targetEntityKey;
        public Vector3 targetPosition;

        public float currDelayTime;
        public bool damageInDelay;

        public bool damageFriendly;

        public float currLifeTime;

        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    public class AttackManagerSaveData : PreRunGameServiceSaveDataBase<IAttackManager> 
    {
        [SerializeField]
        private AttackObjectSaveData[] activeAttackObjects;

        public AttackManagerSaveData(IAttackManager component)
        {
            activeAttackObjects = component.ActiveAttackObjects.Values
                .SelectMany(attackObjects => attackObjects)
                .Select(nextObj => new AttackObjectSaveData
                {
                    code = nextObj.Code,

                    sourceEntityKey = nextObj.Data.source.IsValid() ? nextObj.Data.source.Entity.GetKey() : InputManager.INVALID_ENTITY_KEY,
                    sourceAttackCompCode = nextObj.Data.source?.Code,
                    sourceFactionID = nextObj.Data.sourceFactionID,
                    launcherSourceIndex = nextObj.Data.launcherSourceIndex,

                    targetEntityKey = nextObj.Data.target.IsValid() ? nextObj.Data.target.GetKey() : InputManager.INVALID_ENTITY_KEY,
                    targetPosition = nextObj.Data.targetPosition,

                    currDelayTime = nextObj.DelayTime,
                    damageInDelay = nextObj.Data.damageInDelay,

                    damageFriendly = nextObj.Data.damageFriendly,

                    currLifeTime = nextObj.CurrLifeTime,

                    position = nextObj.transform.position,
                    rotation = nextObj.transform.rotation
                })
                .ToArray();
        }

        public override void OnPostEntitySpawnLoad(IGameManager gameMgr)
        {
            var inputMgr = gameMgr.GetService<IInputManager>();

            foreach (AttackObjectSaveData attackObjSD in activeAttackObjects)
            {
                component.TryGetAttackObjectPrefab(attackObjSD.code, out IAttackObject prefab);
                inputMgr.TryGetEntityInstanceWithKey(attackObjSD.sourceEntityKey, out IEntity sourceEntity);
                inputMgr.TryGetEntityInstanceWithKey(attackObjSD.targetEntityKey, out IEntity targetEntity);

                IAttackComponent attackComp = sourceEntity.IsValid()
                    ? sourceEntity.AttackComponents.Values.Where(comp => comp.Code == attackObjSD.sourceAttackCompCode).FirstOrDefault()
                    : null;

                ModelCacheAwareTransformInput delayParent = null;
                if (attackComp.IsValid())
                    delayParent = attackComp.Launcher.Sources[attackObjSD.launcherSourceIndex].delayParentObject;

                component.SpawnAttackObject(prefab,
                    new AttackObjectSpawnInput
                    (
                        sourceAttackComp: attackComp,

                        sourceFactionID: attackObjSD.sourceFactionID,
                        launcherSourceIndex: attackObjSD.launcherSourceIndex,

                        spawnPosition: attackObjSD.position,
                        spawnRotation: attackObjSD.rotation,

                        target: targetEntity as IFactionEntity,
                        targetPosition: attackObjSD.targetPosition,

                        delayTime: attackObjSD.currDelayTime,
                        damageInDelay: attackObjSD.damageInDelay,
                        delayParent: delayParent,

                        damageFriendly: attackObjSD.damageFriendly,

                        enableLifeTime: true,
                        useDefaultLifeTime: false,
                        customLifeTime: attackObjSD.currLifeTime
                    ));
            }
        }
    }
}
