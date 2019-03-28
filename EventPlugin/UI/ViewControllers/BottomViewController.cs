using CustomUI.BeatSaber;
using CustomUI.UIElements;
using System.Reflection;
using EventPlugin.Misc;
using UnityEngine;
using VRUI;

/**
 * Created by Moon on 3/21/2019, 11:59pm
 */

namespace EventPlugin.UI.ViewControllers
{
    public class BottomViewController : VRUIViewController
    {
        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                
            }
        }
    }
}
