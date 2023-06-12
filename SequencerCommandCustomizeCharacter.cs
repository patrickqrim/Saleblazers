using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{

    [AddComponentMenu("")] // Hide from menu.
    public class SequencerCommandCustomizeCharacter : SequencerCommand
    {

        public HRPlayerCustomizationUI CustomUI = ((HRGameInstance)BaseGameInstance.Get).PlayerCustomization;


        /* Start is called before the first frame update */
        public void Start()
        {
            CustomUI.StartCustomization(fromSeqCom: true);
        }

        public void Update()
        {
            if (!CustomUI.IsCustomizing())
            { 
                // Close the selection window, which is not relevant to customization
                CustomUI.SelectionPopup.gameObject.SetActive(false);
                Stop();
            }
        }
    }

}