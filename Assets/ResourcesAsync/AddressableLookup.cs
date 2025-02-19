using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Video;

public abstract class AsyncBaseLookup<T> : BaseSingleton<AsyncBaseLookup<T>> where T : UnityEngine.Object
{
    private const string AddressablesFolder = "Assets/ResourcesAsync";
    protected abstract string BaseFolder { get; }
    private string GetContainingFolder() => $"{AddressablesFolder}/{BaseFolder}";

    protected virtual string FileExtensionOverride { get; } = null;
    private string fileExtension;

    protected override void Awake()
    {
        fileExtension = FileExtensionOverride ?? typeof(T) switch {
            Type t when t == typeof(Texture2D) => ".png",       // Could also be .jpg, .tga, etc.
            Type t when t == typeof(AudioClip) => ".wav",       // Could be .mp3, .ogg, etc.
            Type t when t == typeof(TextAsset) => ".txt",       // Could also be .json, .xml, etc.
            Type t when t == typeof(Material) => ".mat",
            Type t when t == typeof(Shader) => ".shader",
            Type t when t == typeof(AnimationClip) => ".anim",
            Type t when t == typeof(Mesh) => ".mesh",
            Type t when t == typeof(GameObject) => ".prefab",   // If it's a saved prefab
            Type t when t == typeof(SceneAsset) => ".unity",
            Type t when t == typeof(VideoClip) => ".mp4",       // Could also be .mov, .avi, etc.
            _ => ".asset",  // Default for unknown assets
        };

        base.Awake();
    }


    private Dictionary<string, T> loadedAssets = new();
    private Dictionary<string, List<Action<T>>> queuedOnLoadActions = new();
    
    /// <summary> Tries to get an addressable asset. </summary>
    /// <param name="assetName"> The name of the asset relative to its base containing folder; eg "Spells/Burst" </param>
    /// <param name="onCompletedCallback"> An optional action to queue for when the object is completed </param>
    /// <returns>
    /// Returns true if the asset was already loaded, calling the action instantly; otherwise, starts loading the asset and returns false
    /// </returns>
    public static bool TryGetAsset(string assetName, Action<T> onCompletedCallback = null) {
        if(Instance.loadedAssets.TryGetValue(assetName, out T preloadedAsset)) {
            onCompletedCallback?.Invoke(preloadedAsset);
            return true;
        } else {
            StartLoadingAsset(assetName, onCompletedCallback);
            return false;
        }
    }


    /// <summary> Starts loading an asset into memory if it hasn't been + isn't being loaded into memory. </summary>
    public static void StartLoadingAsset(string assetName, Action<T> onCompletedCallback = null) {
        if(Instance.queuedOnLoadActions.TryGetValue(assetName, out List<Action<T>> assetBeingWaitedOn)) {
            if(onCompletedCallback != null) {
                assetBeingWaitedOn.Add(onCompletedCallback);
            }
            
            return;
        }

        Instance.queuedOnLoadActions[assetName] = onCompletedCallback == null ? new() : new() { onCompletedCallback };

        string pathToAsset = $"{Instance.GetContainingFolder()}/{assetName}{Instance.fileExtension}";
        Addressables.LoadAssetAsync<T>(pathToAsset).Completed += (asset) => { Instance.AssetFinishedLoading(assetName, asset); };
    }


    private void AssetFinishedLoading(string assetName, AsyncOperationHandle<T> assetLoadHandle) {
        if(assetLoadHandle.Status != AsyncOperationStatus.Succeeded) {
            throw new Exception($"Could not load {assetName}!");
        }

        T asset = assetLoadHandle.Result;
        loadedAssets[assetName] = asset;

        foreach(Action<T> action in queuedOnLoadActions[assetName]) {
            action.Invoke(asset);
        }
    }


    public static void PreloadSubfolder(string subFolder, Action onFinishedCallback = null) {
        string folderToLoad = $"{Instance.GetContainingFolder()}/{subFolder}";

        Addressables.LoadResourceLocationsAsync(folderToLoad).Completed += (locationsHandle) => {
            var locations = locationsHandle.Result;
            int totalCount = locations.Count;
            int loadedCount = 0;
            
            foreach (var location in locations) {
                // Derive the assetName as used in StartLoadingAsset.
                string prefix = $"{AddressablesFolder}/{Instance.BaseFolder}/";
                string assetNameWithExtension = location.InternalId.StartsWith(prefix)
                    ? location.InternalId.Substring(prefix.Length)
                    : location.InternalId;
                
                string assetName = assetNameWithExtension;
                if (assetName.EndsWith(Instance.fileExtension))
                    assetName = assetName.Substring(0, assetName.Length - Instance.fileExtension.Length);
                
                StartLoadingAsset(assetName, asset => {
                    loadedCount++;
                    if (loadedCount == totalCount)
                        onFinishedCallback?.Invoke();
                });
            }
        };
    }
}
