using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[ExecuteAlways]
public abstract class BaseDataAsset : MonoBehaviour
{
    public System.Action onLoadAllAssets;
    public System.Action onUnloadAllAssets;
    protected int totalStillUnloaded = 0;

    [ClearOnReload(true)]
    private bool bHasStartRun = false;
    [ClearOnReload(true)]
    private bool bHasDisableRun = false;
    [HideInInspector, ClearOnReload(true)]
    public bool fromGhostObject = false;

    protected void OnEnable()
    {
        LoadAllAssets(); // should call the override implementation of the subclasses
    }

    protected void OnDisable()
    {
        UnloadAllAssets(); // should call the override implementation of the subclasses
    }

    //protected void OnEnable()
    //{
    //    if (bHasStartRun)
    //    {
    //        LoadAllAssets(); // should call the override implementation of the subclasses
    //    }
    //}

    //protected void Start()
    //{
    //    bHasStartRun = true;
    //    if (!fromGhostObject)
    //    {
    //        LoadAllAssets();
    //    }
    //}

    //protected void OnDisable()
    //{
    //    if (bHasDisableRun)
    //    {
    //        UnloadAllAssets(); // should call the override implementation of the subclasses
    //    }
    //    bHasDisableRun = true;
    //}

    // LOADING CODE

    protected bool CheckAllLoaded()
    {
        if (totalStillUnloaded <= 0)
        {
            OnAllAssetsLoaded();
            return true;
        }
        return false;
    }
    protected void OnAllAssetsLoaded()
    {
        //Debug.Log("ALL LOADED!!");
        onLoadAllAssets?.Invoke();
    }
    public virtual void LoadAllAssets()
    {
        totalStillUnloaded = 0;
    }
    protected virtual AsyncOperationHandle<T> LoadAssetAsync<T>(AssetReference assetReference) where T : Object
    {
        return Addressables.LoadAssetAsync<T>(assetReference);
    }

    // UNLOADING CODE

    protected bool CheckAllUnloaded()
    {
        if (totalStillUnloaded <= 0)
        {
            OnAllAssetsUnloaded();
            return true;
        }
        return false;
    }
    protected void OnAllAssetsUnloaded()
    {
        onUnloadAllAssets?.Invoke();
    }

    public virtual void UnloadAllAssets()
    {
        
    }

    protected virtual void UnloadAsset(AsyncOperationHandle<UnityEngine.Object> AssetHandle)
    {

    }
    public virtual void OnSave()
    {

    }

}
