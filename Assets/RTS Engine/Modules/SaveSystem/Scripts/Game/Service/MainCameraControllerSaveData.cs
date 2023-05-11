using System;

using UnityEngine;
using RTSEngine.Game;
using RTSEngine.Cameras;

namespace RTSEngine.Save.Game.Service
{
    [Serializable]
    public class MainCameraControllerSaveData : PreRunGameServiceSaveDataBase<IMainCameraController> 
    {
        [SerializeField]
        private Vector3 position;
        [SerializeField]
        private Quaternion rotation;
        [SerializeField]
        private float fieldOfView;

        public MainCameraControllerSaveData(IMainCameraController component)
        {
            position = component.MainCamera.transform.position;
            rotation = component.MainCamera.transform.rotation;
            fieldOfView = component.MainCamera.fieldOfView;
        }

        public override void OnPostEntitySpawnLoad(IGameManager gameMgr)
        {
            component.MainCamera.transform.position = position;
            component.MainCamera.transform.rotation = rotation;
            component.MainCamera.fieldOfView = fieldOfView;
        }
    }
}
