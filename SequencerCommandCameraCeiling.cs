using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{

    [AddComponentMenu("")] // Hide from menu.
    public class SequencerCommandCameraCeiling : SequencerCommand
    {

        public HRPlayerCustomizationUI CustomUI = ((HRGameInstance)BaseGameInstance.Get).PlayerCustomization;


        /* Start is called before the first frame update */
        public void Start()
        {
            CustomUI.TurnCameraCeiling();
            Stop();
        }
    }

}