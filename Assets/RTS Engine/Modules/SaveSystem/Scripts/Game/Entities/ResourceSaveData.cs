using System;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using UnityEngine;

namespace RTSEngine.Save.Game.Entities
{
    [Serializable]
    public class ResourceSaveData : EntitySaveData
    {
        [SerializeField]
        private InitResourceParametersInput initParamsInput;
        public override int Key => initParamsInput.key;

        public ResourceSaveData(IResource resource)
            : base(resource)
        {
            initParamsInput = new InitResourceParametersInput
            {
                enforceKey = true,
                key = resource.Key,

                factionID = resource.FactionID,
                free = resource.IsFree,

                setInitialHealth = true,
                initialHealth = resource.Health.CurrHealth,

                playerCommand = false
            };
        }

        public override void Spawn(IGameManager gameMgr)
        {
            var resourceMgr = gameMgr.GetService<IResourceManager>();
            var inputMgr = gameMgr.GetService<IInputManager>();

            inputMgr.TryGetEntityPrefabWithCode(Code, out IEntity entity);
            IResource createdResource = resourceMgr.CreateResourceLocal(
                    entity as IResource,
                    Position,
                    Rotation,
                    initParamsInput.ToParams(inputMgr)
                );

            createdResource.transform.localScale = LocalScale;

            OnSpawned(createdResource);
        }
    }
}
