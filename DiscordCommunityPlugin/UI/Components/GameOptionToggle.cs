using System;
using TMPro;
using UnityEngine;

namespace DiscordCommunityPlugin.UI
{
    public class GameOptionToggle
    {
        public GameObject gameObject;

        private HMUI.Toggle _toggle;

        private TextMeshProUGUI _nameText;

        private string _prefKey;

        public event Action<bool> OnToggle;

        internal string NameText
        {
            get
            {
                return this._nameText.text;
            }
            set
            {
                this._nameText.text = value;
            }
        }

        internal bool Value { get; set; }

        internal GameOptionToggle(GameObject parent, GameObject target, string prefKey, Sprite icon, string text, bool defaultValue)
        {
            this.Value = defaultValue;
            this._prefKey = prefKey;

            GameObject gameObject = UnityEngine.Object.Instantiate(target);
            gameObject.name = text;
            gameObject.layer = parent.layer;
            gameObject.transform.parent = parent.transform;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localScale = Vector3.one;
            gameObject.transform.rotation = Quaternion.identity;

            this.gameObject = gameObject;
            this._toggle = gameObject.GetComponentInChildren<HMUI.Toggle>();
            this._toggle.isOn = this.Value;
            this._nameText = gameObject.GetComponentInChildren<TextMeshProUGUI>();
            this.NameText = text;
            this._toggle.didSwitchEvent += new Action<HMUI.Toggle, bool>(this.HandleNoEnergyToggleDidSwitch);
        }

        public virtual void HandleNoEnergyToggleDidSwitch(HMUI.Toggle toggle, bool isOn)
        {
            this.Value = isOn;
            OnToggle?.Invoke(isOn);
        }

    }
}
