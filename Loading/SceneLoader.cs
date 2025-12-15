using qb.Pattern;
using qb.Events;
using System;
using System.Collections.Generic;
using TriInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace qb.SceneManagement
{
    [DeclareTabGroup("#1")]
    public class SceneLoader : MBSingleton<SceneLoader>
    {

        [SerializeField, Required, InlineEditor]
        SceneList sceneList;

        [SerializeField]
        bool smoothProgress = true;
#if UNITY_EDITOR
        bool ShowSmoothEntries => smoothProgress;
        [ShowIf(nameof(ShowSmoothEntries))]
#endif
        [SerializeField]
        float smoothMinStep = 0.01f;
#if UNITY_EDITOR
        [ShowIf(nameof(ShowSmoothEntries))]
#endif
        [SerializeField]
        float smoothStepDuration = 0.1f;

        /*
        [SerializeField,Group("#1"), Tab("Channel commands")]
        EventChannel<int> loadSceneFromIndex;
        [SerializeField, Group("#1"), Tab("Channel commands")]
        EventChannel<string> loadSceneFromId;
        [SerializeField, Group("#1"), Tab("Channel commands")]
        EventChannel<int> unloadSceneFromIndex;
        [SerializeField, Group("#1"), Tab("Channel commands")]
        EventChannel<string> unloadSceneFromId;
        [SerializeField, Group("#1"), Tab("Channel commands")]
        EventChannel unloadAllScenes;
        */

        [SerializeField, Group("#1"), Tab("Event channels")]
        ECProvider_W<string> onSceneLoadedChannel;
        [SerializeField, Group("#1"), Tab("Event channels")]
        ECProvider_W<float> onLoadingProgressChannel;
        [SerializeField, Group("#1"), Tab("Event channels")]
        ECProvider_W<string> onSceneUnloadedChannel;

        [SerializeField, Group("#1"), Tab("Unity Events")]
        UnityEvent<string> onSceneLoaded = new UnityEvent<string>();
        [SerializeField, Group("#1"), Tab("Unity Events")]
        UnityEvent<float> onSceneLoadingProgress = new UnityEvent<float>();
        [SerializeField, Group("#1"), Tab("Unity Events")]
        UnityEvent<string> onSceneUnloaded = new UnityEvent<string>();

        public override bool IsPersistent => true;
        public override EDuplicatedSingletonInstanceAction DuplicatedInstanceAction => EDuplicatedSingletonInstanceAction.Exception;

        public bool IsValidSceneAt(int index)
        {
            var entry = sceneList[index];
            return entry != null && entry.IsValid;
        }
        public bool IsValidSceneFrom(string id)
        {
            var entry = sceneList[id];
            return entry != null && entry.IsValid;
        }

        /// <summary>
        /// Dictionary of loaded SceneHandle with key will be the idOrAddress 
        /// or address in case of loading from catalog address
        /// </summary>
        private Dictionary<string, SceneHandle> sceneHandlesDic = new Dictionary<string, SceneHandle>();

        private List<SceneHandle> sceneHandleStack = new List<SceneHandle>();

        bool isBusy;
        /// <summary>
        /// Flag which indicates if a loading or unloading process is in progress.
        /// </summary>
        public bool IsBusy => isBusy;
        private bool activationPending;
        private string activationPendingSceneId;

        /// <summary>
        /// Waiting task while isBusy flag is set to true
        /// </summary>
        /// <param name="maxWaintingDuration">Maximum duration waiting in seconds</param>
        /// <param name="resetBusyFlag">
        /// Flag which indicate to reset the isBusy to true
        /// in case of waiting duration > maxWaintingDuration 
        /// </param>
        /// <returns></returns>
        private async Awaitable WaitingWhileIsBusy(float maxWaintingDuration = 60, bool resetBusyFlag = true)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            var maxDuration = Mathf.RoundToInt(maxWaintingDuration * 1000);
            stopwatch.Start();
            while (isBusy && stopwatch.ElapsedMilliseconds < maxDuration)
            {
                await Awaitable.EndOfFrameAsync();
            }
            if (isBusy && resetBusyFlag)
                isBusy = false;
            stopwatch.Stop();
        }
       
        /*
        private void OnEnable()
        {
            if(loadSceneFromIndex)
                loadSceneFromIndex.Event += LoadSceneFromIndex_Command;
            if(loadSceneFromId)
                loadSceneFromId.Event += LoadSceneFromId_Command;
            if(unloadSceneFromIndex)
                unloadSceneFromIndex.Event += UnloadSceneFromIndex_Command;
            if(unloadSceneFromId)
                unloadSceneFromId.Event += UnloadSceneFromId_Command;
            if(unloadAllScenes)
                unloadAllScenes.Event += UnloadAllScenes_Command;
        }

        private void OnDisable()
        {
            if (loadSceneFromIndex)
                loadSceneFromIndex.Event -= LoadSceneFromIndex_Command;
            if (loadSceneFromId)
                loadSceneFromId.Event -= LoadSceneFromId_Command;
            if (unloadSceneFromIndex)
                unloadSceneFromIndex.Event -= UnloadSceneFromIndex_Command;
            if (unloadSceneFromId)
                unloadSceneFromId.Event -= UnloadSceneFromId_Command;
        }

        #region commands management
        private async void LoadSceneCommand(SceneEntry sceneEntry)
        {
            if (sceneEntry == null)
                return;
            await Load(sceneEntry);
        }
        private void LoadSceneFromId_Command(string id) => LoadSceneCommand(sceneList[id]);

        private void LoadSceneFromIndex_Command(int index) => LoadSceneCommand(sceneList[index]);

        private async void UnLoadSceneCommand(SceneEntry sceneEntry)
        {
            if (sceneEntry == null)
                return;
            if (sceneHandlesDic.TryGetValue(sceneEntry.Id, out var handle))
                await TryToUnload(handle);
        }

        private void UnloadSceneFromId_Command(string id) => UnLoadSceneCommand(sceneList[id]);

        private void UnloadSceneFromIndex_Command(int index) => UnLoadSceneCommand(sceneList[index]);


        private void UnloadAllScenes_Command()
        {
            throw new NotImplementedException();
        }

        #endregion
        */

        /// <summary>
        /// TryToUnload all additive scenes
        /// </summary>
        /// <remarks>The method unload only activeted scenes</remarks>
        public async Awaitable UnloadAll()
        {
            if (sceneHandlesDic.Count < 2) return;

            if (isBusy)
            {
                await WaitingWhileIsBusy();
            }
            try
            {
                isBusy = true;
                List<SceneHandle> handles = new List<SceneHandle>();
                foreach (var handle in sceneHandlesDic.Values)
                {
                    handles.Add(handle);
                }

                foreach (var handle in handles)
                    await TryToUnload(handle);
            }
            catch (Exception ex)
            {
#if !NO_DEBUG_LOG_EXCEPTION
                Debug.LogException(ex);
#endif
            }
            finally 
            { 
                isBusy = false; 
            } 
        }
        /// <summary>
        /// Try to unload the last loaded scene 
        /// </summary>
        /// <returns></returns>
        public async Awaitable<bool> TryToUnloadLastLoaded()
        {
            if (sceneHandleStack.Count > 0)
                return await TryToUnload(sceneHandleStack[sceneHandleStack.Count - 1]);

            return false;
        }

        /// <summary>
        /// Try to unload addive scene at index
        /// </summary>
        /// <param name="index">The scene entry index from scene list</param>
        /// <remarks>If the loaded channel scene is not an additive scene, or not activated, the method do nothing</remarks>
        public async Awaitable<bool> TryToUnload(int index)
        {
            var sceneEntry = sceneList[index];
            if (sceneEntry != null)
                return await TryToUnload(sceneEntry);
#if !NO_DEBUG_LOG_WARNING
            Debug.LogWarning($"No scene entry at index: {index}");
#endif
            return false;
        }
        /// <summary>
        /// TryToUnload an additive scene previously load by this SceneLoader instance
        /// </summary>
        /// <param name="idOrAddress">The scene idOrAddress entry or the addressable address key</param>
        /// <remarks>If the loaded channel scene is not an additive scene, or not activated, the method do nothing</remarks>
        public async Awaitable<bool> TryToUnload(string idOrAddress)
        {
            if (sceneHandlesDic.TryGetValue(idOrAddress, out var sceneHandle))
                return await TryToUnload(sceneHandle);
#if !NO_DEBUG_LOG_WARNING
            Debug.LogWarning($"No loading scene handle found at id or address: {idOrAddress}");
#endif
            return false;
        }

        /// <summary>
        /// TryToUnload an additive scene previously load by this SceneLoader instance
        /// </summary>
        /// <param name="sceneEntry">The scene entry of the loaded scene</param>
        /// <remarks>If the loaded channel scene is not an additive scene, or not activated, the method do nothing</remarks>
        public async Awaitable<bool> TryToUnload(SceneEntry sceneEntry)
        {
            var id = sceneEntry.Id;
            if (sceneHandlesDic.TryGetValue(id, out var sceneHandle))
                return await TryToUnload(sceneHandle);

#if !NO_DEBUG_LOG_WARNING
            Debug.LogWarning($"The scene[{id}] is not loaded!");
#endif
            return false;
        }

        /// <summary>
        /// TryToUnload an additive scene previously load by this SceneLoader instance
        /// </summary>
        /// <param name="sceneHandle">The SceneHandle generated by a Load method from this SceneLoader instance</param>
        /// <exception cref="Exception">An exception is fire if the SceneHandle is not registered by this SceneLoader instance</exception>
        /// <remarks>If the loaded channel scene is not an additive scene the method do nothing</remarks>
        public async Awaitable<bool> TryToUnload(SceneHandle sceneHandle,bool setBusy=true)
        {
            if (sceneHandle == null) return false;
            var id = sceneHandle.id;
            var sceneEntry = sceneList[id];
            if (!sceneHandle.IsActive)
            {
#if !NO_DEBUG_LOG_WARNING
                Debug.LogWarning($"The scene[{id}] is not activated, unload is not possible!");
#endif
                return false;
            }

            if (sceneHandle.loadSceneMode == LoadSceneMode.Single)
            {
#if !NO_DEBUG_LOG_WARNING
                Debug.LogWarning($"The single scene[{id}]! can't be unload ");
#endif
                return false;
            }

            if (setBusy && isBusy)
            {
                await WaitingWhileIsBusy();
            }

            try
            {
                isBusy = setBusy;
                if (sceneHandle.isAddressable)
                {
                    Addressables.Release(sceneHandle.addressableSceneHandle);
                    await Awaitable.EndOfFrameAsync();
                }
                else
                {
                    var asyncOperation = SceneManager.UnloadSceneAsync(sceneEntry.BuildInSceneName);
                    while (!asyncOperation.isDone)
                    {
                        await Awaitable.EndOfFrameAsync();
                    }
                }
            }
            catch (Exception e)
            {
#if !NO_DEBUG_LOG_EXCEPTION
                Debug.LogException(e);
#endif
                return false;
            }
            finally
            {
                sceneHandlesDic.Remove(id);
                sceneHandleStack.Remove(sceneHandle);
                if (setBusy)
                    isBusy = false;
            }
            onSceneUnloaded.Invoke(id);
            onSceneUnloadedChannel?.DispatchEvent(id);
            return true;
        }


        /// <summary>
        /// Load a scene from a scene entry
        /// </summary>
        /// <param name="sceneEntry">The scene entry</param>
        /// <param name="activateOnLoad">Flag which indicate if the scene will be activated directly after the loading</param>
        /// <param name="progress">The progress action</param>
        /// <param name="priority">The loading priority: see Unity LoadSceneAsync methods</param>
        /// <returns>The SceneHandle of the loaded scene or null if the loading failed.</returns>
        public async Awaitable<SceneHandle> Load(SceneEntry sceneEntry, bool activateOnLoad, Action<float> progress = null, int priority = 100)
        {
            async Awaitable SmoothProgress(float progress, float previousProgress, float delay, Action<float> onProgress = null)
            {
                var delta = progress - previousProgress;
                if (delta > smoothMinStep)
                {
                    while (delta > 0)
                    {
                        delta -= smoothMinStep;
                        previousProgress += smoothMinStep;
                        onSceneLoadingProgress.Invoke(previousProgress);
                        onLoadingProgressChannel?.DispatchEvent(previousProgress);
                        onProgress?.Invoke(previousProgress);
                        await Awaitable.WaitForSecondsAsync(delay);
                    }
                }
            }


            if (isBusy)
            {
                await WaitingWhileIsBusy();
            }

            progress?.Invoke(0);
            await Awaitable.EndOfFrameAsync();

            SceneHandle sceneHandle = null;
            var id = sceneEntry.Id;

            try
            {
                if (sceneHandlesDic.TryGetValue(id, out sceneHandle))
                {
#if !NO_DEBUG_LOG
                    Debug.Log($"Scene[{id}] is already loaded!");
#endif
                    onSceneLoadingProgress.Invoke(1);
                    onLoadingProgressChannel?.DispatchEvent(1);
                    progress?.Invoke(1);

                    //The scene is already loaded
                    //await Awaitable.EndOfFrameAsync();
                    return sceneHandle;
                }

                isBusy = true;

                float totalProgress = 0;

                if (sceneEntry.SceneLocation == SceneEntry.Location.Addressable)
                {
                    var handle = sceneEntry.SceneReference.LoadSceneAsync(sceneEntry.LoadSceneMode, activateOnLoad, priority);
                    while (!handle.IsDone)
                    {
                        var p = handle.PercentComplete;
                        if (smoothProgress)
                        {
                            await SmoothProgress(p, totalProgress, smoothStepDuration, (pp) => progress?.Invoke(pp));
                        }
                        totalProgress = p;
                        onLoadingProgressChannel?.DispatchEvent(totalProgress);
                        onSceneLoadingProgress.Invoke(totalProgress);
                        progress?.Invoke(totalProgress);

                        await Awaitable.EndOfFrameAsync();
                    }

                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        if (totalProgress < 1)
                        {
                            if (smoothProgress)
                                await SmoothProgress(1, totalProgress, smoothStepDuration, (pp) => progress?.Invoke(totalProgress));
                            else
                            {
                                onLoadingProgressChannel?.DispatchEvent(1);
                                onSceneLoadingProgress.Invoke(1);
                                progress?.Invoke(1);
                                await Awaitable.EndOfFrameAsync();
                            }
                        }

                        var sceneInstance = handle.Result;
                        sceneHandle = new SceneHandle(id, sceneEntry.LoadSceneMode, true, handle, activateOnLoad, () => activationPending = false);
                    }

                }
                else
                {
                    if (!activationPending)
                    {
                        var asyncOperation = SceneManager.LoadSceneAsync(sceneEntry.BuildInSceneName, sceneEntry.LoadSceneMode);
                        activationPending = !activateOnLoad;
                        activationPendingSceneId = (activationPending) ? id : "";

                        asyncOperation.allowSceneActivation = activateOnLoad;

                        sceneHandle = new SceneHandle(id, sceneEntry.LoadSceneMode, true, asyncOperation, activateOnLoad, () => activationPending = false);

                        while (!asyncOperation.isDone)
                        {
                            var p = asyncOperation.progress;
                            if (smoothProgress)
                            {
                                await SmoothProgress(p, totalProgress, smoothStepDuration, (pp)=> progress?.Invoke(pp));
                            }
                            totalProgress = p;

                            onLoadingProgressChannel?.DispatchEvent(totalProgress);
                            onSceneLoadingProgress.Invoke(totalProgress);
                            progress?.Invoke(totalProgress);

                            await Awaitable.EndOfFrameAsync();

                            if (!activateOnLoad && totalProgress >= 0.9f)
                            {
                                    onLoadingProgressChannel?.DispatchEvent(1);
                                    onSceneLoadingProgress.Invoke(1);
                                    progress?.Invoke(1);

                                await Awaitable.EndOfFrameAsync();
                                break;
                            }
                        }
                        if (totalProgress < 1)
                        {
                            if (smoothProgress)
                                await SmoothProgress(1, totalProgress, smoothStepDuration, (pp) => progress?.Invoke(pp));
                            else
                            {
                                onLoadingProgressChannel?.DispatchEvent(1);
                                onSceneLoadingProgress.Invoke(1);
                                progress?.Invoke(1);
                                await Awaitable.EndOfFrameAsync();
                            }
                        }
                    }
                    else
                    {
#if !NO_DEBUG_LOG_WARNING
                        Debug.LogWarning($"The buildInScene[{activationPendingSceneId}] is waiting to be activated\n");
#endif
                    }
                }

                if (sceneHandle != null)
                {
                    if (sceneHandle.loadSceneMode == LoadSceneMode.Single)
                    {
                        sceneHandlesDic.Clear();
                    }
                    sceneHandlesDic.Add(id, sceneHandle);
                    sceneHandleStack.Add(sceneHandle);
                }
                else
                {
#if !NO_DEBUG_LOG_WARNING
                    Debug.LogWarning($"Scene idOrAddress[{id}] loading failed!");
#endif
                }

            }
            catch (Exception e)
            {
#if !NO_DEBUG_LOG_EXCEPTION
                Debug.LogException(e);
#endif
            }
            finally
            {
                isBusy = false;  
            }
            if (sceneHandle != null)
            {
                onSceneLoaded.Invoke(id);
                onSceneLoadedChannel?.DispatchEvent(id);
            }
            return sceneHandle;
        }


        /// <summary>
        /// Load a scene from a scene entry
        /// </summary>
        /// <param name="sceneEntry">The scene entry</param>
        /// <param name="activateOnLoad">Flag which indicate if the scene will be activated directly after the loading</param>
        /// <param name="progress">The progress action</param>
        /// <param name="priority">The loading priority: see Unity LoadSceneAsync methods</param>
        /// <returns>The SceneHandle of the loaded scene or null if the loading failed.</returns>
        public async Awaitable<SceneHandle> Load(SceneEntry sceneEntry, Action<float> progress = null, int priority = 100)
        {
            return await Load(sceneEntry, sceneEntry.ActivateAfterLoad,progress, priority);
        }

        /// <summary>
        /// Load a scene from a scene entry index.
        /// </summary>
        /// <param name="index">The index of the registered scene entry</param>
        /// <param name="activateOnLoad">Flag which indicate if the scene will be activated directly after the loading</param>
        /// <param name="progress">The progress action</param>
        /// <param name="priority">The loading priority: see Unity LoadSceneAsync methods</param>
        /// <returns>The SceneHandle of the loaded scene or null if the loading failed.</returns>
        public async Awaitable<SceneHandle> TryToLoad(int index, bool activateOnLoad = false, Action<float> progress = null, int priority = 100)
        {
            var sceneEntry = sceneList[index];
            if (sceneEntry != null)
                return await Load(sceneEntry, activateOnLoad, progress, priority);
#if !NO_DEBUG_LOG_WARNING
            Debug.LogWarning($"No scene entry found at index: {index}");
#endif
            return null;
        }
        /// <summary>
        /// Try to load a scene from a scene entry idOrAddress.
        /// </summary>
        /// <param name="idOrAddress">The id or address of the register scene entry</param>
        /// <param name="activateOnLoad">Flag which indicate if the scene will be activated directly after the loading</param>
        /// <param name="progress">The progress action</param>
        /// <param name="priority">The loading priority: see Unity LoadSceneAsync methods</param>
        /// <returns>The SceneHandle of the loaded scene or null if no scene entry was found from idOrAddress or address</returns>
        public async Awaitable<SceneHandle> TryToLoad(string idOrAddress, bool activateOnLoad = false, Action<float> progress = null, int priority = 100)
        {
            var sceneEntry = sceneList[idOrAddress];

            if (sceneEntry!=null)
                return await Load(sceneEntry, activateOnLoad, progress, priority);
#if !NO_DEBUG_LOG_WARNING
            Debug.LogWarning($"No scene entry found at id or address: {idOrAddress}");
#endif
            return null;
        }
    }

}

