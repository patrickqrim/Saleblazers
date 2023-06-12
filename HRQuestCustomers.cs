using BaseScripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PixelCrushers.DialogueSystem;

[System.Serializable]
public struct HRQuestCustomer
{
    public GameObject customerPrefab;
    public float selectionWeight;
    public bool bUnlocked;
    public bool bSpawned;
    public bool bInvited;
    public int initialVisits;
    public BaseScriptingEvent SpawnUnityEvent;
}

[System.Serializable]
public struct HRQuestCustomer_Saved
{
    public bool bUnlocked;
}

[Ceras.SerializedType]
public class HRQuestCustomers : Mirror.NetworkBehaviour, IHRSaveable
{
    public bool questCharacterDebugMode = false;  // FOR DEBUGGING: sets the probability of quest characters spawning to 100%
    public HRQuestCustomer[] questCustomerList;    
    [Ceras.SerializedField]
    public HRQuestCustomer_Saved[] questCustomerSavedList;
    private int numUnlocked;
    private int numSpawned;

    // Probability variables
    public float initProbability = 0.2f;  
    public float maxProbability = 0.4f;  // ceiling for exponential increase
    public float increaseRate = 1.1f;  // exponential increase
    public float customerPenalty = 0.6f;  // exponential penalty (to the power of number of quest characters currently spawned)

    public ByteBitArray testArray = new ByteBitArray(10);
    public int NumUnlocked => numUnlocked;
    public int NumSpawned => numSpawned;

    public int ChooseCustomer()
    {
        float totalWeights = 0f;
        for (int idx = 0; idx < questCustomerList.Length; idx++)
        {
            HRQuestCustomer customer = questCustomerList[idx];
            if (customer.bUnlocked && !customer.bSpawned)
            {
                if (customer.bInvited)  // choose 100% if invited
                {
                    Debug.Log("CHOSE " + idx.ToString());
                    return idx;
                }
                totalWeights += customer.selectionWeight;
            }
        }

        float randomFloat = Random.Range(0f, totalWeights);
        float runningFloat = 0f;
        for (int idx = 0; idx < questCustomerList.Length; idx++)
        {
            HRQuestCustomer customer = questCustomerList[idx];
            if (customer.bUnlocked && !customer.bSpawned)
            {
                runningFloat += customer.selectionWeight;
                if (runningFloat > randomFloat)
                {
                    return idx;
                }
            }
        }
        // Should never get here
        Debug.Log("MATH IS WRONG");
        return 0;
    }

    public GameObject Spawn(int idx)
    {
        if (!questCustomerList[idx].bSpawned)
        {
            questCustomerList[idx].SpawnUnityEvent.FireEvents();
            questCustomerList[idx].bSpawned = true;
            numSpawned++;
        }
        return questCustomerList[idx].customerPrefab;
    }

    public void Despawn(int idx)
    {
        if (questCustomerList[idx].bSpawned)
        {
            questCustomerList[idx].bSpawned = false;
            numSpawned--;
        }
        questCustomerList[idx].bInvited = false;
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void Unlock_Command(int idx)
    {
        Unlock_Implementation(idx);
        Unlock_ClientRpc(idx);
    }

    [Mirror.ClientRpc]
    public void Unlock_ClientRpc(int idx)
    {
        if (HRNetworkManager.IsHost()) return;

        Unlock_Implementation(idx);
    }

    public void Unlock_Implementation(int idx)
    {
        if (!questCustomerList[idx].bUnlocked)
        {
            questCustomerList[idx].bUnlocked = true;
            numUnlocked++;
            questCustomerList[idx].initialVisits = 3;
        }
    }

    public void Unlock(int idx)
    {
        Unlock_Command(idx);
        Debug.Log("UNLOCKED " + idx.ToString());
    }

    public void Unlock(System.Single idx)
    {
        Unlock((int)idx);
    }

    public void Lock(int idx)
    {
        if (questCustomerList[idx].bUnlocked)
        {
            questCustomerList[idx].bUnlocked = false;
            numUnlocked--;
        }
    }

    public void Invite(int idx)
    {
        questCustomerList[idx].bInvited = true;
    }

    public void Invite(System.Single idx)
    {
        Invite((int)idx);
    }

    public int countUnlocked()
    {
        int count = 0;
        foreach (HRQuestCustomer customer in questCustomerList)
        {
            if (customer.bUnlocked)
            {
                count++;
            }
        }
        return count;
    }

    public void RegisterDialogueLua()
    {
        //Debug.LogError("REGISTERING WITH LUA");
        Lua.RegisterFunction("UnlockQuestCustomer", this, SymbolExtensions.GetMethodInfo(() => Unlock((System.Single)0f)));
        Lua.RegisterFunction("InviteQuestCustomer", this, SymbolExtensions.GetMethodInfo(() => Invite((System.Single)0f)));
    }

    public void DeregisterDialogueLua()
    {
        //Debug.LogError("deREGISTERING WITH LUA");
        Lua.UnregisterFunction("UnlockQuestCustomer");
        Lua.UnregisterFunction("InviteQuestCustomer");
    }

    void Start()
    {
        numUnlocked = countUnlocked();

        int countSpawned = 0;
        foreach (HRQuestCustomer customer in questCustomerList)
        {
            if (customer.bSpawned)
            {
                countSpawned++;
            }
        }
        numSpawned = countSpawned;

        RegisterDialogueLua();
    }

    void OnDestroy()
    {
        DeregisterDialogueLua();
    }

    public override void Awake()
    {
        base.Awake();
        
    }

    public void HandleSaveComponentInitialize(HRSaveComponent InSaveComponent, int ComponentID, int AuxIndex)
    {

    }
    public void HandlePreSave()
    {
        questCustomerSavedList = new HRQuestCustomer_Saved[questCustomerList.Length];
        for (int i = 0; i < questCustomerSavedList.Length; i++)
        {
            questCustomerSavedList[i] = new HRQuestCustomer_Saved();
            questCustomerSavedList[i].bUnlocked = questCustomerList[i].bUnlocked;
        }
    }
    public void HandleLoaded()
    {
        for (int i = 0; i < questCustomerList.Length; i++)
        {
            questCustomerList[i].bUnlocked = questCustomerSavedList[i].bUnlocked;
        }
    }
    public void HandleSaved()
    {

    }
    public void HandleReset()
    {

    }
    public bool IsSaveDirty()
    {
        return true;
    }
}