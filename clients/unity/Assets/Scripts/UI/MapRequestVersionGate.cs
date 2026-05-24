namespace SpaceTraders.UI.Map
{
    internal sealed class MapRequestVersionGate
    {
        private int _currentVersion;

        public int Begin()
        {
            _currentVersion++;
            return _currentVersion;
        }

        public bool IsCurrent(int requestVersion)
        {
            return requestVersion == _currentVersion;
        }
    }
}
