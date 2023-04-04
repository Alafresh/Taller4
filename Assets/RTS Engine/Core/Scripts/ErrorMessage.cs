namespace RTSEngine
{
    public enum ErrorMessage
    {
        // -----------------------------------------
        // EntitySelectionGroup
        unitGroupSet,
        unitGroupEmpty,
        unitGroupSelected,
        // -----------------------------------------

        none, // NO ERROR MESSAGE

        inactive, 
        undefined,
        disabled,

        invalid,
        blocked,
        locked,
        failed,

        noAuthority,

        // IEntity
        uninteractable,
        healthDead,
        entityCodeMismatch,

        // Movement
        mvtDisabled,
        mvtTargetPositionNotFound,
        mvtPositionMarkerReserved,
        mvtPositionNavigationOccupied,
        mvtPositionObstacleReserved,

        // IEntityTargetComponent
        targetOutOfRange,
        targetPickerUndefined,
        targetDead,
        targetHealthtMaxReached,

        // Search
        searchCellNotFound,
        searchTargetNotFound,
        searchAreaMissingFullAmount,

        // Health
        healthPreAddBlocked,
        healthtMaxReached,
        healthLow,
        healthNoIncrease,
        healthNoDecrease,

        // Resources
        resourceOutsideTerritory,
        resourceNotCollectable,
        resourceTypeMismatch,
        resourceTypeLimitCapacityReached,

        // WorkerManager
        workersMaxAmountReached,

        // Rallypoint 
        rallypointTargetNotInRange,
        rallypointTerrainAreaMismatch,

        // Dropoff
        dropOffTargetMissing,
        dropOffMaxCapacityReached,
        dropOffComponentMissing,

        // Faction
        factionLimitReached,
        factionUnderAttack,
        factionMismatch,
        factionIsFriendly,
        factionLocked,

        // Task/Action
        taskSourceCanNotLaunch,
        taskMissingFactionEntityRequirements,
        taskMissingResourceRequirements,

        // IUnitCarrier
        carrierCapacityReached,
        carrierIdleOnlyAllowed,
        carrierAttackerNotAllowed,
        carrierMissing,
        carriableComponentMissing,
        carrierForceSlotOccupied,

        // LOS
        LOSObstacleBlocked,
        LOSAngleBlocked,

        // Attack
        attackTypeActive,
        attackTypeLocked,
        attackTypeNotFound,
        attackTypeInCooldown,
        attackTargetNoChange,
        attackTargetRequired,
        attackTargetOutOfRange,
        attackSourceDisabled,
        attackTargetDisabled,
        attackPositionNotFound,
        attackPositionOutOfRange,
        attackMoveToTargetOnly,
        attackTerrainDisabled,
        attackAlreadyInPosition,

        // Upgrade
        upgradeLaunched,
        upgradeTypeMismatch,

        // UnitCreator 
        unitCreatorMaxLaunchTimesReached,
        unitCreatorMaxActiveInstancesReached,

        // Terrain
        terrainHeightCacheNotFound,

        // Selection
        positionOutOfSelectionBounds,
        selected,

        // Placement
        placementSegmentAmountNotMet,
        placementBuildingCenterMissing,
        alreadyPlaced,
        placementNotInBorder,
        placementNotOnMap,
        placementOverlapColliders,
        placementAroundInvalid,
        placementInFog,
        placementGridOccupied,

        // Construction
        constructionResourcesMissing,

        // Game
        gameFrozen,
        gamePeaceTimeActive,

        // Lobby
        lobbyMinSlotsUnsatisfied,
        lobbyMaxSlotsUnsatisfied,
        lobbyHostOnly,
        lobbyPlayersNotAllReady,
    }
}
