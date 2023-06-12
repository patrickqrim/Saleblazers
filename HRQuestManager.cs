using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers;
using PixelCrushers.QuestMachine;
using PixelCrushers.QuestMachine.DialogueSystemSupport;
using System;

public class HRQuestManager : Mirror.NetworkBehaviour
{
    public delegate void HRQuestStateChangedSignature(HRQuestManager InManager, string QuestName, string QuestNodeName, string QuestNodeState);
    public HRQuestStateChangedSignature OnQuestStateChangedDelegate;
    public HRQuestStateChangedSignature OnQuestNodeStateChangedDelegate;

    public static HRQuestManager Get;

    QuestJournal GlobalQuestJournal;
    QuestJournal LocalQuestJournal;

    [HideInInspector]
    public string ActiveQuestID;

    [HideInInspector]
    public List<string> ActiveQuestNodeIDs = new List<string>();

    bool _bInitialized = false;
    bool _bBoundDelegates = false;


    public bool bDebugLogs = true;

    public override void Awake()
    {
        base.Awake();
        if (!Get)
        {
            Get = this;
        }
        if (BaseGameInstance.Get)
        {
            GlobalQuestJournal = ((HRGameInstance)BaseGameInstance.Get).GlobalQuestJournal;
            LocalQuestJournal = ((HRGameInstance)BaseGameInstance.Get).QuestJournal;
        }
        else
        {
            StartCoroutine(GameInstanceStartCoroutine());
        }
    }

    private IEnumerator GameInstanceStartCoroutine()
    {
        yield return new WaitUntil(() => BaseGameInstance.Get != null);
        GlobalQuestJournal = ((HRGameInstance)BaseGameInstance.Get).GlobalQuestJournal;
        LocalQuestJournal = ((HRGameInstance)BaseGameInstance.Get).QuestJournal;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(WaitForDialogue());
    }

    private void OnStartClient_Implementation()
    {
        // Listen for quest and give the correct HRRewardStruct
        // End the quest 
        // Get the player journal
        BindDelegates(true);

        if (!HRNetworkManager.IsHost())
        {
            StartCoroutine(WaitForOwningPlayer()); // Redraw the hud after time
        }
        _bInitialized = true;
    }

    private IEnumerator WaitForDialogue()
    {
        yield return new WaitForSeconds(1f);
        OnStartClient_Implementation();
    }

