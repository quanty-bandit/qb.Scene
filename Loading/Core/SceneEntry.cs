using System;
using TriInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using System.Collections.Generic;
#endif
namespace qb.SceneManagement
{
    /// <summary>
    /// Scene entry container to indexed
    /// by id and index a scene source location and
    /// loading mode.
    /// </summary>

    [Serializable]
    public class SceneEntry
    {
        [SerializeField, ReadOnly]
        protected int index;
        public int Index
        {
            get => index;
#if UNITY_EDITOR
            set
            {
                if (Application.isPlaying)
                    throw new Exception("This parameter cannot be set at runtime!");
                index = value;
            }
#endif
        }

#if UNITY_EDITOR
        [ValidateInput(nameof(ValidateUniqueId))]
#endif
        [SerializeField]
        protected string id;

        public string Id
        {
            get => id;
#if UNITY_EDITOR
            set
            {
                if (Application.isPlaying)
                    throw new System.Exception("This parameter cannot be set at runtime!");
                id = value;
            }
#endif
        }

        [SerializeField]
        LoadSceneMode loadSceneMode = LoadSceneMode.Additive;
        public LoadSceneMode LoadSceneMode => loadSceneMode;

        [SerializeField]
        bool activateAfterLoad = true;
        public bool ActivateAfterLoad => activateAfterLoad;

        public enum Location { BuildIn, Addressable }
        [SerializeField]
        protected Location sceneLocation;
        public Location SceneLocation => sceneLocation;

        #region build in scene property
#if UNITY_EDITOR
        [Dropdown(nameof(GetBuildInSceneNames)), ShowIf(nameof(IsBuildIn))]
#endif
        [SerializeField, Required]
        private string buildInSceneName;
        public string BuildInSceneName => buildInSceneName;
        #endregion

        #region addressable property
#if UNITY_EDITOR
        [ValidateInput(nameof(ValidateUniqueSceneRef)), HideIf(nameof(IsBuildIn))]
#endif
        [SerializeField, PropertySpace(spaceBefore: 0, spaceAfter: 10)]
        private Scene_AssetReference sceneReference;
        public Scene_AssetReference SceneReference => sceneReference;
        #endregion

#if UNITY_EDITOR
        private TriValidationResult ValidateUniqueId()
        {
            if (ValidateUniqueIdFunc == null) return TriValidationResult.Valid;
            return ValidateUniqueIdFunc(this);
        }
        public Func<SceneEntry, TriValidationResult> ValidateUniqueIdFunc;

        private bool IsBuildIn => sceneLocation == Location.BuildIn;
        private IEnumerable<TriDropdownItem<string>> GetBuildInSceneNames()
        {
            if (GetBuildInSceneNamesFunc != null)
                return GetBuildInSceneNamesFunc(buildInSceneName);
            return new TriDropdownList<string> { { buildInSceneName, buildInSceneName } };
        }
        public Func<string, IEnumerable<TriDropdownItem<string>>> GetBuildInSceneNamesFunc;

        private TriValidationResult ValidateUniqueSceneRef()
        {
            if (ValidateUniqueSceneRefFunc == null || sceneLocation != Location.Addressable) return TriValidationResult.Valid;
            return ValidateUniqueSceneRefFunc(this);
        }
        public Func<SceneEntry, TriValidationResult> ValidateUniqueSceneRefFunc;
#endif
        public bool IsValid => sceneLocation == Location.BuildIn ? !string.IsNullOrEmpty(buildInSceneName) : sceneReference!=null;
    }

}
