using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;


#if UNITY_EDITOR
using TriInspector;
using UnityEditor;
#endif
namespace qb.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneList", menuName = "qb/SceneList")]
    public class SceneList : ScriptableObject, IEnumerable
    {
#if UNITY_EDITOR
        [OnValueChanged(nameof(UpdateFuncAndIndex))]
#endif
        [SerializeField]
        private List<SceneEntry> content = new List<SceneEntry>();

        public SceneEntry this[int key] => key >= 0 && key < content.Count ? content[key] : null;
        public SceneEntry this[string key]
        {
            get
            {
                foreach (var entry in content)
                {
                    if (entry.Id == key) return entry;
                }
                return null;
            }
        }
        public int Count=>content.Count;

        public IEnumerator GetEnumerator()
        {
            return content.GetEnumerator();
        }

        #region inspector entries validation
#if UNITY_EDITOR
        private void UpdateFuncAndIndex()
        {
            for (int i = 0; i < content.Count; i++)
            {
                var sceneEntry = content[i];
                sceneEntry.ValidateUniqueIdFunc = ValidateUniqueId;
                sceneEntry.GetBuildInSceneNamesFunc = GetBuildInSceneNames;
                sceneEntry.ValidateUniqueSceneRefFunc = ValidateUniqueSceneRef;
                sceneEntry.Index = i;
            }
        }
        private TriValidationResult ValidateUniqueId(SceneEntry entry)
        {
            var id = entry.Id;
            if (string.IsNullOrEmpty(id)) return TriValidationResult.Error("The id can't be empty!");
            for (int i = 0; i < content.Count; i++)
            {
                var sceneEntry = content[i];
                if (sceneEntry == entry) continue;
                if (sceneEntry.Id == id) return TriValidationResult.Error($"The id is already isUsed by the entry n° {i}");
            }
            return TriValidationResult.Valid;
        }

        private IEnumerable<TriDropdownItem<string>> GetBuildInSceneNames(string sceneInBuildName)
        {
            EditorBuildSettingsScene[] buildInScenes = EditorBuildSettings.scenes;


            if (buildInScenes.Length > 0)
            {
                List<string> usedSceneNames = new List<string>();

                foreach (var entry in content)
                {
                    if (!usedSceneNames.Contains(entry.BuildInSceneName))
                        usedSceneNames.Add(entry.BuildInSceneName);
                }

                var result = new List<TriDropdownItem<string>>();
                for (int i = 0; i < buildInScenes.Length; i++)
                {
                    var entry = buildInScenes[i];
                    var name = System.IO.Path.GetFileNameWithoutExtension(entry.path);

                    if (!entry.enabled || (sceneInBuildName != name && usedSceneNames.Contains(name))) continue;

                    result.Add(new TriDropdownItem<string>() { Text = $"{i} {name}", Value = name });
                }
                return result;
            }

            return new TriDropdownList<string> { { sceneInBuildName, sceneInBuildName } };
        }

        private TriValidationResult ValidateUniqueSceneRef(SceneEntry entry)
        {
            var sceneRef = entry.SceneReference;

            if (sceneRef == null || sceneRef.editorAsset == null) return TriValidationResult.Error("The scene reference can't be empty!");
            for (int i = 0; i < content.Count; i++)
            {
                var sceneEntry = content[i];

                if (sceneEntry == entry || sceneEntry.SceneLocation != SceneEntry.Location.Addressable) continue;

                if (sceneEntry.SceneReference.editorAsset == sceneRef.editorAsset) return TriValidationResult.Error($"The scene reference is already isUsed by the entry n° {i}");
            }
            return TriValidationResult.Valid;
        }

        

#endif
        #endregion



    }
}
