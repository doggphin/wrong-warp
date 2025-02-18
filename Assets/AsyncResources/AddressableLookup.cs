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
    private const string AddressablesFolder = "Assets/AsyncResources";
    protected abstract string BaseFolder { get; }

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
    /// <param name="actionOnCompleted"> An optional action to queue for when the object is completed </param>
    /// <returns>
    /// Returns true if the asset was already loaded, calling the action instantly; otherwise, starts loading the asset and returns false
    /// </returns>
    public static bool TryGetAsset(string assetName, Action<T> actionOnCompleted = null) {
        if(Instance.loadedAssets.TryGetValue(assetName, out T preloadedAsset)) {
            actionOnCompleted?.Invoke(preloadedAsset);
            return true;
        } else {
            StartLoadingAsset(assetName, actionOnCompleted);
            return false;
        }
    }

    /// <summary> Starts loading an asset into memory if it hasn't been + isn't being loaded into memory. </summary>
    public static void StartLoadingAsset(string assetName, Action<T> actionOnCompleted = null) {
        if(Instance.queuedOnLoadActions.TryGetValue(assetName, out List<Action<T>> assetBeingWaitedOn)) {
            if(actionOnCompleted != null) {
                assetBeingWaitedOn.Add(actionOnCompleted);
            }
            
            return;
        }

        Instance.queuedOnLoadActions[assetName] = actionOnCompleted == null ? new() : new() { actionOnCompleted };

        string pathToAsset = $"{AddressablesFolder}/{Instance.BaseFolder}/{assetName}{Instance.fileExtension}";
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
}
