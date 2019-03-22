using CustomUI.BeatSaber;
using CustomUI.UIElements;
using System.Reflection;
using TeamSaberPlugin.Misc;
using UnityEngine;
using VRUI;

/**
 * Created by Moon on 3/21/2019, 11:59pm
 * View controller designed to be a speed scale
 */

namespace TeamSaberPlugin.UI.ViewControllers
{
    public class SpeedViewController : VRUIViewController
    {
        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                var sliderTransform = CommunityUI._slider.transform as RectTransform;
                sliderTransform.SetParent(rectTransform, false);
                sliderTransform.anchorMin = new Vector2(0.5f, 0.5f);
                sliderTransform.anchorMax = new Vector2(0.5f, 0.5f);
                sliderTransform.anchoredPosition = new Vector2(-20f, 0f);
                sliderTransform.sizeDelta = new Vector2(50, sliderTransform.sizeDelta.y);

                var setButton = BeatSaberUI.CreateUIButton(rectTransform, "QuitButton", buttonText: "SET SPEED");
                setButton.onClick.AddListener(() =>
                {
                    Config.Speed = CommunityUI._slider.GetField<CustomSlider>("_sliderInst").CurrentValue;
                    setButton.SetButtonText($"Speed: {Config.Speed * 100}%");
                });

                var buttonTransform = setButton.transform as RectTransform;
                buttonTransform.anchorMin = new Vector2(0.5f, 0.5f);
                buttonTransform.anchorMax = new Vector2(0.5f, 0.5f);
                buttonTransform.anchoredPosition = new Vector2(0, 10f);
                buttonTransform.sizeDelta = new Vector2(38f, 10f);
            }
        }
    }
}
