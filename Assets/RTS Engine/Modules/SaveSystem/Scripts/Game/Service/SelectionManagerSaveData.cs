using System.Linq;
using System;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Selection;

namespace RTSEngine.Save.Game.Service
{
    [Serializable]
    public class SelectionManagerSaveData : PreRunGameServiceSaveDataBase<ISelectionManager>
    {
        [SerializeField]
        private int[] selectedEntitiesKeys;

        public SelectionManagerSaveData(ISelectionManager component)
        {
            selectedEntitiesKeys = component.GetEntitiesList(EntityType.all, exclusiveType: false, localPlayerFaction: false)
                .Select(entity => entity.Key)
                .ToArray();
        }

        public override void OnPostEntitySpawnLoad(IGameManager gameMgr)
        {
            IInputManager inputMgr = gameMgr.GetService<IInputManager>();

            component.Add(selectedEntitiesKeys
                .Select(entityKey =>
                {
                    inputMgr.TryGetEntityInstanceWithKey(entityKey, out IEntity entity);
                    return entity;
                }));
        }
    }
}
