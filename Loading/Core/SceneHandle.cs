using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
namespace qb.SceneManagement
{
    /// <summary>
    /// Scene handle container
    /// used to manage loaded scenes and activation if needed
    /// </summary>
    public class SceneHandle
    {
        /// <summary>
        /// Can be the id on the scene entry 
        /// or the address of the scene for those loaded using the method 
        /// LoadScene(string address, LoadSceneMode loadMode,...)
        /// </summary>
        public readonly string id;

        public readonly LoadSceneMode loadSceneMode;

        public readonly bool isFromSceneEntry;

        public AsyncOperation buildInAsyncOperation
        {
            get;
            private set;
        }

        public readonly AsyncOperationHandle<SceneInstance> addressableSceneHandle;
        public bool isAddressable
        {
            get;
            private set;
        }

        bool isActive;
        /// <summary>
        /// Flag to indicate if the channel scene is active
        /// </summary>
        public bool IsActive => isActive;

        float activationProgress;
        public float ActivationProgress=>activationProgress;


        /// <summary>
        /// Activate the channel scene if it's not
        /// </summary>
        /// <param name="progress">Optional progress Action</param>
        /// <returns></returns>
        public async Task ActivateAsync(Action<float> progress = null)
        {
            if (isActive)
            {
#if !NO_DEBUG_LOG_WARNING
                Debug.LogWarning($"The scene[{id}] is already activated");
#endif
                return;
            }
            activationProgress = 0f;    
            if (isAddressable)
            {
                if (addressableSceneHandle.IsValid())
                {
                    //Addressable scene activation
                    buildInAsyncOperation = addressableSceneHandle.Result.ActivateAsync();

                    while (!buildInAsyncOperation.isDone)
                    {
                        activationProgress = buildInAsyncOperation.progress;
                        progress?.Invoke(activationProgress);
                        await Task.Yield();
                    }
                    isActive = true;
                }
                else
                {
#if !NO_DEBUG_LOG_WARNING
                    Debug.LogWarning($"Activation failed!\nThe addressableSceneHandle from scene[{id}] is invalid");
#endif
                }
            }
            else
            {
                buildInAsyncOperation.allowSceneActivation = true;
                while (!buildInAsyncOperation.isDone)
                {
                    activationProgress = (buildInAsyncOperation.progress - 0.9f) * 10;
                    progress?.Invoke(activationProgress);
                    await Task.Yield();
                }
                OnBuildInSceneActivated?.Invoke();
                isActive = true;
            }
        }

        public SceneHandle(string id, LoadSceneMode loadSceneMode, bool isFromSceneEntry, AsyncOperation asyncOperation, bool isActive, Action onBuildInSceneActivated)
        {
            this.id = id;
            this.loadSceneMode = loadSceneMode;
            this.isFromSceneEntry = isFromSceneEntry;
            this.buildInAsyncOperation = asyncOperation;
            this.isActive = isActive;
            this.OnBuildInSceneActivated = onBuildInSceneActivated;
            isAddressable = false;
        }


        public SceneHandle(string id, LoadSceneMode loadSceneMode, bool isFromSceneEntry, AsyncOperationHandle<SceneInstance> addressableSceneHandle, bool isActive, Action onBuildInSceneActivated)
        {
            this.id = id;
            this.loadSceneMode = loadSceneMode;
            this.isFromSceneEntry = isFromSceneEntry;
            this.addressableSceneHandle = addressableSceneHandle;
            this.isActive = isActive;
            this.OnBuildInSceneActivated = onBuildInSceneActivated;
            isAddressable = true;
        }

        private Action OnBuildInSceneActivated;

    }
}
