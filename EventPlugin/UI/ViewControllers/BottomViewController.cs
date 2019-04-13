using System.Reflection;
using VRUI;

/**
 * Created by Moon on 3/21/2019, 11:59pm
 */

namespace EventPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class BottomViewController : VRUIViewController
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
