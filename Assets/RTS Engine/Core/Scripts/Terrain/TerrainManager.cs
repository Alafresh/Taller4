using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;
using RTSEngine.Utilities;
using RTSEngine.Cameras;
using RTSEngine.Model;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;

namespace RTSEngine.Terrain
{
    public struct TerrainAreaMask
    {
        public int mask { private set; get; }

        public TerrainAreaMask(int mask)
        {
            this.mask = mask;
        }

        public void Add(int areaIndex)
        {
            this.mask |= 1 << areaIndex;
        }

        public bool Intersect(TerrainAreaMask other)
        {
            return other.mask == 0 || mask == 0 || (other.mask & mask) != 0;
        }

        public bool HasAllAreas() => mask == 0 || mask == ~0;
    }

    public class TerrainManager : MonoBehaviour, ITerrainManager
    {
        #region Attributes
        [SerializeField, Tooltip("Add an element for each terrain area type you have in the map (ground, air, water, etc..).")]
        private TerrainAreaType[] areas = new TerrainAreaType[0];
        public IEnumerable<TerrainAreaType> Areas => areas;
        // key: unique code of the terrain area type.
        private IReadOnlyDictionary<string, TerrainAreaType> areasDic = null;

        // Terrain Area Mask layers related
        private IReadOnlyDictionary<TerrainAreaType, int> AreaTypeToLayer;
        private IReadOnlyDictionary<int, TerrainAreaType> AreaLayerToType;

        [SerializeField, Min(1.0f), Tooltip("Approximation of the map size. This value is used by NPC factions to determine the amount of territory it aims to control within the map.")]
        private float mapSize = 16900;
        public float MapSize => mapSize;

        // Height Caching
        [Header("Height Caching"), SerializeField, Tooltip("Defines the lower-left corner of the map as a boundary for caching height values.")]
        private Int2D lowerLeftCorner = new Int2D { x = 0, y = 0 };
        public Int2D HeightCacheLowerLeftCorner => lowerLeftCorner;

        [SerializeField, Tooltip("Defines the upper-right corner of the map as a boundary for caching height values.")]
        private Int2D upperRightCorner = new Int2D { x = 120, y = 120 };
        public Int2D HeightCacheUpperRightCorner => upperRightCorner;

        [SerializeField, Tooltip("Starting from the lower left corner of the map, move by this distance value to cache height values each time until reaching the upper right corner of the map."), Min(0)]
        private int heightCacheDistance = 1;

        [SerializeField, Tooltip("When sampling terrain's height to cache, this offset is added to the raycast source position which will head downwards to detect the terrain object and sample the height."), Min(1)]
        private int heightCacheSampleOffset = 10;

        [Header("Base Terrain"), SerializeField, Tooltip("A pre-defined layer (make sure it is defined in your project settings layers and not used on any object by default) used to create a collider to be used for caching, main camera and minimap clicks among other things. By default it is assigned to 'BaseTerrain'")]
        private string baseTerrainLayer = "BaseTerrain";
        public LayerMask BaseTerrainLayerMask { private set; get; }

        [SerializeField, Tooltip("The center of the base terrain. It is recommended to have the base terrain at a level lower than all other terrain areas.")]
        private Vector3 baseTerrainCenter = new Vector3(60.0f, 0.0f, 60.0f);
        [SerializeField, Tooltip("The size of the base terrain. Make sure this covers more than the entire map so that at any allowed camera position, a ray can be drawn from any of the camera's corners and it hits the base terrain collider.")]
        private Vector2 baseTerrainSize = new Vector2(x: 350, y: 350);

        [SerializeField, Tooltip("When inspecting the main camera's boundaries and using them to determine the positions on the base terrain that they hit, this margin value allows to widen the main camera's boundaries. This is mainly useful in caching model objects as a small margin would allow to display models just outside of the camera view to make sure that they are visible immediately when the camera pans over a little to look at the model objects. Pick the margin applied to the top right and top left corners of the camera view (Upper Margin) and the one applied to the bottom left and right corners (Bottom Margin).")]
        public CameraBoundariesMargin cameraBoundariesToBaseTerrainPositionMargin = new CameraBoundariesMargin { upperMargin = 10, bottomMargin = 2000 };
        public CameraBoundariesToTerrainPositions BaseTerrainCameraBounds { get; private set; }

        [SerializeField, Tooltip("In case the main camera can be rotated to NOT face the bottom of the map at all times, enable this option to generate base terrain colliders on the sides of the map to avoid the raycasting error that occurs when the main camera is not direclty facing the bottom of the map.")]
        private bool enableSideBaseTerrain = false;

