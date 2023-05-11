using System.Linq;
using System;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.EntityComponent;
using RTSEngine.Task;
using RTSEngine.Save.Game.EntityComponent;
using RTSEngine.Attack;
using System.Collections.Generic;

namespace RTSEngine.Save.Game.Entities
{
    [Serializable]
    public struct EntityComponentSaveData
    {
        public string code;
        public EntityComponentData data;
    }
    [Serializable]
    public struct ProgressEntityComponentSaveData
    {
        public string code;
        public EntityTargetComponentProgressData data;
        public int targetKey;
    }
    [Serializable]
    public struct TargetEntityComponentSaveData
    {
        public string code;
        public EntityTargetComponentData data;
        public bool isMoveAttackRequest;
    }

    [Serializable]
    public struct AttackComponentSaveData
    {
        public string code;

        public bool isLocked;

        public AttackObjectLaunchLogInput[] launchLogs;
        public int launchLogsTargetKey;

        public Quaternion weaponRotation;
    }

    [Serializable]
    public struct PendingTaskHandlerSaveData
    {
        public float queueTime;
        public PendingTaskSaveData[] pendingTasks;
    }

    [Serializable]
    public struct PendingTaskSaveData
    {
        public string compCode;
        public int taskID;
        public bool playerCommand;
    }

    [Serializable]
    public struct TasksQueueSaveData
    {
        public string componentCode;

        public EntityTargetComponentData target;
        public bool playerCommand;

        public bool includeMovement;
        public bool isMoveAttackRequest;

        public bool fromQueue;
    }

    [Serializable]
    public struct CustomEntityComponentSaveData
    {
        public string compCode;
        public string value;
    }

    [Serializable]
    public abstract class EntitySaveData
    {
        public abstract int Key { get; }

        [SerializeField]
        private string code;
        public string Code => code;

        [SerializeField]
        private Vector3 position;
        public Vector3 Position => position;
        [SerializeField]
        private Quaternion rotation;
        public Quaternion Rotation => rotation;
        [SerializeField]
        private Vector3 localScale;
        public Vector3 LocalScale => localScale;

        [SerializeField]
        private EntityComponentSaveData[] entityComponents;
        [SerializeField]
        private ProgressEntityComponentSaveData[] progressEntityComponents;
        [SerializeField]
        private TargetEntityComponentSaveData[] targetEntityComponents; 
        [SerializeField]
        private AttackComponentSaveData[] attackComponents;
        [SerializeField]
        private PendingTaskHandlerSaveData pendingTaskHandler;

        [SerializeField]
        private CustomEntityComponentSaveData[] customEntityComponentsData;

        [SerializeField]
        private string tasksQueueRunningCompCode;
        [SerializeField]
        private TasksQueueSaveData[] tasksQueueData;

        public EntitySaveData(IEntity entity)
        {
            code = entity.Code;

            position = entity.transform.position;
            rotation = entity.transform.rotation;
            localScale = entity.transform.localScale;

            entityComponents = entity.EntityComponents
                .Select(comp => new EntityComponentSaveData
                {
                    code = comp.Key,
                    data = comp.Value.Data
                })
                .ToArray();

            targetEntityComponents = entity.EntityTargetComponents
                .Where(comp => comp.Value.HasTarget)
                .Select(comp => new TargetEntityComponentSaveData
                {
                    code = comp.Key,
                    data = comp.Value.TargetData,
                    isMoveAttackRequest = entity.FirstActiveAttackComponent.IsValid() && entity.FirstActiveAttackComponent.IsAttackMoveActive
                })
                .ToArray();

            attackComponents = entity.AttackComponents
                .Select(elem => new AttackComponentSaveData
                {
                    code = elem.Key,

                    isLocked = elem.Value.IsLocked,

                    launchLogs = elem.Value.Launcher.LaunchLog
                    .Select(log => new AttackObjectLaunchLogInput
                    {
                        sourceIndex = log.sourceIndex,

                        preDelayTimer = log.preDelayTimer.CurrValue,
                        postDelayTimer = log.postDelayTimer.CurrValue,

                        isLastLaunch = log.isLastLaunch
                    })
                    .ToArray(),

                    launchLogsTargetKey = elem.Value.TargetData.targetKey,

                    weaponRotation = elem.Value.WeaponTransform.IsValid()
                        ? elem.Value.WeaponTransform.Rotation
                        : Quaternion.identity

                })
                .ToArray();

            progressEntityComponents = entity.EntityTargetProgressComponents
                .Where(comp => comp.Value.HasTarget && comp.Value.InProgress)
                .Select(comp => new ProgressEntityComponentSaveData
                {
                    code = comp.Key,
                    data = comp.Value.ProgressData,
                    targetKey = comp.Value.TargetData.targetKey
                })
                .ToArray();

            if (entity.PendingTasksHandler.IsValid())
            {
                pendingTaskHandler = new PendingTaskHandlerSaveData
                {
                    queueTime = entity.PendingTasksHandler.QueueTimerValue,
                    pendingTasks = entity.PendingTasksHandler.Queue
                    .Select(pendingTask => new PendingTaskSaveData
                    {
                        compCode = pendingTask.sourceComponent.Code,
                        taskID = pendingTask.sourceTaskInput.ID,
                        playerCommand = pendingTask.playerCommand
                    })
                    .ToArray()
                };
            }

            if(entity.TasksQueue.IsValid())
            {
                if (entity.TasksQueue.IsRunningQueueTask)
                    tasksQueueRunningCompCode = entity.TasksQueue.RunningQueueTaskCompCode;

                tasksQueueData = entity.TasksQueue.Queue
                    .Select(task => new TasksQueueSaveData
                    {
                        componentCode = task.componentCode,

                        fromQueue = task.fromTasksQueue,

                        includeMovement = task.includeMovement,
                        isMoveAttackRequest = task.isMoveAttackRequest,

                        playerCommand = task.playerCommand,

                        target = new EntityTargetComponentData
                        {
                            targetKey = task.target.instance.GetKey(),
                            position = task.target.position,
                            opPosition = task.target.opPosition
                        }
                    })
                    .ToArray();
            }

            customEntityComponentsData = entity.gameObject.GetComponentsInChildren<ISavableEntityComponent>()
                .Select(comp => new CustomEntityComponentSaveData
                {
                    compCode = comp.Code,
                    value = comp.Save()
                })
                .ToArray();
        }

