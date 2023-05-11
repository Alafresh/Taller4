using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;
using RTSEngine.Game;
using RTSEngine.Task;

namespace RTSEngine.Save.Game.Service
{
    [Serializable]
    public struct EntityToTaskInputComponentsSaveData
    {
        public int entityKey;
        public ComponentToTaskInputsSaveData[] taskInputComponents;
    }

    [Serializable]
    public struct ComponentToTaskInputsSaveData
    {
        public string componentCode;
        public TaskInputSaveData[] taskInputs;
    }

    [Serializable]
    public struct TaskInputSaveData
    {
        public int taskID;
        public EntityComponentTaskInputData data;
    }

    [Serializable]
    public class TaskManagerSaveData : PreRunGameServiceSaveDataBase<ITaskManager> 
    {
        [SerializeField]
        private EntityToTaskInputComponentsSaveData[] entityComponentTasksData;

        public TaskManagerSaveData(ITaskManager component)
        {
            var nextData = component.EntityComponentTaskInputTrackerToData();
            entityComponentTasksData = nextData
                .GroupBy(kvp0 => kvp0.Key.Entity.Key)
                .Select(kvp1 => new EntityToTaskInputComponentsSaveData
                {
                    entityKey = kvp1.Key,
                    taskInputComponents = kvp1.Select(kvp2 => new ComponentToTaskInputsSaveData
                    {
                        componentCode = kvp2.Key.Code,
                        taskInputs = kvp2.Value.Select(kvp3 => new TaskInputSaveData
                        {
                            taskID = kvp3.Key,
                            data = kvp3.Value
                        })
                        .ToArray()
                    })
                    .ToArray()
                })
                .ToArray();
        }

        public override void OnPreEntitySpawnLoad(IGameManager gameMgr)
        {
            var newInitialData = new Dictionary<int, Dictionary<string, Dictionary<int, EntityComponentTaskInputData>>>();
            component.ResetEntityComponentTaskInputInitialData(entityComponentTasksData
                .ToDictionary(element => element.entityKey, element => element.taskInputComponents
                    .ToDictionary(element2 => element2.componentCode, element2 => element2.taskInputs
                        .ToDictionary(element3 => element3.taskID, element3 => element3.data))));
        }

        public override void OnPostEntitySpawnLoad(IGameManager gameMgr)
        {
            // To remove the initial task data from memory
            component.ResetEntityComponentTaskInputInitialData(null);
        }
    }
}
