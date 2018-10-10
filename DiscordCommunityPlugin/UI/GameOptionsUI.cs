using DiscordCommunityPlugin.Misc;
using DiscordCommunityPlugin.UI.Components;
using DiscordCommunityPlugin.UI.ViewControllers;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using Logger = DiscordCommunityShared.Logger;

namespace DiscordCommunityPlugin.UI
{
    class GameOptionsUI
    {
        public static IEnumerator DoMenuTesting()
        {
            Logger.Info($"WAITING FOR VALUES");
            StandardLevelDetailViewController _sldvc = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
            GameplayOptionsViewController _govc = _sldvc.GetField<GameplayOptionsViewController>("_gameplayOptionsViewController");

            Logger.Info($"DOING MENU TESTING");

            RectTransform container = (RectTransform)_govc.transform.Find("Switches").Find("Container");
            container.sizeDelta = new Vector2(container.sizeDelta.x, container.sizeDelta.y + 7f);
            //container.position = new Vector3(container.position.x, container.position.y + 0.1f, container.position.z);

            Transform noEnergyOriginal = container.Find("NoEnergy");
            Transform noObstaclesOriginal = container.Find("NoObstacles");
            Transform mirrorOriginal = container.Find("Mirror");
            Transform staticLightsOriginal = container.Find("StaticLights");

            Transform noEnergy = null;
            Transform noObstacles = null;
            Transform mirror = null;
            Transform staticLights = null;
            
            GameObject chromaToggle = null;

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
                chromaToggle.SetActive(false);
            });
            _pageUpButton.interactable = true;

            //Duplicate and delete default toggles so that the up button is on the top
            noEnergy = UnityEngine.Object.Instantiate(noEnergyOriginal, container);
            noObstacles = UnityEngine.Object.Instantiate(noObstaclesOriginal, container);
            mirror = UnityEngine.Object.Instantiate(mirrorOriginal, container);
            staticLights = UnityEngine.Object.Instantiate(staticLightsOriginal, container);

            UnityEngine.Object.DestroyImmediate(noEnergyOriginal.gameObject);
            UnityEngine.Object.DestroyImmediate(noObstaclesOriginal.gameObject);
            UnityEngine.Object.DestroyImmediate(mirrorOriginal.gameObject);
            UnityEngine.Object.DestroyImmediate(staticLightsOriginal.gameObject);

            //Create test toggle
            chromaToggle = UnityEngine.Object.Instantiate(noEnergy.gameObject);
            chromaToggle.GetComponentInChildren<TextMeshProUGUI>().text = "ChromaToggle";
            chromaToggle.name = "ChromaToggle";
            chromaToggle.layer = container.gameObject.layer;
            chromaToggle.transform.parent = container;
            chromaToggle.transform.localPosition = Vector3.zero;
            chromaToggle.transform.localScale = Vector3.one;
            chromaToggle.transform.rotation = Quaternion.identity;
            chromaToggle.SetActive(false);

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
                chromaToggle.SetActive(true);
            });
            _pageDownButton.interactable = true;

            Logger.Info($"PARENT ANCHORS  : {(_pageDownButton.transform.parent as RectTransform).anchorMin.x} {(_pageDownButton.transform.parent as RectTransform).anchorMin.y} {(_pageDownButton.transform.parent as RectTransform).anchorMax.x} {(_pageDownButton.transform.parent as RectTransform).anchorMax.y}");
            Logger.Info($"PARENT SIZES    : {(_pageDownButton.transform.parent as RectTransform).sizeDelta.x} {(_pageDownButton.transform.parent as RectTransform).sizeDelta.y} {(_pageDownButton.transform.parent as RectTransform).rect.size.x} {(_pageDownButton.transform.parent as RectTransform).rect.size.y}");
            Logger.Info($"PARENT POSITIONS: {(_pageDownButton.transform.parent as RectTransform).position.x} {(_pageDownButton.transform.parent as RectTransform).position.y} {(_pageDownButton.transform.parent as RectTransform).anchoredPosition.x} {(_pageDownButton.transform.parent as RectTransform).anchoredPosition.y}");
            Logger.Info($"BUTTON ANCHORS  : {(_pageDownButton.transform as RectTransform).anchorMin.x} {(_pageDownButton.transform as RectTransform).anchorMin.y} {(_pageDownButton.transform as RectTransform).anchorMax.x} {(_pageDownButton.transform as RectTransform).anchorMax.y}");
            Logger.Info($"BUTTON SIZES    : {(_pageDownButton.transform as RectTransform).sizeDelta.x} {(_pageDownButton.transform as RectTransform).sizeDelta.y} {(_pageDownButton.transform as RectTransform).rect.size.x} {(_pageDownButton.transform as RectTransform).rect.size.y}");
            Logger.Info($"BUTTON POSITIONS: {(_pageDownButton.transform as RectTransform).position.x} {(_pageDownButton.transform as RectTransform).position.y} {(_pageDownButton.transform as RectTransform).anchoredPosition.x} {(_pageDownButton.transform as RectTransform).anchoredPosition.y}");

            Logger.Success($"DONE MENU TESTING");
            yield return null;
        }
    }
}
