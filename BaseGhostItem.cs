using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseGhostItem : MonoBehaviour
{
    public bool bCanPlace = true;
    public bool bShopPlot = false;
    public HRShopPlot plotRef;

    // References to child objects
    [Header("Child Object References")]
    public MeshRenderer meshRenderer;
    public BoxCollider placementCollider;
    public BaseItemPlacementCollision placementCollision;
    public BoxCollider boxCollider;
}
