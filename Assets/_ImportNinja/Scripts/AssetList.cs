using System.Collections.Generic;
using UnityEngine;

namespace ImportNinja
{
    public class AssetList : ScriptableObject
    {
        public List<AssetInfo> assets;
    }

    [System.Serializable]
    public class AssetInfo
    {
        public string name;
        public string URL;
    }
}
