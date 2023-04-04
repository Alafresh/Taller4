using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.Terrain;
using RTSEngine.Audio;
using RTSEngine.Selection;
using RTSEngine.Cameras;
using RTSEngine.Logging;
using RTSEngine.Controls;
using RTSEngine.Utilities;

namespace RTSEngine.BuildingExtension
{

    public class BuildingPlacement : MonoBehaviour, IBuildingPlacement 
    {
        #region Attributes
        [HideInInspector]
        public Int2D tabID = new Int2D { x = 0, y = 0 };

        // Each faction slot that can place buildings has a valid entry in this array with its faction slot id as the index
        private Dictionary<int, IBuildingPlacementHandler> handlers;

        public bool IsLocalPlayerPlacingBuilding => IsPlacingBuilding(gameMgr.LocalFactionSlotID);
        public bool IsPlacingBuilding(int factionID)
            => handlers.TryGetValue(factionID, out IBuildingPlacementHandler handler) && handler.IsActive;

        [SerializeField, Tooltip("This value is added to the building's position on the Y axis.")]
        private float buildingPositionYOffset = 0.01f; 
        public float BuildingPositionYOffset => buildingPositionYOffset;

        [SerializeField, Tooltip("The maximum distance that a building and the closest terrain area that it can be placed on can have.")]
        private float terrainMaxDistance = 1.5f; 
        public float TerrainMaxDistance => terrainMaxDistance;

        [SerializeField, Tooltip("Input the terrain areas where buildings can be placed.")]
        private TerrainAreaType[] placableTerrainAreas = new TerrainAreaType[0];
        public IReadOnlyList<TerrainAreaType> PlacableTerrainAreas => placableTerrainAreas;
        // This would include the layers defined in the placableTerrainAreas
        public LayerMask PlacableLayerMask { private set; get; }

        [SerializeField, Tooltip("Building placement instances will ignore collision with objects of layers assigned to the terrain areas in this array field.")]
        private TerrainAreaType[] ignoreTerrainAreas = new TerrainAreaType[0];
        public IReadOnlyList<TerrainAreaType> IgnoreTerrainAreas => ignoreTerrainAreas;

        [SerializeField, Tooltip("Fields related to the local player faction building placement.")]
        private LocalFactionPlacementHandler localFactionPlacementHandler = new LocalFactionPlacementHandler();

        // Grid Placement
        public IGridPlacementHandler GridHandler { private set; get; }

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; } 
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        protected IGameControlsManager controls { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>(); 
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();
            this.controls = gameMgr.GetService<IGameControlsManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();

            GridHandler = gameObject.GetComponent<IGridPlacementHandler>();
            if (GridHandler.IsValid())
                GridHandler.Init(gameMgr);

            PlacableLayerMask = new LayerMask();

            if (placableTerrainAreas.Length == 0)
              logger.LogWarning($"[{GetType().Name}] No building placement terrain areas have been defined in the 'Placable Terrain Areas'. You will not be able to place buildings!");

            if (!placableTerrainAreas.IsValid())
            {
                logger.LogError($"[{GetType().Name}] 'Placable Terrain Areas' field has some invalid elements!");
                return;
            }

            foreach(TerrainAreaType area in placableTerrainAreas)
                PlacableLayerMask |= area.Layers;

            handlers = new Dictionary<int, IBuildingPlacementHandler>();

            gameMgr.GameStartRunning += HandleGameStartRunning;

        }

        private void HandleGameStartRunning(IGameManager sender, EventArgs args)
        {
            if (gameMgr.LocalFactionSlot.IsValid())
            {
                // In case a component that implements the required interface is attached to the same game object
                // Use that component instead of the localFactionPlacementHandler field.
                IBuildingPlacementHandler nextHandler = GetComponent<IBuildingPlacementHandler>();
                if (!nextHandler.IsValid())
                    nextHandler = localFactionPlacementHandler;

                RegisterFactionPlacementHandler(gameMgr.LocalFactionSlotID, nextHandler);
            }

            gameMgr.GameStartRunning -= HandleGameStartRunning;
        }

        public bool RegisterFactionPlacementHandler(int factionID, IBuildingPlacementHandler newHandler)
        {
            if (handlers.ContainsKey(factionID)
                || !factionID.IsValidFaction())
            {
                logger.LogError($"[{GetType().Name}] Attempting to register new placement handler for faction ID '{factionID}' while it already has one assigned or is an invalid faction ID!");
                return false;
            }

            newHandler.Init(gameMgr, gameMgr.GetFactionSlot(factionID));
            handlers.Add(factionID, newHandler);
            return true;
        }

        private void OnDestroy()
        {
        }
        #endregion

        private void Update()
        {
            if (gameMgr.State != GameStateType.running)
                return;

            foreach (var handler in handlers.Values)
                handler.OnUpdate();
        }

        public virtual ErrorMessage CanPlace(IBuildingPlacer buildingPlacer)
        {
            int factionID = buildingPlacer.Building.FactionID;
            if(!handlers.ContainsKey(factionID))
            {
                logger.LogError($"[{GetType().Name}] '{factionID}' is an invalid faction ID or one that does not have a valid placement handler!");
                return ErrorMessage.none;
            }

            return handlers[factionID].CanPlace(buildingPlacer);
        }

        public bool Add(int factionID, IBuildingPlacementTask task, BuildingPlacementOptions options = default)
        {
            if(!handlers.ContainsKey(factionID))
            {
                logger.LogError($"[{GetType().Name}] '{factionID}' is an invalid faction ID or one that does not have a valid placement handler!");
                return false;
            }

            return handlers[factionID].Add(task, options) == ErrorMessage.none;
        }

        public bool Stop(int factionID)
        {
            if(!handlers.ContainsKey(factionID))
            {
                logger.LogError($"[{GetType().Name}] '{factionID}' is an invalid faction ID or one that does not have a valid placement handler!");
                return false;
            }

            return handlers[factionID].Stop();
        }

    }
}
