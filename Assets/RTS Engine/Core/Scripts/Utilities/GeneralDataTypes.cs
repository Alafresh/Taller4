namespace RTSEngine.Utilities
{
    [System.Serializable]
    public struct SmoothSpeed
    {
        public float value;
        public float smoothFactor;
    }

    [System.Serializable]
    public struct ToggableSmoothFactor
    {
        public bool enabled;
        public float smoothFactor;
    }
}
