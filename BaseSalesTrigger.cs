using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using PixelCrushers;

public class BaseSalesTrigger : BaseTrigger
{
    public HRShopManager TargetShop;

    // Tracks a single item ID and its sales in a single period
    public int TargetSales = 5;
    public float TargetMoneySales;
    public int TargetItemID = -1;
    public bool bCheckCustomerType;
    [ShowIf("bCheckCustomerType")]
    public List<string> CustomerTypes;
    public int TargetItemSales = 5;
    public float TargetItemMoneySales;

    int CurrentSales;
    float CurrentMoney;
    int CurrentSalesOfItem;
    float CurrentMoneyFromItem;

    public BaseScriptingEvent SalesCountCompletedEvent;
    public BaseScriptingEvent MoneyCompletedEvent;
    public BaseScriptingEvent ItemCountSoldCompletedEvent;
    public BaseScriptingEvent ItemMoneyCompletedEvent;

    public int NumBartersToTrigger = 0;
    private int CurrentBarters = 0;
    public BaseScriptingEvent BarterEvent;

    public int NumFailsToTrigger = 0;
    private int CurrentFails = 0;
    public BaseScriptingEvent MinigameFailedEvent;

    //public string CounterMessageName;
    //public string CounterParameterName;

    bool bSalesTriggered = false;
    bool bBartersTriggered = false;
    bool bFailsTriggered = false;
    bool bMoneyTriggered = false;
    bool bItemCountTriggered = false;
    bool bItemMoneyTriggered = false;
    bool initialized = false;

    public float EventStartDelay;

    public override void OnStartServer()
    {
        Initialize();
    }


    public override void SetEnabled(bool bEnabled, bool bRemoveFromList = false)
    {
        if (!HRNetworkManager.IsHost())
        {
            return;
        }

        base.SetEnabled(bEnabled, bRemoveFromList);

        if (bEnabled)
        {
            Initialize();
        }
    }

    void Initialize()
    {
        if (!initialized)
        {
            initialized = true;

            if (TargetShop)
            {
                TargetShop.ShopSaleDelegate -= HandleItemSold;
                TargetShop.ShopBarterDelegate -= HandleBarter;
                TargetShop.ShopSaleDelegate += HandleItemSold;
                TargetShop.ShopBarterDelegate += HandleBarter;
            }
        }
    }

    void HandleItemSold(HRShopManager InShopManager, HRSaleInfo InSaleInfo)
    {
        if(this == null)
        {
            return;
        }
        
        if (InSaleInfo.SaleType.Equals(HRSaleType.FAILED))
        {
            // Check if using Minigame
            if (InSaleInfo.bUsedMinigame)
            {
                if (HRNetworkManager.IsHost())
                {
                    HandleItemSoldFailure_Implementation(InShopManager, InSaleInfo);
                }
                else
                {
                    HandleItemSoldFailure_Command(InShopManager, InSaleInfo);
                }
            }
        }
        else
        {
            if (HRNetworkManager.IsHost())
            {
                HandleItemSold_Implementation(InShopManager, InSaleInfo);
            }
            else
            {
                HandleItemSold_Command(InShopManager, InSaleInfo);
            }
        }
    }

    void HandleBarter(HRShopManager InShopManager)
    {
        if (HRNetworkManager.IsHost())
        {
            HandleBarter_Implementation(InShopManager);
        }
        else
        {
            HandleBarter_Command(InShopManager);
        }
    }


    private IEnumerator DelayEvent(UnityEngine.Events.UnityEvent Event, float TimeToWait)
    {
        // TODO: Determine if any player is active in a conversation. This works only for the host.
        yield return new WaitUntil(() => HRDialogueSystem.Get.ConversationCount <= 0);

        yield return new WaitForSeconds(TimeToWait);

        Event.Invoke();
    }

