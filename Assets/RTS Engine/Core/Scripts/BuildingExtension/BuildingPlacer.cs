using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Terrain;
using RTSEngine.Search;
using RTSEngine.Effect;
using RTSEngine.Model;

namespace RTSEngine.BuildingExtension
{
    public class BuildingPlacer : MonoBehaviour, IBuildingPlacer, IEntityPreInitializable
    {
        #region Attributes
        public IBuilding Building { private set; get; }

        public bool CanPlace { private set; get; }
        public ErrorMessage CanPlaceError { private set; get; }

        [SerializeField, Tooltip("If populated then this defines the types of terrain areas where the building can be placed at. When empty, all terrain area types would be valid.")]
        private TerrainAreaType[] placableTerrainAreas = new TerrainAreaType[0];
        public IReadOnlyList<TerrainAreaType> PlacableTerrainAreas => placableTerrainAreas;

        [SerializeField, Tooltip("Can the building be placed outside the faction's territory (defined by the Border)?")]
        private bool canPlaceOutsideBorder = false;
        public bool CanPlaceOutsideBorder => canPlaceOutsideBorder;

        [SerializeField, Tooltip("Can the building be placed inside an enemy faction's territory? This requires the above 'Can Place Outside Broder* to be enabled.")]
        private bool canPlaceInEnemyBorder = false;

        public bool Placed { get; private set; } = false;

        // The value of this field will updated during the placement of the building until the building is placed and the center is set in the Building component.
        public IBorder PlacementCenter { private set; get; }

        [SerializeField, Tooltip("Colliders attached to game objects with layers in this mask will be ignored and not considered as obstacles during placement.")]
        private LayerMask ignoreLayerMask = new LayerMask();

        // How many colliders is the building overlapping with at any given time? It is the size of this list.
        private List<Collider> overlappedColliders;
        private LayerMask ignoreCollisionLayerMask;

        private Collider boundaryCollider = null;

        // Additional placement conditions that can be hooked up into the building
        private IBuildingPlacerCondition[] conditions;

        public bool IsPlacementStarted { private set; get; }
        public BuildingPlacedData PlacedData { get; private set; }

        // Advanced Placement Only
        public virtual BuildingPlacementSegmentData SegmentData => new BuildingPlacementSegmentData
        {
            enabled = false
        };
        public virtual BuildingPlacementGridData GridOptions => new BuildingPlacementGridData { };

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        protected IBuildingPlacement placerMgr { private set; get; }
        protected IGridSearchHandler gridSearch { private set; get; }
        #endregion

        #region Events
        public event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementStatusUpdated;
        public event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementTransformUpdated;
        #endregion

        #region Raising Events
        private void RaiseBuildingPlacementTransformUpdated()
        {
            CustomEventHandler<IBuilding, EventArgs> handler = BuildingPlacementTransformUpdated;
            handler?.Invoke(Building, EventArgs.Empty);
        }

