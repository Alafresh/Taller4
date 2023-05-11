using System.Linq;
using System;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Health;
using UnityEngine;

namespace RTSEngine.Save.Game.Entities
{
    [Serializable]
    public struct DamageOverTimeSaveData
    {
        public DamageOverTimeData data;
        public int damage;
        public int sourceKey;

        public int remainingCycles;
        public float currCycleTime;
    }

    [Serializable]
    public abstract class FactionEntitySaveData : EntitySaveData
    {
        [SerializeField]
        private DamageOverTimeSaveData[] dot;

        public FactionEntitySaveData(IFactionEntity factionEntity)
            : base(factionEntity)
        {
            dot = factionEntity.Health.DOTHandlers
                .Select(dotHandler => new DamageOverTimeSaveData
                {
                    data = dotHandler.Data,
                    damage = dotHandler.Damage,
                    sourceKey = dotHandler.Source.GetKey(),

                    remainingCycles = dotHandler.RemainingCycles,
                    currCycleTime = dotHandler.CurrCycleTime
                })
                .ToArray();
        }

        private IFactionEntity factionEntity;
        public override void OnSpawned(IEntity entity)
        {
            base.OnSpawned(entity);
            this.factionEntity = entity as IFactionEntity;
        }

        public override void LoadComponents(IGameManager gameMgr)
        {
            base.LoadComponents(gameMgr);

            var inputMgr = gameMgr.GetService<IInputManager>();

            foreach (var dotSD in dot)
            {
                inputMgr.TryGetEntityInstanceWithKey(dotSD.sourceKey, out IEntity sourceEntity);

                factionEntity.Health.AddDamageOverTime(
                    new DamageOverTimeData
                    {
                        infinite = dotSD.data.infinite,
                        cycleDuration = dotSD.data.cycleDuration,
                        cycles = dotSD.remainingCycles,
                    },
                    dotSD.damage,
                    sourceEntity,
                    initialCycleDuration: dotSD.currCycleTime);
            }
        }
    }
}