        // Dictionary that holds the cached height values where:
        // key (string): unique identifier of the terrain area type
        // value: A dictionary that has Int2D positions as a key and the height as a float value for each position.
        private Dictionary<string, Dictionary<Int2D, float>> heightCacheDict;

        // Game services
        protected IMovementManager mvtMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; } 

        // Other components
        protected IGameManager gameMgr { private set; get; }

        public int Priority => 200; 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.mvtMgr = this.gameMgr.GetService<IMovementManager>();
            this.logger = this.gameMgr.GetService<IGameLoggingService>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>(); 

            if (!logger.RequireTrue(areas.Length > 0,
              $"[{GetType().Name}] The 'Areas' field must at least have one element!"))
                return;
            else if (!logger.RequireValid(areas,
              $"[{GetType().Name}] The 'Areas' field has some invalid elements!"))
                return;
            else if (!logger.RequireTrue(areas.Distinct().Count() == areas.Length,
              $"[{GetType().Name}] The 'Areas' field can not have duplicate elements!"))
                return;

            areasDic = areas
                .ToDictionary(area => area.Key, area => area);

            int counter = -1;
            AreaTypeToLayer = areas
                .ToDictionary(area => area, area => { counter++; return counter; });
            counter = -1;
            AreaLayerToType = areas
                .ToDictionary(area => { counter++; return counter; }, area => area);

            StartCoroutine(CacheHeightValues());