        private void RaiseBuildingPlacementStatusUpdated()
        {
            CustomEventHandler<IBuilding, EventArgs> handler = BuildingPlacementStatusUpdated;
            handler?.Invoke(Building, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.gameMgr = gameMgr;
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.placerMgr = gameMgr.GetService<IBuildingPlacement>();
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();

            this.Building = entity as IBuilding;

            if (!logger.RequireTrue(placableTerrainAreas.Length == 0 || placableTerrainAreas.All(terrainArea => terrainArea.IsValid()),
                  $"[{GetType().Name} - {Building.Code}] The 'Placable Terrain Areas' field must be either empty or populated with valid elements!"))
                return;

            // Boundary collider is only used to detect collisions and therefore having it as trigger is just enough.
            boundaryCollider = gameObject.GetComponent<Collider>();
            if (!logger.RequireValid(boundaryCollider,
                $"[{GetType().Name} - {Building.Code}] Building object must have a Collider component attached to it to detect obstacles while placing the building!"))
                return;
            boundaryCollider.isTrigger = true;

            // This allows the boundary collider to be ignored for mouse clicks and mouse hovers.
            boundaryCollider.gameObject.layer = 2;

            conditions = gameObject.GetComponents<IBuildingPlacerCondition>();

            // If the building is not a placement instance then it is placed by default.
            Placed = !Building.IsPlacementInstance;

            // Make sure that the base terrain is excluded from the overlapping colliders check
            ignoreCollisionLayerMask = new LayerMask();
            ignoreCollisionLayerMask |= terrainMgr.BaseTerrainLayerMask;
            // Ignore the terrain areas that the placement manager asks to ignore on placement
            foreach (TerrainAreaType terrainArea in placerMgr.IgnoreTerrainAreas)
                ignoreCollisionLayerMask |= terrainArea.Layers;

            // User-defined building-specific layer mask to ignore is added as well
            ignoreCollisionLayerMask |= ignoreLayerMask;

            OnInit();
        }

        protected virtual void OnInit() { }

        public void InitPlaced(BuildingPlacedData placedData)
        {
            this.PlacedData = placedData;

            OnPlacedInit();
        }

        protected virtual void OnPlacedInit() { }

        public void Disable() { }
        #endregion

        #region Handling Placement Status
        public void OnPlacementStart(BuildingPlacementUpdateData data)
        {
            overlappedColliders = new List<Collider>();
            CanPlace = false;
            IsPlacementStarted = true;

            OnPlacementStarted(data);

            OnPlacementUpdate();
        }
        protected virtual void OnPlacementStarted(BuildingPlacementUpdateData data) { }

        public void OnPlacementStop(BuildingPlacementUpdateData data)
        {
            OnPlacementStopped(data);
        }
        protected virtual void OnPlacementStopped(BuildingPlacementUpdateData data) { }

        public void OnPlacementUpdate()
        {
            OnPlacementUpdate(changePosition: false);
        }
        public void OnPlacementUpdate(Vector3 position)
        {
            OnPlacementUpdate(changePosition: true, newPosition: position);
        }
        public void OnPlacementUpdate(bool changePosition = false, Vector3 newPosition = default)
        {
            if (Placed
                || !RTSHelper.HasAuthority(Building))
                return;

            if(changePosition)
                OnPositionUpdate(newPosition);

#if UNITY_EDITOR
            if(debug)
            {
                OnDebugPlacement();
            }    
#endif

            RaiseBuildingPlacementTransformUpdated();

            //if the building is not in range of a building center, not on the map or not around the entity that is has to be around within a certain range
            //--> not placable
            TogglePlacementStatus();
        }
        protected virtual void OnPositionUpdate(Vector3 position)
        {
            Building.transform.position = position;
        }

        private void TestPlacementRequirements()
        {
            CanPlaceError = ErrorMessage.none;

            if ((CanPlaceError = placerMgr.CanPlace(this)) != ErrorMessage.none)
                return;
            else if (!IsBuildingInBorder())
                CanPlaceError = ErrorMessage.placementNotInBorder;
            else if (!IsBuildingOnMap())
                CanPlaceError = ErrorMessage.placementNotOnMap;
            else if (overlappedColliders.Count > 0 && overlappedColliders.Any(elem => elem.IsValid()))
                CanPlaceError = ErrorMessage.placementOverlapColliders;
            else if (conditions.Length > 0)
            {
                for (int i = 0; i < conditions.Length; i++)
                {
                    IBuildingPlacerCondition condition = conditions[i];
                    if ((CanPlaceError = condition.CanPlaceBuilding(Building)) != ErrorMessage.none)
                        break;
                }
            } 
        }

        private void TogglePlacementStatus ()
        {
            TestPlacementRequirements();

            CanPlace = CanPlaceError == ErrorMessage.none;

            if(Building.IsLocalPlayerFaction())
                Building.SelectionMarker?.Enable((CanPlace) ? Color.green : Color.red);

            RaiseBuildingPlacementStatusUpdated();
            globalEvent.RaiseBuildingPlacementStatusUpdatedGlobal(Building);

            OnPlacementStatusUpdated();
        }
        protected virtual void OnPlacementStatusUpdated() { }
        #endregion

        #region Handling Placement Conditions
        private void OnTriggerEnter(Collider other)
        {
            // Ignore colliders that belong to this building (its selection colliders namely) and ones attached to effect objects
            if (!IsPlacementStarted
                || Placed 
                || ignoreCollisionLayerMask == (ignoreCollisionLayerMask | (1 << other.gameObject.layer))
                || Building.Selection.IsSelectionCollider(other)
                || other.gameObject.GetComponent<IEffectObject>().IsValid()
                // If the collider is attached to any model object then do not count it as an obstacle
                || other.gameObject.GetComponentInParent<EntityModelConnections>())
                return;

            overlappedColliders.Add(other);

            OnPlacementUpdate();
        }

        private void OnTriggerExit(Collider other)
        {
            // Ignore colliders that belong to this building (its selection colliders namely) and ones attached to effect objects
            if (!IsPlacementStarted
                || Placed 
                || Building.Selection.IsSelectionCollider(other)
                || other.gameObject.GetComponent<IEffectObject>().IsValid()
                || other.gameObject.GetComponentInParent<EntityModelConnections>())
                return;

            overlappedColliders.Remove(other);

            OnPlacementUpdate();
        }

        public bool IsBuildingInBorder()
        {
            bool inRange = false; //true if the building is inside its faction's territory

            if (PlacementCenter.IsValid()) //if the building is already linked to a building center
            {
                //check if the building is still inside this building center's territory
                if (PlacementCenter.IsInBorder(transform.position)) //still inside the center's territory
                    inRange = true; //building is in range
                else
                {
                    inRange = false; //building is not in range
                    PlacementCenter = null; //set the current center to null, so we can find another one
                }
            }

            if (!PlacementCenter.IsValid()) //if at this point, the building doesn't have a building center.
            {
                foreach (IBuilding center in Building.FactionMgr.BuildingCenters)
                {
                    if (!center.BorderComponent.IsActive) //if the border of this center is not active yet
                        continue;

                    if (center.BorderComponent.IsInBorder(Building.transform.position) && center.BorderComponent.IsBuildingAllowedInBorder(Building)) //if the building is inside this center's territory and it's allowed to have this building around this center
                    {
                        inRange = true; //building center found
                        PlacementCenter = center.BorderComponent;
                        break; //leave the loop
                    }
                }
            }

            if (canPlaceOutsideBorder)
                inRange = true;
            
            if ((PlacementCenter.IsValid() || canPlaceOutsideBorder) && inRange) //if, at this point, the building has a center assigned
            {
                //Sometimes borders collide with each other but the priority of the borders is determined by the order of the creation of the borders
                //That's why we need to check for other factions' borders and make sure the building isn't inside one of them:

                foreach(IBorder border in buildingMgr.AllBorders)
                {
                    //if the border is not active or it belongs to the building's faction
                    if (!border.IsActive || border.Building.IsFriendlyFaction(Building) || canPlaceInEnemyBorder)
                        continue; //off to the next one

                    if (border.IsInBorder(Building.transform.position) 
                        && (!PlacementCenter.IsValid() || border.SortingOrder > PlacementCenter.SortingOrder)) //if the building is inside this center's territory
                    {
                        //and if the border has a priority over the one that the building belongs to:
                        return false;
                    }

                }
            }

            return inRange; //return whether the building is in range a building center or not
        }

        public bool IsBuildingOnMap()
        {
            Ray ray = new Ray(); //create a new ray
            RaycastHit[] hits; //this will hold the registerd hits by the above ray

            BoxCollider boxCollider = boundaryCollider.GetComponent<BoxCollider>();

            //Start by checking if the middle point of the building's collider is over the map.
            //Set the ray check source point which is the center of the collider in the game world:
            ray.origin = new Vector3(transform.position.x + boxCollider.center.x, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z);

            ray.direction = Vector3.down; //The direction of the ray is always down because we want check if there's terrain right under the building's object:

            int i = 4; //we will check the four corners and the center
            while (i > 0) //as long as the building is still on the map/terrain
            {
                hits = Physics.RaycastAll(ray, placerMgr.TerrainMaxDistance); //apply the raycast and store the hits

                bool hitTerrain = false; //did one the hits hit the terrain?
                foreach(RaycastHit rh in hits) //go through all hits
                    if (terrainMgr.IsTerrainArea(rh.transform.gameObject, placableTerrainAreas)) 
                        hitTerrain = true;

                if (hitTerrain == false) //if there was no registerd terrain hit
                    return false; //stop and return false

                i--;

                //If we reached this stage, then applying the last raycast, we successfully detected that there was a terrain under it, so we'll move to the next corner:
                switch (i)
                {
                    case 0:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x + boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z + boxCollider.size.z / 2);
                        break;
                    case 1:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x + boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z - boxCollider.size.z / 2);
                        break;
                    case 2:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x - boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z - boxCollider.size.z / 2);
                        break;
                    case 3:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x - boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z + boxCollider.size.z / 2);
                        break;
                }
            }

            return true; //at this stage, we're sure that the center and all corners of the building are on the map, so return true
        }
        #endregion

#if UNITY_EDITOR
        #region Editor
        [Space(), SerializeField, Tooltip("Debug message content: If all placement requirements are met or the error code that is making the placement not allowed.")]
        private bool debug = false;

        private void OnDebugPlacement()
        {
            if (!Building.IsLocalPlayerFaction())
                return;

            string debugMessage;
            switch(CanPlaceError)
            {
                case ErrorMessage.none:
                    debugMessage = "All placement requirements are met!";
                    break;
                default:
                    debugMessage = $"{CanPlaceError}";
                    break;
            }

            logger.Log($"[{Building.Code} - Placement Status] {debugMessage}", source: this);
        }
        #endregion
#endif
    }
}

