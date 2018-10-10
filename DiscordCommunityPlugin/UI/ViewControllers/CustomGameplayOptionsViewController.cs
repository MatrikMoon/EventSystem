using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = DiscordCommunityShared.Logger;

namespace DiscordCommunityPlugin.UI.ViewControllers
{
    class CustomGameplayOptionsViewController : GameplayOptionsViewController
    {
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            Logger.Info("ACTIVATED");
            base.DidActivate(firstActivation, activationType);
            if (firstActivation)
            {
                _noEnergyToggle.didSwitchEvent += TestAction;
            }
        }

        public virtual void TestAction(HMUI.Toggle toggle, bool isOn)
        {
            Logger.Info("TESTACTION");
            ModalViewController _requiredModsModal = BaseUI.CreateViewController<ModalViewController>();
            _requiredModsModal.Message = "You do not have the following required mods installed:\n" +
            "SongLoaderPlugin\n\n" +
            "DiscordCommunityPlugin will not function.";
            _requiredModsModal.Type = ModalViewController.ModalType.Ok;
            PresentModalViewController(_requiredModsModal, null);
        }
    }
}