    void HandleItemSold_Implementation(HRShopManager InShopManager, HRSaleInfo SaleInfo)
    {
        CurrentSales++;
        string CustomerType = "";

        if (SaleInfo.Buyer)
        {
            var Customer = SaleInfo.Buyer.GetComponent<HRCustomerAI>();

            if (Customer)
            {
                CustomerType = Customer.CustomerType;
            }
        }
        if ((!bCheckCustomerType || (bCheckCustomerType &&
            (CustomerTypes.Count == 0 || CustomerTypes.Contains("Any") || CustomerTypes.Contains(CustomerType)))))
        {
            HRQuestManager.Get.SaleMade_ClientRpc();
        }

        if(CurrentSales >= TargetSales && (!bCheckCustomerType || (bCheckCustomerType && 
            (CustomerTypes.Count == 0 || CustomerTypes.Contains("Any") || CustomerTypes.Contains(CustomerType)))))
        {
            if(HRNetworkManager.IsHost())
            {
                CompleteSalesCount_Implementation(CurrentSales);
                CompleteSalesCount_ClientRpc(CurrentSales);
            }
        }

        CurrentMoney += SaleInfo.SalePrice;

        if (CurrentMoney >= TargetMoneySales && (!bCheckCustomerType || (bCheckCustomerType &&
            (CustomerTypes.Count == 0 || CustomerTypes.Contains("Any") || CustomerTypes.Contains(CustomerType)))))
        {
            CompleteSalesMoney_ClientRpc(CurrentMoney);
            CompleteSalesMoney_Implementation(CurrentMoney);
        }

        if(TargetItemID != -1 && SaleInfo.Weapon && SaleInfo.Weapon.ItemID == TargetItemID)
        {
            CurrentSalesOfItem += SaleInfo.ItemCount;

            if (CurrentSalesOfItem >= TargetItemSales && (!bCheckCustomerType || (bCheckCustomerType &&
                (CustomerTypes.Count == 0 || CustomerTypes.Contains("Any") || CustomerTypes.Contains(CustomerType)))))
            {
                CompleteItemCountSold_Implementation(CurrentSalesOfItem);
                CompleteItemCountSold_ClientRpc(CurrentSalesOfItem);
            }


            CurrentMoneyFromItem += SaleInfo.SalePrice;

            if (CurrentMoneyFromItem >= TargetItemMoneySales && (!bCheckCustomerType || (bCheckCustomerType &&
                (CustomerTypes.Count == 0 || CustomerTypes.Contains("Any") || CustomerTypes.Contains(CustomerType)))))
            {
                CompleteItemMoneySold_ClientRpc(CurrentMoneyFromItem);
                CompleteItemMoneySold_Implementation(CurrentMoneyFromItem);
            }
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    void HandleItemSold_Command(HRShopManager InShopManager, HRSaleInfo InSaleInfo)
    {
        HandleItemSold_Implementation(InShopManager, InSaleInfo);
    }

    void HandleItemSoldFailure_Implementation(HRShopManager InShopManager, HRSaleInfo SaleInfo)
    {
        CurrentFails++;

        if (CurrentFails >= NumFailsToTrigger)
        {
            FailSalesCount_Implementation(CurrentSales);
            FailSalesCount_ClientRpc(CurrentSales);
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    void HandleItemSoldFailure_Command(HRShopManager InShopManager, HRSaleInfo InSaleInfo)
    {
        HandleItemSoldFailure_Implementation(InShopManager, InSaleInfo);
    }

    void HandleBarter_Implementation(HRShopManager InShopManager)
    {
        CurrentBarters++;

        if (CurrentBarters >= NumBartersToTrigger)
        {
            if (!bBartersTriggered)
            {
                bBartersTriggered = true;

                BarterEvent?.OneShotEvent?.Invoke();
                //StartCoroutine(DelayEvent(BarterEvent.OneShotEvent, EventStartDelay));
            }

            BarterEvent?.RepeatedEvent?.Invoke();
            //StartCoroutine(DelayEvent(BarterEvent.RepeatedEvent, EventStartDelay));

            CompleteSalesCount_ClientRpc(CurrentSales);
        }
    }


    [Mirror.Command(ignoreAuthority = true)]
    void HandleBarter_Command(HRShopManager InShopManager)
    {
        HandleBarter_Implementation(InShopManager);
    }


    [Mirror.ClientRpc]
    public void CompleteSalesCount_ClientRpc(int Sales)
    {
        if (HRNetworkManager.IsHost()) return;

        CompleteSalesCount_Implementation(Sales);
    }

    void CompleteSalesCount_Implementation(int Sales)
    {
        if (Sales >= TargetSales)
        {
            MessageSystem.SendMessage(this, HRQuestMessages.TotalSalesChanged, "TotalSales", Sales);
            if (!bSalesTriggered)
            {
                SalesCountCompletedEvent.OneShotEvent.Invoke();
                bSalesTriggered = true;
            }

            SalesCountCompletedEvent.RepeatedEvent.Invoke();
        }
    }


    [Mirror.ClientRpc]
    public void FailSalesCount_ClientRpc(int Fails)
    {
        if (HRNetworkManager.IsHost()) return;

        FailSalesCount_Implementation(Fails);
    }

    void FailSalesCount_Implementation(int Fails)
    {
        if (Fails >= NumFailsToTrigger)
        {
            if (!bFailsTriggered)
            {
                bFailsTriggered = true;

                MinigameFailedEvent?.OneShotEvent?.Invoke();
                //StartCoroutine(DelayEvent(MinigameFailedEvent.OneShotEvent, EventStartDelay));
            }

            MinigameFailedEvent?.RepeatedEvent?.Invoke();
            //StartCoroutine(DelayEvent(MinigameFailedEvent.RepeatedEvent, EventStartDelay));
        }
    }


    [Mirror.ClientRpc]
    public void HandleBarter_ClientRpc(int Barters)
    {
        if (HRNetworkManager.IsHost()) return;

        HandleBarter_Implementation(Barters);
    }

    void HandleBarter_Implementation(int Barters)
    {
        if (Barters >= NumBartersToTrigger)
        {
            if (!bBartersTriggered)
            {
                bBartersTriggered = true;

                BarterEvent?.OneShotEvent?.Invoke();
                //StartCoroutine(DelayEvent(BarterEvent.OneShotEvent, EventStartDelay));
            }

            BarterEvent?.RepeatedEvent?.Invoke();
            //StartCoroutine(DelayEvent(BarterEvent.RepeatedEvent, EventStartDelay));
        }
    }


    [Mirror.ClientRpc]
    public void CompleteSalesMoney_ClientRpc(float Money)
    {
        if (HRNetworkManager.IsHost()) return;

        CompleteSalesMoney_Implementation(Money);
    }

    void CompleteSalesMoney_Implementation(float Money)
    {
        if (Money >= TargetMoneySales)
        {
            if (!bMoneyTriggered)
            {
                MoneyCompletedEvent.OneShotEvent.Invoke();
                bMoneyTriggered = true;
            }

            MoneyCompletedEvent.RepeatedEvent.Invoke();
        }
    }


    [Mirror.ClientRpc]
    public void CompleteItemCountSold_ClientRpc(int Count)
    {
        if (HRNetworkManager.IsHost()) return;

        CompleteItemCountSold_Implementation(Count);
    }

    void CompleteItemCountSold_Implementation(int Count)
    {
        if (Count >= TargetItemSales)
        {
            if (!bItemCountTriggered)
            {
                ItemCountSoldCompletedEvent.OneShotEvent.Invoke();
                bItemCountTriggered = true;
            }

            ItemCountSoldCompletedEvent.RepeatedEvent.Invoke();
        }
    }


    [Mirror.ClientRpc]
    public void CompleteItemMoneySold_ClientRpc(float Money)
    {
        if (HRNetworkManager.IsHost()) return;

        CompleteItemMoneySold_Implementation(Money);
    }

    public void CompleteItemMoneySold_Implementation(float Money)
    {
        if (Money >= TargetItemMoneySales)
        {
            if (!bItemMoneyTriggered)
            {
                ItemMoneyCompletedEvent.OneShotEvent.Invoke();
                bItemMoneyTriggered = true;
            }

            ItemMoneyCompletedEvent.RepeatedEvent.Invoke();
        }
    }
}