        private IEntity entity;
        public abstract void Spawn(IGameManager gameMgr);
        public virtual void OnSpawned(IEntity entity)
        {
            this.entity = entity;
        }

        public virtual void LoadComponents(IGameManager gameMgr)
        {
            var inputMgr = gameMgr.GetService<IInputManager>();

            foreach (EntityComponentSaveData comp in entityComponents)
            {
                if (comp.data.isActive != entity.EntityComponents[comp.code].IsActive)
                    entity.EntityComponents[comp.code].SetActiveLocal(comp.data.isActive, playerCommand: false);
            }

            foreach (ProgressEntityComponentSaveData comp in progressEntityComponents)
            {
                inputMgr.TryGetEntityInstanceWithKey(comp.targetKey, out IEntity nextTarget);
                entity.EntityTargetProgressComponents[comp.code].LaunchActionLocal(
                    (byte)ProgressActionType.setNextProgressData,
                    new SetTargetInputData {
                        target = new TargetData<IEntity>
                        {
                            instance = nextTarget,
                            position = new Vector3(comp.data.progressTime, 0.0f, 0.0f)
                        },
                        playerCommand = false
                    });
            }

            foreach(AttackComponentSaveData attackComponentSD in attackComponents)
            {
                IAttackComponent nextAttackComp = entity.AttackComponents[attackComponentSD.code];

                if (nextAttackComp.IsLocked != attackComponentSD.isLocked)
                    nextAttackComp.LockAttackAction(attackComponentSD.isLocked, playerCommand: false);

                if (nextAttackComp.WeaponTransform.IsValid())
                    nextAttackComp.WeaponTransform.Rotation = attackComponentSD.weaponRotation;

                inputMgr.TryGetEntityInstanceWithKey(attackComponentSD.launchLogsTargetKey, out IEntity nextTarget);
                nextAttackComp.SetNextLaunchLogActionLocal(
                    attackComponentSD.launchLogs,
                    nextTarget as IFactionEntity,
                    playerCommand: false);
            }

            foreach (TargetEntityComponentSaveData comp in targetEntityComponents)
            {
                inputMgr.TryGetEntityInstanceWithKey(comp.data.targetKey, out IEntity nextTarget);
                entity.EntityTargetComponents[comp.code].SetTargetLocal(
                    new SetTargetInputData
                    {
                        target = new TargetData<IEntity>
                        {
                            instance = nextTarget,
                            position = comp.data.position,
                            opPosition = comp.data.opPosition
                        },
                        isMoveAttackRequest = comp.isMoveAttackRequest,
                        playerCommand = false,
                    });
            }

            if (entity.PendingTasksHandler.IsValid())
            {
                foreach (var nextTask in pendingTaskHandler.pendingTasks)
                {
                    var sourceComp = entity.EntityComponents[nextTask.compCode] as IPendingTaskEntityComponent;
                    entity.PendingTasksHandler.Add(new PendingTask
                    {
                        sourceComponent = sourceComp,
                        sourceTaskInput = sourceComp.Tasks.Where(task => task.ID == nextTask.taskID).First(),
                        playerCommand = nextTask.playerCommand
                    },
                    useCustomQueueTime: true,
                    customQueueTime: pendingTaskHandler.queueTime);
                }
            }

            if(entity.TasksQueue.IsValid())
            {
                if(entity.EntityTargetComponents.ContainsKey(tasksQueueRunningCompCode))
                    entity.TasksQueue.LaunchActionLocal(
                        (byte)TasksQueueActionType.setRunningComponent,
                        new SetTargetInputData
                        {
                            componentCode = tasksQueueRunningCompCode
                        });

                foreach(var task in tasksQueueData)
                {
                    inputMgr.TryGetEntityInstanceWithKey(task.target.targetKey, out IEntity nextTarget);

                    entity.TasksQueue.LaunchActionLocal(
                        (byte)TasksQueueActionType.addNoLaunchOnEmpty,
                        new SetTargetInputData
                        {
                            componentCode = task.componentCode,

                            fromTasksQueue = task.fromQueue,

                            includeMovement = task.includeMovement,
                            isMoveAttackRequest = task.isMoveAttackRequest,

                            playerCommand = task.playerCommand,

                            target = new TargetData<IEntity> 
                            {
                                instance = nextTarget,
                                position = task.target.position,
                                opPosition = task.target.opPosition
                            }
                        });
                }
            }

            foreach (var data in customEntityComponentsData)
            {
                (entity.EntityComponents[data.compCode] as ISavableEntityComponent).Load(data.value);
            }
        }
    }
}
