using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HRQuestMessages
{
    ///<summary> The amount a player has of a certain item in their inventory has changed </summary>
    public const string ItemCountChanged = "ItemCountChanged";

    ///<summary> The amount a player has of a certain item in their inventory has changed for GLOBAL QUESTS </summary>
    public const string ItemCountChangedGlobal = "ItemCountChangedGlobal";

    ///<summary> The number of total sales that have been made has changed </summary>
    public const string TotalSalesChanged = "TotalSalesChanged";

    ///<summary> A sale has been made </summary>
    public const string SaleMade = "SaleMade";

    ///<summary> A specific item has been built </summary>
    public const string BuildingItemPlaced = "BuildingItemPlaced";

    ///<summary> A node on the Research Bench Tree has been purchased </summary>
    public const string ResearchNodePurchased = "ResearchNodePurchased";
}
