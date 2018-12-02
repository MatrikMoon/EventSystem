using CustomUI.BeatSaber;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

/*
 * Created by Moon on 8/23/2018
 * This is the class for the Modal which would appear when
 * the user doesn't have the mods required for this plugin
 */

namespace DiscordCommunityPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class ModalViewController : VRUIViewController
    {
        public string Message { get; set; } = "Default Text";
        public ModalType Type { get; set; } = ModalType.None;
        public bool DismissImmediately { get; set; } = false;
        public bool DontDismiss { get; set; } = false;
        private Button _backButton;

        public SimpleCallback YesCallback { get; set; } = () => { };
        public SimpleCallback NoCallback { get; set; } = () => { };
        public SimpleCallback OkCallback { get; set; } = () => { };

        public delegate void SimpleCallback();

        public enum ModalType
        {
            YesNo,
            Ok,
            None
        };

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (_backButton == null)
            {
                _backButton = BeatSaberUI.CreateBackButton(rectTransform);
                _backButton.onClick.AddListener(() =>
                {
                    __DismissViewController(null, false); //TODO: Moon... Don't use deprecated things
                });
            }

            Prompt();
        }

        private void Prompt()
        {
            var buttonWidth = 38f;
            var buttonHeight = 10f;
            Button yesButton = null;
            Button noButton = null;
            Button okButton = null;

            TextMeshProUGUI promptText = BeatSaberUI.CreateText(rectTransform,
                Message,
                new Vector2(0f, -30f));
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.fontSize = 5;

            if (Type == ModalType.Ok)
            {
                okButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                okButton.SetButtonText("OK");
                okButton.onClick.AddListener(() =>
                {
                    OkCallback?.Invoke();
                    if (!DontDismiss) __DismissViewController(null, DismissImmediately); //TODO: Moon... Don't use deprecated things
                });
                (okButton.transform as RectTransform).sizeDelta = new Vector2(buttonWidth, buttonHeight);
                (okButton.transform as RectTransform).anchoredPosition = new Vector2((rectTransform.rect.width / 2) - (buttonWidth / 2), 10f);
            }
            else if (Type == ModalType.YesNo)
            {
                yesButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                yesButton.SetButtonText("YES");
                yesButton.onClick.AddListener(() =>
                {
                    YesCallback?.Invoke();
                    if (!DontDismiss) __DismissViewController(null, DismissImmediately); //TODO: Moon... Don't use deprecated things
                });
                (yesButton.transform as RectTransform).sizeDelta = new Vector2(buttonWidth, buttonHeight);
                (yesButton.transform as RectTransform).anchoredPosition = new Vector2((rectTransform.rect.width / 4) - (buttonWidth / 2), 10f);

                noButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                noButton.SetButtonText("NO");
                noButton.onClick.AddListener(() =>
                {
                    NoCallback?.Invoke();
                    if (!DontDismiss) __DismissViewController(null, DismissImmediately); //TODO: Moon... Don't use deprecated things
                });
                (noButton.transform as RectTransform).sizeDelta = new Vector2(buttonWidth, buttonHeight);
                (noButton.transform as RectTransform).anchoredPosition = new Vector2(((rectTransform.rect.width / 4) * 3) - (buttonWidth / 2), 10f);
            }
        }
    }
}
