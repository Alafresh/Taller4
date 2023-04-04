
using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Utilities;

namespace RTSEngine.Minimap.Icons
{
    public class MinimapIconSpawnInput : PoolableObjectSpawnInput
    {
        public IEntity SourceEntity { get; }
        public float Height { get; }
        public IEntityMinimapIconData Data { get; }

        public MinimapIconSpawnInput(IEntity sourceEntity, float height, IEntityMinimapIconData data, Quaternion spawnRotation)
            : base(parent: null, useLocalTransform: false, spawnPosition: Vector3.zero, spawnRotation)
        {
            this.Data = data;
            this.SourceEntity = sourceEntity;
            this.Height = height;
        }
    }
}