            InitBaseTerrain();
        }

        private void InitBaseTerrain()
        {
            GameObject colliderChildObject = new GameObject("BaseTerrain_Collider_Bottom");
            colliderChildObject.transform.SetParent(transform);
            colliderChildObject.transform.position = baseTerrainCenter;
            colliderChildObject.transform.localScale = Vector3.one;
            colliderChildObject.layer = RTSHelper.TryNameToLayer(baseTerrainLayer);

            // Bottom collider
            var bottomCollider = colliderChildObject.AddComponent<BoxCollider>();
            //bottomCollider.center = transform.InverseTransformPoint(baseTerrainCenter);
            bottomCollider.size = new Vector3(baseTerrainSize.x, 1.0f, baseTerrainSize.y);

            if (enableSideBaseTerrain)
            {
                // Side Colliders
                int sign = 1;
                for (int i = 0; i < 4; i++)
                {
                    GameObject nextSideCollider = Instantiate(colliderChildObject);
                    nextSideCollider.transform.SetParent(transform);
                    nextSideCollider.transform.localScale = Vector3.one;
                    nextSideCollider.gameObject.name = $"BaseTerrain_Collider_Side_{i+1}";

                    nextSideCollider.transform.localPosition = new Vector3(
                        i < 2 ? sign * (baseTerrainSize.x / 2.0f + (sign * baseTerrainCenter.x)) : baseTerrainCenter.x,
                        baseTerrainCenter.y,
                        i >= 2 ? sign * (baseTerrainSize.y / 2.0f + (sign * baseTerrainCenter.z)) : baseTerrainCenter.z);

                    nextSideCollider.transform.localEulerAngles = new Vector3(
                        i >= 2 ? 90.0f : 0.0f,
                        0.0f,
                        i < 2 ? 90.0f : 0.0f);

                    sign *= -1;
                }
            }

            BaseTerrainLayerMask = new LayerMask();
            BaseTerrainLayerMask |= (1 << colliderChildObject.layer);

            BaseTerrainCameraBounds = new CameraBoundariesToTerrainPositions(
                new RaycastHitter(gameMgr.GetService<ITerrainManager>().BaseTerrainLayerMask),
                gameMgr.GetService<IMainCameraController>().MainCamera,
                cameraBoundariesToBaseTerrainPositionMargin
            );
        }
        #endregion

        #region Caching Terrain Height Values
        private IEnumerator CacheHeightValues()
        {
            if (heightCacheDistance < 0)
            {
                logger.LogError($"[{GetType().Name}] The height cache movement distance (field: 'Height Cache Distance') must be > 0.");
                yield return null;
            }

            heightCacheDict = new Dictionary<string, Dictionary<Int2D, float>>();

            foreach (TerrainAreaType areaType in areas)
            {
                heightCacheDict.Add(areaType.Key, new Dictionary<Int2D, float>());

                for (int x = lowerLeftCorner.x; x < upperRightCorner.x; x += heightCacheDistance)
                    for (int y = lowerLeftCorner.y; y < upperRightCorner.y; y += heightCacheDistance)
                    {
                        // Each search cell instance is added to the dictionary after it is created for easier direct access using coordinates in the future.
                        Int2D nextPosition = new Int2D
                        {
                            x = x,
                            y = y
                        };

                        if(GetTerrainAreaPosition(
                            new Vector3(nextPosition.x, areaType.BaseHeight + heightCacheSampleOffset, nextPosition.y),
                            areaType.Key,
                            out Vector3 outPosition))
                            heightCacheDict[areaType.Key].Add(nextPosition, outPosition.y);
                    }
            }

            yield return null;
        }

        public ErrorMessage TryGetCachedHeight(Vector3 position, IReadOnlyList<TerrainAreaType> areaTypes, out float height, bool logWarning = false)
        {
            // When no specific terrain area is supplied then check all of the possible ones.
            if (areaTypes == null || areaTypes.Count == 0)
                areaTypes = areas;

            height = position.y;

            // Find the coordinates of the potential search cell where the input position is in
            Int2D nextPosition = new Int2D
            {
                x = (((int)position.x - lowerLeftCorner.x) / heightCacheDistance) * heightCacheDistance + lowerLeftCorner.x,
                y = (((int)position.z - lowerLeftCorner.y) / heightCacheDistance) * heightCacheDistance + lowerLeftCorner.y
            };

            float closestHeightDistance = Mathf.Infinity;
            float nextDistance;
            bool hasFoundHeight = false;

            for (int i = 0; i < areaTypes.Count; i++)
                if (heightCacheDict[areaTypes[i].Key].TryGetValue(nextPosition, out float nextHeight))
                {
                    hasFoundHeight = true;
                    if ((nextDistance = Mathf.Abs(nextHeight - position.y)) < closestHeightDistance)
                    {
                        closestHeightDistance = nextDistance;
                        height = nextHeight;
                    }
                }

            if(hasFoundHeight)
                return ErrorMessage.none;

            if(logWarning)
                logger.LogWarning(
                    $"[TerrainManager] Unable to get the height for position: {position}! Consider increasing the size of the height sampling area by modifying the lower left corner and top right corner fields.",
                    source: this);

            return ErrorMessage.terrainHeightCacheNotFound;
        }
        #endregion

        #region Sampling Height
        public  bool SampleHeight(Vector3 position, IMovementComponent refMvtComp, out float height)
            => SampleHeight(position, refMvtComp.TerrainAreas, out height);

        public bool SampleHeight(Vector3 position, IReadOnlyList<TerrainAreaType> areaTypes, out float height)
            => TryGetCachedHeight(position, areaTypes, out height) == ErrorMessage.none;
        #endregion

        #region Handling Terrain Area Masks
        public TerrainAreaMask TerrainAreasToMask (IReadOnlyList<TerrainAreaType> areaTypes)
        {
            if (areaTypes.Count == 0)
                return new TerrainAreaMask(~0); // All terrain area types.

            TerrainAreaMask mask = new TerrainAreaMask(); // No terrain areas assigned initially.

            // Add each area type index to the mask
            for (int i = 0; i < areaTypes.Count; i++)
                mask.Add(AreaTypeToLayer[areaTypes[i]]);

            return mask;
        }
        #endregion

        #region Handling Terrain Areas
        public bool IsTerrainArea(GameObject obj)
            => IsTerrainArea(obj, areas);

        public bool IsTerrainArea(GameObject obj, IReadOnlyList<TerrainAreaType> areaTypes)
        {
            if(!areaTypes.IsValid() || areaTypes.Count == 0)
                return true;

            for (int i = 0; i < areaTypes.Count; i++)
            {
                if (IsTerrainArea(obj, areaTypes[i].Key))
                    return true;
            }

            return false;
        }

        public bool IsTerrainArea(GameObject obj, int areaLayer)
        {
            if (!AreaLayerToType.TryGetValue(areaLayer, out TerrainAreaType areaType))
            {
                logger.LogError($"[TerrainManager] The input area layer: {areaLayer} has not been registered for this map!");
                return false;
            }

            return IsTerrainArea(obj, areaType);
        }

        public bool IsTerrainArea(GameObject obj, string areaKey)
        {
            if (!areasDic.TryGetValue(areaKey, out TerrainAreaType areaType))
            {
                logger.LogError($"[TerrainManager] The input area key: {areaKey} has not been registered for this map!");
                return false;
            }

            return IsTerrainArea(obj, areaType);
        }

        public bool IsTerrainArea(GameObject obj, TerrainAreaType areaType) => areaType.Layers == (areaType.Layers | (1 << obj.layer));

        public bool GetTerrainAreaPosition(Vector3 inPosition, IReadOnlyList<TerrainAreaType> areaTypes, out Vector3 outPosition)
        {
            outPosition = inPosition;

            if (!areaTypes.IsValid() || areaTypes.Count == 0)
                return true;

            float closestHeightDistance = Mathf.Infinity;
            float nextDistance;
            bool hasFoundPosition = false;

            for (int i = 0; i < areaTypes.Count; i++)
            {
                inPosition.y += areaTypes[i].TestHeightOffset;

                if (GetTerrainAreaPosition(inPosition, areaTypes[i].Key, out Vector3 nextPosition))
                {
                    hasFoundPosition = true;
                    if ((nextDistance = (nextPosition - inPosition).sqrMagnitude) < closestHeightDistance)
                    {
                        closestHeightDistance = nextDistance;
                        outPosition = nextPosition;
                    }
                }
            }

            if(hasFoundPosition)
                return true;

            return false;
        }

        public bool GetTerrainAreaPosition(Vector3 inPosition, TerrainAreaType areaType, out Vector3 outPosition)
            => GetTerrainAreaPosition(inPosition, areaType.Key, out outPosition);

        public bool GetTerrainAreaPosition(Vector3 inPosition, string areaKey, out Vector3 outPosition)
        {
            outPosition = inPosition;

            if (!areasDic.TryGetValue(areaKey, out TerrainAreaType areaType))
            {
                logger.LogError($"[TerrainManager] The input area key: {areaKey} has not been registered for this map!");
                return false;
            }

            inPosition.y += areaType.TestHeightOffset;

            // Create a ray that goes down vertically and attempt to find the terrain area.
            // Use JobSystem to shoot the rays into threads

            NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(1, Allocator.TempJob);
            NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(1, Allocator.TempJob);

            commands[0] = new RaycastCommand(inPosition, Vector3.down, Mathf.Infinity, areaType.Layers);

            JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, minCommandsPerJob: 1, dependsOn: default(JobHandle));
            handle.Complete();

            RaycastHit hit = results[0];

            results.Dispose();
            commands.Dispose();

            if(hit.collider.IsValid())
            {
                outPosition = hit.point;
                return true;
            }

            return false;
        }
        #endregion

        #region Helper Methods
        public bool ScreenPointToTerrainPoint(Vector3 screenPoint, IReadOnlyList<TerrainAreaType> areaTypes, out Vector3 terrainPoint)
        {
            int layers = 0;

            IReadOnlyList<TerrainAreaType> nextAreaTypes = (areaTypes.IsValid() && areaTypes.Count > 0) ? areaTypes : areas;
            for (int i = 0; i < nextAreaTypes.Count; i++)
                layers |= nextAreaTypes[i].Layers;

            if (Physics.Raycast(mainCameraController.MainCamera.ScreenPointToRay(screenPoint), out RaycastHit hit, Mathf.Infinity, layers))
            {
                terrainPoint = hit.point;
                return true;
            }

            terrainPoint = Vector3.zero;
            return false;
        }
        #endregion

        #region Drawing Terrain Height Caching Area 
