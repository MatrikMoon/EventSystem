using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

namespace DiscordCommunityPlugin.UI
{
    class ModalViewController : VRUINavigationController
    {
        public string Message { get; set; } = "Default Text";
        public ModalType Type { get; set; } = ModalType.None;
        private Button _backButton;
        private bool _continue = false;

        public SimpleCallback YesCallback { get; set; } = () => { };
        public SimpleCallback NoCallback { get; set; } = () => { };
        public SimpleCallback OkCallback { get; set; } = () => { };

        public delegate void SimpleCallback();
        private SimpleCallback action;

        public enum ModalType
        {
            YesNo,
            Ok,
            None
        };

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (_backButton == null)
            {
                _backButton = BaseUI.CreateBackButton(rectTransform);
                _backButton.onClick.AddListener(() =>
                {
                    DismissModalViewController(null, false);
                });
            }

            StartCoroutine(Prompt());
        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            action?.Invoke();
        }

        private IEnumerator Prompt()
        {
            _continue = false;
            var buttonWidth = 38f;
            var buttonHeight = 10f;
            Button yesButton = null;
            Button noButton = null;
            Button okButton = null;

            TextMeshProUGUI promptText = BaseUI.CreateText(rectTransform,
                Message,
                new Vector2(0f, 15f));
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.fontSize = 5;

            Logger.Error("RECT SIZE: " + rectTransform.rect.width + " : " + rectTransform.rect.height);

            if (Type == ModalType.Ok)
            {
                okButton = BaseUI.CreateUIButton(rectTransform, "QuitButton");
                BaseUI.SetButtonText(okButton, "OK");
                okButton.onClick.AddListener(() =>
                {
                    _continue = true;
                    action = OkCallback;
                });
                (okButton.transform as RectTransform).sizeDelta = new Vector2(buttonWidth, buttonHeight);
                (okButton.transform as RectTransform).anchoredPosition = new Vector2((rectTransform.rect.width / 2) - (buttonWidth / 2), 10f);
            }
            else if (Type == ModalType.YesNo)
            {
                yesButton = BaseUI.CreateUIButton(rectTransform, "QuitButton");
                BaseUI.SetButtonText(yesButton, "YES");
                yesButton.onClick.AddListener(() =>
                {
                    _continue = true;
                    action = YesCallback;
                });
                (yesButton.transform as RectTransform).sizeDelta = new Vector2(buttonWidth, buttonHeight);
                (yesButton.transform as RectTransform).anchoredPosition = new Vector2((rectTransform.rect.width / 4) - (buttonWidth / 2), 10f);

                noButton = BaseUI.CreateUIButton(rectTransform, "QuitButton");
                BaseUI.SetButtonText(noButton, "NO");
                noButton.onClick.AddListener(() =>
                {
                    _continue = true;
                    action = NoCallback;
                });
                (noButton.transform as RectTransform).sizeDelta = new Vector2(buttonWidth, buttonHeight);
                (noButton.transform as RectTransform).anchoredPosition = new Vector2(((rectTransform.rect.width / 4) * 3) - (buttonWidth / 2), 10f);
            }

            yield return new WaitUntil(() => _continue);

            if (yesButton != null) Destroy(yesButton);
            if (noButton != null) Destroy(noButton);
            if (okButton != null) Destroy(okButton);
            Destroy(promptText);

            DismissModalViewController(null, true);
        }
    }
}
