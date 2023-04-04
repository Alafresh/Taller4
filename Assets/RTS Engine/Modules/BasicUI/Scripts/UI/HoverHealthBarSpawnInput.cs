using RTSEngine.Entities;
using RTSEngine.Health;
using RTSEngine.Utilities;
using UnityEngine;

namespace RTSEngine.UI
{
    public class HoverHealthBarSpawnInput : PoolableObjectSpawnInput
    {
        public IEntity entity { get; }
        public HoverHealthBarData data { get; }

        public HoverHealthBarSpawnInput(IEntity entity, HoverHealthBarData data)
            : base(entity.transform, true, data.offset, Quaternion.identity)
        {
            this.entity = entity;
            this.data = data;
        }

        public HoverHealthBarSpawnInput(IEntity entity,
                                        Vector3 spawnPosition)
            : base(entity.transform, true, spawnPosition, Quaternion.identity)
        {
            this.entity = entity;
        }
    }
}