#if UNITY_EDITOR
        [Header("Gizmos")]
        public Color heightCacheAreaColor = Color.green;
        public Color baseTerrainAreaColor = Color.blue;

        private void OnDrawGizmosSelected()
        {
            if (heightCacheDistance > 0)
            {
                Gizmos.color = heightCacheAreaColor;
                Vector3 size = new Vector3(upperRightCorner.x - lowerLeftCorner.x, 1.0f, upperRightCorner.y - lowerLeftCorner.y);
                Gizmos.DrawWireCube(new Vector3(lowerLeftCorner.x + size.x/2.0f, 0.0f, lowerLeftCorner.y + size.z/2.0f), size);
            }

            Gizmos.color = baseTerrainAreaColor;
            Gizmos.DrawWireCube(baseTerrainCenter, new Vector3(baseTerrainSize.x, 1.0f, baseTerrainSize.y));

            if (BaseTerrainCameraBounds.IsValid())
            {
                Debug.DrawLine(BaseTerrainCameraBounds.Get().TopLeft, BaseTerrainCameraBounds.Get().TopRight, Color.blue);
                Debug.DrawLine(BaseTerrainCameraBounds.Get().TopRight, BaseTerrainCameraBounds.Get().BottomRight, Color.green);
                Debug.DrawLine(BaseTerrainCameraBounds.Get().BottomRight, BaseTerrainCameraBounds.Get().BottomLeft, Color.yellow);
                Debug.DrawLine(BaseTerrainCameraBounds.Get().BottomLeft, BaseTerrainCameraBounds.Get().TopLeft, Color.red);
            }
        }
#endif
        #endregion
    }
}