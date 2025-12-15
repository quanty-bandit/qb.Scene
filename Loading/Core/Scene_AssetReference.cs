using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
///from https://gist.github.com/shana/2eaaf2f21796c258b05ba794d28207d0
namespace UnityEngine.AddressableAssets
{
    [Serializable]
    public class Scene_AssetReference : AssetReferenceT<SceneReference>
    {
        public Scene_AssetReference(string guid) : base(guid)
        {
        }

        public override bool ValidateAsset(string path)
        {
#if UNITY_EDITOR
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            return typeof(SceneAsset).IsAssignableFrom(type);
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        public new SceneAsset editorAsset
        {
            get
            {
                if (CachedAsset != null || string.IsNullOrEmpty(AssetGUID))
                    return CachedAsset as SceneAsset;

                var assetPath = AssetDatabase.GUIDToAssetPath(AssetGUID);
                var main = AssetDatabase.LoadMainAssetAtPath(assetPath) as SceneAsset;
                if (main != null)
                    CachedAsset = main;
                return main;
            }
        }
#endif
    }

    [Serializable]
    public class SceneReference : UnityEngine.Object
    {
    }
}
