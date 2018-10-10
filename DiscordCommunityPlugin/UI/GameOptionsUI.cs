using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = DiscordCommunityShared.Logger;

namespace DiscordCommunityPlugin.UI
{
    abstract class GameOption
    {
        public GameObject gameObject;
        public string optionName;
        public abstract void Instantiate();
    }

    class ToggleOption : GameOption
    {
        public event Action<bool> OnToggle;

        public ToggleOption(string optionName)
        {
            this.optionName = optionName;
        }

        public override void Instantiate()
        {
            //We have to find our own target
            //TODO: Clean up time complexity issue. This is called for each new option
            StandardLevelDetailViewController _sldvc = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
            GameplayOptionsViewController _govc = _sldvc.GetField<GameplayOptionsViewController>("_gameplayOptionsViewController");
            RectTransform container = (RectTransform)_govc.transform.Find("Switches").Find("Container");

            //TODO: Can probably slim this down a bit
            gameObject = UnityEngine.Object.Instantiate(container.Find("NoEnergy").gameObject, container);
            gameObject.GetComponentInChildren<TextMeshProUGUI>().text = optionName;
            gameObject.name = optionName;
            gameObject.layer = container.gameObject.layer;
            gameObject.transform.parent = container;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localScale = Vector3.one;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.SetActive(false); //All options start disabled

            gameObject.GetComponentInChildren<HMUI.Toggle>().didSwitchEvent += (_, e) => { OnToggle?.Invoke(e); };
        }
    }

    class MultiSelectOption
    {

    }

    class GameOptionsUI
    {
        //Holds all the 
        private static IList<GameOption> customOptions = new List<GameOption>();

        public static ToggleOption CreateToggleOption(string optionName)
        {
            ToggleOption ret = new ToggleOption(optionName);
            customOptions.Add(ret);
            return ret;
        }

        public static void Build()
        {
            //Grab necessary references
            StandardLevelDetailViewController _sldvc = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
            GameplayOptionsViewController _govc = _sldvc.GetField<GameplayOptionsViewController>("_gameplayOptionsViewController");

            //Get reference to the switch container
            RectTransform container = (RectTransform)_govc.transform.Find("Switches").Find("Container");
            container.sizeDelta = new Vector2(container.sizeDelta.x, container.sizeDelta.y + 7f); //Grow container so it aligns properly with text

            //Get references to the original switches, so we can later duplicate then destroy them
            Transform noEnergyOriginal = container.Find("NoEnergy");
            Transform noObstaclesOriginal = container.Find("NoObstacles");
            Transform mirrorOriginal = container.Find("Mirror");
            Transform staticLightsOriginal = container.Find("StaticLights");

            //Future duplicated switches
            Transform noEnergy = null;
            Transform noObstacles = null;
            Transform mirror = null;
            Transform staticLights = null;

            //Create up button
            Button _pageUpButton = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), container);
            _pageUpButton.transform.parent = container;
            _pageUpButton.transform.localScale = Vector3.one;
            (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2((_pageUpButton.transform.parent as RectTransform).sizeDelta.x, 3.5f);
            _pageUpButton.onClick.AddListener(delegate ()
            {
                noEnergy.gameObject.SetActive(true);
                noObstacles.gameObject.SetActive(true);
                mirror.gameObject.SetActive(true);
                staticLights.gameObject.SetActive(true);
                customOptions.ToList().ForEach(x => x.gameObject.SetActive(false));
            });
            _pageUpButton.interactable = true;

            //Duplicate and delete default toggles so that the up button is on the top
            noEnergy = UnityEngine.Object.Instantiate(noEnergyOriginal, container);
            noObstacles = UnityEngine.Object.Instantiate(noObstaclesOriginal, container);
            mirror = UnityEngine.Object.Instantiate(mirrorOriginal, container);
            staticLights = UnityEngine.Object.Instantiate(staticLightsOriginal, container);

            //Create custom options
            foreach (GameOption option in customOptions)
            {
                option.Instantiate();
            }

            //Destroy original toggles
            UnityEngine.Object.DestroyImmediate(noEnergyOriginal.gameObject);
            UnityEngine.Object.DestroyImmediate(noObstaclesOriginal.gameObject);
            UnityEngine.Object.DestroyImmediate(mirrorOriginal.gameObject);
            UnityEngine.Object.DestroyImmediate(staticLightsOriginal.gameObject);

            //Create down button
            Button _pageDownButton = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), container);
            _pageDownButton.transform.parent = container;
            _pageDownButton.transform.localScale = Vector3.one;
            (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2((_pageDownButton.transform.parent as RectTransform).sizeDelta.x, (_pageDownButton.transform as RectTransform).sizeDelta.y);
            _pageDownButton.onClick.AddListener(delegate ()
            {
                noEnergy.gameObject.SetActive(false);
                noObstacles.gameObject.SetActive(false);
                mirror.gameObject.SetActive(false);
                staticLights.gameObject.SetActive(false);
                customOptions.ToList().ForEach(x => x.gameObject.SetActive(true));
            });
            _pageDownButton.interactable = true;
        }
    }
}
