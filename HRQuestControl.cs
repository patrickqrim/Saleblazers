// Copyright (c) Pixel Crushers. All rights reserved.

using PixelCrushers;
using PixelCrushers.QuestMachine;
using UnityEngine;
public class HRQuestControl : QuestControl
{
    public void GiveQuest()
    {
        HRQuestManager.Get.RequestGiveQuest("DEBUG", StringField.GetStringValue(questID));
    }

    public void SetQuestNodeState_Inactive()
    {
        SetQuestNodeState(QuestNodeState.Inactive);
    }

    public void SetQuestNodeState_Active()
    {
        SetQuestNodeState(QuestNodeState.Active);
    }

    public void SetQuestNodeState_True()
    {
        SetQuestNodeState(QuestNodeState.True);
    }

    public void SetQuestState_WaitingToStart()
    {
        SetQuestState(QuestState.WaitingToStart);
    }

    public void SetQuestState_Active()
    {
        SetQuestState(QuestState.Active);
    }

    public void SetQuestState_Successful()
    {
        SetQuestState(QuestState.Successful);
    }

    public void SetQuestState_Disabled()
    {
        SetQuestState(QuestState.Disabled);
    }

    public override void IncrementQuestCounter(int count)
    {
        IncrementQuestCounter_Command(count);
    }

    public void IncrementQuestCounterLocal(int count)
    {
        IncrementQuestCounter_Implementation(count);
    }

    [Mirror.Command(ignoreAuthority = true)]
    public void IncrementQuestCounter_Command(int count)
    {
        IncrementQuestCounter_Implementation(count);
        IncrementQuestCounter_ClientRpc(count);
    }

    public void IncrementQuestCounter_Implementation(int count)
    {
        base.IncrementQuestCounter(count);
    }

    [Mirror.ClientRpc]
    public void IncrementQuestCounter_ClientRpc(int count)
    {
        if (HRNetworkManager.IsHost()) return;

        IncrementQuestCounter_Implementation(count);
    }
}