    private IEnumerator WaitForOwningPlayer()
    {
        // We have to wait for the LocalPlayerController reference to be assigned
        yield return new WaitUntil(() => HRGameInstance.Get != null);

        HRGameInstance instance = ((HRGameInstance)HRGameInstance.Get);
        instance.QuestHUDUI.Repaint(instance.GlobalQuestJournal, instance.QuestJournal);
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void SyncToHostQuestSave_Command(BaseScripts.BasePlayerController requester, string PlayerID, string Data)
    {
        if (GlobalQuestJournal)
        {
            SavedGameData data = SaveSystem.RecordSavedGameData();

            List<string> ClearedQuests = new List<string>();

            foreach (var key in ((HRGameInstance)BaseGameInstance.Get).GlobalQuestJournal.QuestPlayers.Keys)
            {
                if (((HRGameInstance)BaseGameInstance.Get).GlobalQuestJournal.QuestPlayers[key].Contains(PlayerID))
                {
                    ClearedQuests.Add(key);
                }
            }

            HRDialogueSystemSave.Get.ApplyPlayerQuestJournalData(PlayerID, Data);
            string LocalData = HRDialogueSystemSave.Get.GetLocalQuestData(PlayerID);

            ApplyHostGlobalData_TargetRpc(requester.connectionToClient, data.GetData(GlobalQuestJournal.key), ClearedQuests, LocalData);
        }
    }

    [Mirror.TargetRpc]
    private void ApplyHostGlobalData_TargetRpc(Mirror.NetworkConnection target, string data, List<string> CompletedQuests, string LocalQuestSave)
    {
        if (DialogueManager.isConversationActive)
        {
            Debug.LogError("[HRDialogueSystemSave] Couldn't apply host data due to active conversation!");
        }
        else
        {
            HRDialogueSystemSave.Get.ApplyHostGlobalData(data, CompletedQuests, LocalQuestSave);
        }
    }

    private Quest GetQuest(string questGiverID, string questID, out QuestGiver giver)
    {
        giver = ((HRGameInstance)BaseGameInstance.Get).QuestMachineBridge.GetQuestGiver(questGiverID);
        if (giver == null)
        {
            if (DialogueDebug.LogWarnings) Debug.LogWarning("Dialogue System: Can't find quest giver '" + questGiverID + "' to give quest '" + questID + "'.", this);
            return null;
        }
        Quest quest = giver.FindQuest(questID);
        if (quest == null)
        {
            if (DialogueDebug.LogWarnings) Debug.LogWarning("Dialogue System: Quest giver '" + questGiverID + "' doesn't have quest '" + questID + "' to give.", this);
            return null;
        }
        return quest;
    }

    [Mirror.Command]
    public void Repaint_Command()
    {
        Repaint_Implementation();
        Repaint_ClientRpc();
    }

    [Mirror.ClientRpc]
    public void Repaint_ClientRpc()
    {
        if (HRNetworkManager.IsHost()) return;

        Repaint_Implementation();
    }

    public void Repaint_Implementation()
    {
        HRGameInstance instance = ((HRGameInstance)HRGameInstance.Get);
        instance.QuestHUDUI.Repaint(instance.GlobalQuestJournal, instance.QuestJournal);
    }


    void HandleQuestStateChanged(Quest InQuest)
    {
        //TODO: make the host send an rpc to sync all the client quest states?
        //Repaint_Command();

        OnQuestStateChangedDelegate?.Invoke(this, InQuest.id.value, "", InQuest.GetState().ToString());
        ActiveQuestNodeIDs.Clear();
        if (InQuest.GetState().ToString() == PixelCrushers.QuestMachine.QuestState.Active.ToString())
        {
            HRNotification NewNotification = new HRNotification();
            NewNotification.Duration = 5.0f;
            NewNotification.Messages = new string[2] { InQuest.title.value, "QUEST ACCEPTED" };

            ((HRGameInstance)BaseGameInstance.Get).QuestNotificationSystem.ClearAllNotifications();
            ((HRGameInstance)BaseGameInstance.Get).QuestNotificationSystem.AddNotification(NewNotification);

            //Show this quest in the hud if there isn't one currently
            //Otherwise turn off this bool in case it was enabled by default
            if (ActiveQuestID == "" || ActiveQuestID == InQuest.id.value)
            {
                InQuest.showInTrackHUD = true;
            }
            else
            {
                InQuest.showInTrackHUD = false;
            }
        }
        else if (InQuest.GetState().ToString() == PixelCrushers.QuestMachine.QuestState.Successful.ToString())
        {
            HRNotification NewNotification = new HRNotification();
            NewNotification.Duration = 5.0f;
            NewNotification.Messages = new string[2] { InQuest.title.value, "QUEST COMPLETED" };

            ((HRGameInstance)BaseGameInstance.Get).QuestNotificationSystem.ClearAllNotifications();
            ((HRGameInstance)BaseGameInstance.Get).QuestNotificationSystem.AddNotification(NewNotification);
            ActiveQuestID = "";
            // Save the completed quest state so we can add rewards for late joiners
            if (HRNetworkManager.IsHost())
            {
                if (InQuest.isGlobal)
                {
                    ((HRGameInstance)BaseGameInstance.Get).GlobalQuestJournal.AddPlayerToQuest(
                        StringField.GetStringValue(InQuest.id),
                        HRSaveSystem.Get.GetLocalPlayerID());
                }
                else
                {
                    ((HRGameInstance)BaseGameInstance.Get).QuestJournal.AddPlayerToQuest(
                        StringField.GetStringValue(InQuest.id),
                        HRSaveSystem.Get.GetLocalPlayerID());
                }
            }
            else
            {
                if (InQuest.isGlobal)
                {
                    OnGlobalQuestCompleted_Command(StringField.GetStringValue(InQuest.id),
                        HRSaveSystem.Get.GetLocalPlayerID());
                }
            }
        }

        string QuestyData = ((HRGameInstance)BaseGameInstance.Get).QuestJournal.RecordData();
        string StaticQuest = ((HRGameInstance)BaseGameInstance.Get).GlobalQuestJournal.RecordData();

        if (!InQuest.isGlobal)
        {
            // Send the current state of this player's local journal to the host
            string QuestData = ((HRGameInstance)BaseGameInstance.Get).QuestJournal.RecordData();

            if (!HRNetworkManager.IsHost())
            {
                SaveLocalQuestData_Command(QuestData, HRSaveSystem.Get.GetLocalPlayerID());
            }
            else
            {
                HRDialogueSystemSave.Get.SavePlayerQuestJournals(HRSaveSystem.Get.CurrentFileInstance);
            }
        }


    }

    private void HandleTrackedQuestChanged(Quest quest)
    {
        ActiveQuestID = quest.id.value;
    }

    #region Give Quest
    public void RequestGiveQuest(string questGiverID, string questID)
    {
        QuestGiver giver;
        Quest quest = GetQuest(questGiverID, questID, out giver);
        if (quest != null)
        {
            if (quest.isGlobal)
            {
                GiveGlobalQuest(questGiverID, questID);
            }
            else if(!LocalQuestJournal.ContainsQuest(questID))
            {
                GiveQuest_Implementation(questGiverID, questID, false);
            }
        }
    }

    public Quest GetQuestInstance(string questID)
    {
        var quester = QuestMachineConfiguration.instance.GlobalQuestJournal;
        return (quester != null) ? quester.FindQuest(questID) : QuestMachine.GetQuestInstance(questID, StringField.GetStringValue(quester.id));
    }

    public PixelCrushers.QuestMachine.QuestState GetQuestState(string questID)
    {
        var questInstance = GetQuestInstance(questID);
        if (questInstance != null)
        {
            return questInstance.GetState();
        }
        return PixelCrushers.QuestMachine.QuestState.WaitingToStart;
    }

    private void GiveGlobalQuest(string questGiverID, string questID)
    {
        if (HRNetworkManager.IsHost())
        {
            GiveGlobalQuest_Server(questGiverID, questID);
        }
        else
        {
            GiveGlobalQuest_Command(questGiverID, questID);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void GiveGlobalQuest_Command(string questGiverID, string questID)
    {
        GiveGlobalQuest_Server(questGiverID, questID);
    }

    private void GiveGlobalQuest_Server(string questGiverID, string questID)
    {
        if (GlobalQuestJournal.ContainsQuest(questID))
        {
            return;
        }
        if (netIdentity)
        {
            GiveQuest_ClientRpc(questGiverID, questID, true);
        }
        else
        {
            GiveQuest_Implementation(questGiverID, questID, true);
        }
    }

    [Mirror.ClientRpc]
    private void GiveQuest_ClientRpc(string questGiverID, string questID, bool bIsGlobal)
    {
        GiveQuest_Implementation(questGiverID, questID, bIsGlobal);
    }

    private void GiveQuest_Implementation(string questGiverID, string questID, bool bIsGlobal)
    {
        if (!_bInitialized)
        {
            return;
        }
        QuestGiver giver;
        Quest quest = GetQuest(questGiverID, questID, out giver);
        if (quest != null)
        {
            QuestJournal quester = null;
            if (bIsGlobal)
            {
                quester = GlobalQuestJournal;
            }
            else
            {
                quester = LocalQuestJournal;
            }

            giver.GiveQuestToQuester(quest, quester);
            ((HRGameInstance)BaseGameInstance.Get).QuestMachineBridge.SetConversationQuesterID(quester);
            if (HRQuestJournalUI.Instance)
            {
                HRQuestJournalUI.Instance.hudFocusedQuestID = quest.id.ToString();
                StartCoroutine(DelayedInvokeRepaint()); //Repaint again to show as selected quest
            }

            if (bDebugLogs)
            {
                Debug.Log("Quest given: " + quest.id + " by " + quest.questGiverID + ".");
            }
        }
    }

    IEnumerator DelayedInvokeRepaint()
    {
        yield return new WaitForSeconds(0.1f);
        yield return new WaitForEndOfFrame();
        Repaint_Implementation();
    }
    #endregion

    #region Set Quest State
    public void RequestSetQuestState(string questID, string state, string questerID)
    {

    }
    #endregion

    #region Set Quest Node State

    public void CompleteActiveQuestNodes()
    {
        if (ActiveQuestID == "") return;
        for (int i = 0; i < ActiveQuestNodeIDs.Count; i++)
        {
            RequestSetQuestNodeState(ActiveQuestID, ActiveQuestNodeIDs[i], "success", "");
        }
    }
    public void RequestSetQuestNodeState(string questID, string questNodeID, string state, string questerID)
    {
        Quest questInstance = ((HRGameInstance)BaseGameInstance.Get).QuestMachineBridge.GetQuestInstance(questID, questerID);
        if (questInstance != null)
        {
            if (questInstance.isGlobal)
            {
                SetGlobalQuestNodeState(questID, questNodeID, state, questerID);
            }
            else
            {
                SetQuestNodeState_Implementation(questID, questNodeID, state, questerID);
            }
        }
    }

    private void SetGlobalQuestNodeState(string questID, string questNodeID, string state, string questerID)
    {
        if (questerID == "")
        {
            questerID = GlobalQuestJournal.id.value;
        }
        if (HRNetworkManager.IsHost())
        {
            if (netIdentity)
            {
                SetQuestNodeState_ClientRpc(questID, questNodeID, state, questerID);
            }
            else
            {
                SetQuestNodeState_Implementation(questID, questNodeID, state, questerID);
            }
        }
        else
        {
            SetGlobalQuestNodeState_Command(questID, questNodeID, state, questerID);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SetGlobalQuestNodeState_Command(string questID, string questNodeID, string state, string questerID)
    {
        SetQuestNodeState_ClientRpc(questID, questNodeID, state, questerID);
    }

    [Mirror.ClientRpc]
    private void SetQuestNodeState_ClientRpc(string questID, string questNodeID, string state, string questerID)
    {
        SetQuestNodeState_Implementation(questID, questNodeID, state, questerID);
    }

    private void SetQuestNodeState_Implementation(string questID, string questNodeID, string state, string questerID)
    {
        if (!_bInitialized)
        {
            return;
        }
        Quest questInstance = ((HRGameInstance)BaseGameInstance.Get).QuestMachineBridge.GetQuestInstance(questID, questerID);
        if (questInstance != null)
        {
            QuestNode node = questInstance.GetNode(questNodeID);

            if (node != null)
            {
                var prevState = node.GetState();
                var newState = string.Equals(state, QuestLog.StateToString(PixelCrushers.DialogueSystem.QuestState.Active)) ? QuestNodeState.Active
                : string.Equals(state, QuestLog.StateToString(PixelCrushers.DialogueSystem.QuestState.Success)) ? QuestNodeState.True
                : QuestNodeState.Inactive;
                node.SetState(newState);
                if (bDebugLogs)
                {
                    Debug.Log($"Quest node {node.id} updated from {prevState} to {newState}.");
                }
            }
        }
    }

    #endregion

    public string GetQuestNodeState(string questID, string questNodeID, string questerID)
    {
        if (string.IsNullOrEmpty(questerID))
        {
            if (GlobalQuestJournal.ContainsQuest(questID))
            {
                questerID = GlobalQuestJournal.id.value;
            }
        }
        var questInstance = ((HRGameInstance)BaseGameInstance.Get).QuestMachineBridge.GetQuestInstance(questID, questerID);
        if (questInstance == null)
        {
            return QuestLog.StateToString(PixelCrushers.DialogueSystem.QuestState.Unassigned);
        }
        var node = questInstance.GetNode(questNodeID);
        if (node == null)
        {
            if (DialogueDebug.LogWarnings) Debug.LogWarning("Dialogue System: Quest Machine quest with ID '" + questID + "' doesn't have a node '" + questNodeID + "'. Can't get its state.", this);
            return QuestLog.StateToString(PixelCrushers.DialogueSystem.QuestState.Unassigned);
        }
        switch (node.GetState())
        {
            default:
            case QuestNodeState.Inactive:
                return QuestLog.StateToString(PixelCrushers.DialogueSystem.QuestState.Unassigned);
            case QuestNodeState.Active:
                return QuestLog.StateToString(PixelCrushers.DialogueSystem.QuestState.Active);
            case QuestNodeState.True:
                return QuestLog.StateToString(PixelCrushers.DialogueSystem.QuestState.Success);
        }
    }

    void HandleQuestNodeStateChanged(QuestNode InQuestNode)
    {
        Quest ResultingQuest = InQuestNode.quest;

        // Node
        // Debug.Log("Node " + messageArgs.values[0] + " changed in Quest " + messageArgs.parameter);
        // Get the node's information from the quest database
        if (InQuestNode.nodeType != QuestNodeType.Start && (InQuestNode.quest.GetState() == PixelCrushers.QuestMachine.QuestState.Active ||
            InQuestNode.quest.GetState() == PixelCrushers.QuestMachine.QuestState.Successful))
        {
            if (InQuestNode.GetState() == PixelCrushers.QuestMachine.QuestNodeState.True)
            {
                GiveRewards(InQuestNode.reward);
            }

            HRNotification NewNotification = new HRNotification();
            NewNotification.Duration = 5.0f;
            NewNotification.Messages = new string[2] { ResultingQuest.title.value, "QUEST UPDATED" };

            ((HRGameInstance)BaseGameInstance.Get).QuestNotificationSystem.AddNotification(NewNotification);
        }

        //Add or remove node from list
        if (InQuestNode.nodeType != QuestNodeType.Start)
        {
            if (InQuestNode.GetState() == QuestNodeState.Active)
            {
                ActiveQuestNodeIDs.Add(InQuestNode.id.value);
            }
            else
            {
                ActiveQuestNodeIDs.Remove(InQuestNode.id.value);
            }
        }

        OnQuestNodeStateChangedDelegate?.Invoke(this, ResultingQuest.id.value, InQuestNode.id.ToString(), InQuestNode.GetState().ToString());
        if (!HRNetworkManager.IsHost())
        {
            SaveLocalQuestData_Command(((HRGameInstance)BaseGameInstance.Get).QuestJournal.RecordData(), HRSaveSystem.Get.GetLocalPlayerID());
        }
        else
        {
            HRDialogueSystemSave.Get.SavePlayerQuestJournals(HRSaveSystem.Get.CurrentFileInstance);
        }
    }

    //KNOWLEDGE

    /// <summary>
    /// Use a text table to reference a knowledge text field ID and use this to unlock 
    /// </summary>
    public void UnlockKnowledge(string questId, string questNodeId, int knowledgeTextFieldId)
    {
        Quest quest = GetQuestInstance(questId);
        if (quest == null) { Debug.LogError("QuestManager - UnlockKnowledge - Quest cannot be found"); return; }
        QuestNode node = quest.GetNode(questNodeId);
        if (node == null) { Debug.LogError("QuestManager - UnlockKnowledge - Quest Node cannot be found"); return; }
        node.UnlockQuestKnowledge(knowledgeTextFieldId);
    }

    //REWARDS

    public void GiveRewards(HRRewardStruct InRewardStruct)
    {
        if (!InRewardStruct.HasRewards()) return;
        // Must be delayed to account for issues with loading.
        StartCoroutine(GiveRewards_Delayed(InRewardStruct));
    }

    private IEnumerator GiveRewards_Delayed(HRRewardStruct InRewardStruct)
    {
        HRGameInstance GameInstance = ((HRGameInstance)BaseGameInstance.Get);

        yield return new WaitUntil(() => !HRSaveSystem.Get.bIsLoadingPlayer && BaseGameInstance.Get.GetFirstPawn() != null && GameInstance.PersistentPlayerWallet != null);

        HeroPlayerCharacter PC = BaseGameInstance.Get.GetFirstPawn() as HeroPlayerCharacter;

        // Add money
        if (InRewardStruct.MoneyReward > 0)
        {
            GameInstance.PersistentPlayerWallet.AddMoney(InRewardStruct.MoneyReward);
            ((HRGameInstance)BaseGameInstance.Get).TextDamageManager.Add("+$" + InRewardStruct.MoneyReward, PC.transform, "addmoney");
        }

        // Add XP
        if (InRewardStruct.XPReward > 0)
        {
            PC.XPComponent.AddHP(InRewardStruct.XPReward, null);
        }

        //Give Items
        SpawnItemRewards(InRewardStruct, PC.HRPC);

        //Give Recipes
        GiveRecipeRewards(InRewardStruct);
    }

    void GiveRecipeRewards(HRRewardStruct InRewardStruct)
    {
        var Encyclopedia = ((HRGameInstance)BaseGameInstance.Get).EncyclopediaSystem;
        HRPlayerUI PlayerUI = ((HRPlayerController)BaseGameInstance.Get.GetFirstPawn().PlayerController).PlayerUI;

        for (int i = 0; i < InRewardStruct.ItemRecipeRewards.Length; i++)
        {
            Encyclopedia.SetUnlocked(((HRItemSO)InRewardStruct.ItemRecipeRewards[i]).GetItemId(), true);
        }

        // This is so bad. This UI needs to bind to the recipe system instead of doing this badness, but I don't have time to fix this.
        if (PlayerUI && PlayerUI.RecipeBookUI.NewRecipesAvailable)
        {
            PlayerUI.RecipeBookUI.OpenUnlockedRecipeUI();
        }
    }

    HRRewardStruct.HRServerItemReward[] ConvertItemRewards(HRRewardStruct InRewardStruct)
    {
        HRRewardStruct.HRServerItemReward[] ServerItemRewards = new HRRewardStruct.HRServerItemReward[InRewardStruct.ItemRewards.Length];
        for (int i = 0; i < InRewardStruct.ItemRewards.Length; ++i)
        {
            if (((HRItemSO)InRewardStruct.ItemRewards[i].ItemReference))
            {
                ServerItemRewards[i].ItemId = ((HRItemSO)InRewardStruct.ItemRewards[i].ItemReference).GetItemId();
                ServerItemRewards[i].ItemCount = InRewardStruct.ItemRewards[i].ItemCount;
            }
        }
        return ServerItemRewards;
    }

    private void SpawnItemRewards(HRRewardStruct InRewardStruct, BaseScripts.BasePlayerController PlayerController)
    {
        HRRewardStruct.HRServerItemReward[] ItemRewards = ConvertItemRewards(InRewardStruct);
        if (HRNetworkManager.IsHost())
        {
            SpawnItemRewards_Server(ItemRewards, PlayerController);
        }
        else
        {
            SpawnItemRewards_Command(ItemRewards, PlayerController);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void SpawnItemRewards_Command(HRRewardStruct.HRServerItemReward[] InRewards, BaseScripts.BasePlayerController PlayerController)
    {
        SpawnItemRewards_Server(InRewards, PlayerController);
    }

    private void SpawnItemRewards_Server(HRRewardStruct.HRServerItemReward[] InRewards, BaseScripts.BasePlayerController PlayerController)
    {
        for (int i = 0; i < InRewards.Length; ++i)
        {
            ((HeroPlayerCharacter)PlayerController.PlayerPawn).WeaponManager.AddWeaponByID(InRewards[i].ItemId, InRewards[i].ItemCount);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void OnGlobalQuestCompleted_Command(string QuestID, string PlayerID)
    {
        ((HRGameInstance)BaseGameInstance.Get).GlobalQuestJournal.AddPlayerToQuest(QuestID, PlayerID);
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void OnLocalQuestCompleted_Command(string QuestID, string PlayerID)
    {
        ((HRGameInstance)BaseGameInstance.Get).QuestJournal.AddPlayerToQuest(QuestID, PlayerID);
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void OnLocalQuestJournalUpdated_Command(string QuestData, string PlayerID)
    {
        HRDialogueSystemSave.Get.AddLocalQuestData(QuestData, PlayerID);
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void RequestLocalQuestData_Command(HRPlayerController requester, string SteamID, long CharacterUniqueID)
    {
        string ID = HRSaveSystem.Get.GetCharacterPlayerID(SteamID, CharacterUniqueID);
        string data = HRDialogueSystemSave.Get.GetLocalQuestData(ID);

        ReceiveLocalQuestData_TargetRpc(requester.connectionToClient, ID, data);
    }


    [Mirror.TargetRpc]
    private void ReceiveLocalQuestData_TargetRpc(Mirror.NetworkConnection target, string ID, string data)
    {
        HRDialogueSystemSave.Get.ApplyPlayerQuestJournalData(ID, data); // Kenny Change 104
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void SaveLocalQuestData_Command(string data, string ID)
    {
        HRDialogueSystemSave.Get.SavePlayerQuestJournals_ByID(ID, data);
    }

    private void OnDestroy()
    {
        BindDelegates(false);
    }

    void BindDelegates(bool bBind)
    {
        if (bBind)
        {
            if (!_bBoundDelegates)
            {
                QuestJournal PlayerJournal = ((HRGameInstance)BaseGameInstance.Get).QuestJournal;
                if (PlayerJournal)
                {
                    PlayerJournal.questStateChanged += HandleQuestStateChanged;
                    PlayerJournal.questNodeStateChanged += HandleQuestNodeStateChanged;
                    PlayerJournal.trackedQuestChanged += HandleTrackedQuestChanged;
                }
                if (GlobalQuestJournal)
                {
                    GlobalQuestJournal.questStateChanged += HandleQuestStateChanged;
                    GlobalQuestJournal.questNodeStateChanged += HandleQuestNodeStateChanged;
                    GlobalQuestJournal.trackedQuestChanged += HandleTrackedQuestChanged;
                }

                _bBoundDelegates = true;
            }
        }
        else
        {
            if (_bBoundDelegates)
            {
                QuestJournal PlayerJournal = ((HRGameInstance)BaseGameInstance.Get).QuestJournal;
                if (PlayerJournal)
                {
                    PlayerJournal.questStateChanged -= HandleQuestStateChanged;
                    PlayerJournal.questNodeStateChanged -= HandleQuestNodeStateChanged;
                    PlayerJournal.trackedQuestChanged -= HandleTrackedQuestChanged;
                }
                if (GlobalQuestJournal)
                {
                    GlobalQuestJournal.questStateChanged -= HandleQuestStateChanged;
                    GlobalQuestJournal.questNodeStateChanged -= HandleQuestNodeStateChanged;
                    GlobalQuestJournal.trackedQuestChanged -= HandleTrackedQuestChanged;
                }

                _bBoundDelegates = false;
            }
        }
    }

    // Sales trigger (because this client rpc needs to be called from a manager that always exists for everyone)
    [Mirror.ClientRpc]
    public void SaleMade_ClientRpc()
    {
        MessageSystem.SendMessage(this, HRQuestMessages.SaleMade, "", 1);
    }
}
