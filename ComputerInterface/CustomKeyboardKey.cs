﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace ComputerInterface
{
    public class CustomKeyboardKey : GorillaTriggerBox
    {
        private const int PRESS_COOLDOWN = 150;
        private const float KEY_BUMP_AMOUNT = 0.2f;
        private readonly Color _pressedColor = new Color(0.5f, 0.5f, 0.5f);

        public static bool KeyDebuggerEnabled;

        private static Dictionary<EKeyboardKey, Key> _keyMap;

        public EKeyboardKey KeyboardKey { get; private set; }

        public float pressTime;

        public bool functionKey;

        private CustomComputer _computer;

        private bool _isOnCooldown;

        private Material _material;
        private Color _originalColor;
        private KeyHandler _keyHandler;

        private void Awake()
        {
            enabled = false;
            _material = GetComponent<MeshRenderer>().material;
            _originalColor = _material.color;

            CreateKeyMap();
        }

        /// <summary>
        /// Used for debugging keyboard feature
        /// </summary>
        public void Fetch()
        {
            _keyHandler?.Fetch();
        }

        public void Init(CustomComputer computer, EKeyboardKey key)
        {
            _computer = computer;
            KeyboardKey = key;

            if (_keyHandler != null)
            {
                _keyHandler.OnClick -= OnISKeyPress;
            }

            if (_keyMap.TryGetValue(key, out var ISKey))
            {
                _keyHandler = new KeyHandler(Keyboard.current[ISKey]);
                _keyHandler.OnClick += OnISKeyPress;
            }

            enabled = true;
        }

        public void Init(CustomComputer computer, EKeyboardKey key, string text)
        {
            Init(computer, key);
            GetComponentInChildren<Text>().text = text;
        }

        public void Init(CustomComputer computer, EKeyboardKey key, string text, Color buttonColor)
        {
            Init(computer, key, text);
            _material.color = buttonColor;
            _originalColor = buttonColor;
        }

        private async void OnTriggerEnter(Collider collider)
        {
            BumpIn();
            if (_isOnCooldown) return;
            _isOnCooldown = true;

            if (collider.GetComponentInParent<GorillaTriggerColliderHandIndicator>() != null)
            {
                GorillaTriggerColliderHandIndicator component = collider.GetComponent<GorillaTriggerColliderHandIndicator>();

                _computer.PressButton(this);

                if (component != null)
                {
                    GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
                }
            }

            await Task.Delay(PRESS_COOLDOWN);
            _isOnCooldown = false;
        }

        private void OnTriggerExit(Collider collider)
        {
            BumpOut();
        }

        private void BumpIn()
        {
            var pos = transform.localPosition;
            pos.y -= KEY_BUMP_AMOUNT;
            transform.localPosition = pos;

            _material.color = _pressedColor;
        }

        private void BumpOut()
        {
            var pos = transform.localPosition;
            pos.y += KEY_BUMP_AMOUNT;
            transform.localPosition = pos;

            _material.color = _originalColor;
        }

        private void OnISKeyPress()
        {
            _computer.PressButton(this);
        }

        private void CreateKeyMap()
        {
            if (_keyMap != null) return;

            _keyMap = new Dictionary<EKeyboardKey, Key>();

            _keyMap.Add(EKeyboardKey.Left, Key.LeftArrow);
            _keyMap.Add(EKeyboardKey.Right, Key.RightArrow);
            _keyMap.Add(EKeyboardKey.Up, Key.UpArrow);
            _keyMap.Add(EKeyboardKey.Down, Key.DownArrow);

            _keyMap.Add(EKeyboardKey.Back, Key.Escape);
            _keyMap.Add(EKeyboardKey.Delete, Key.Backspace);

            // add num keys
            for (int i = 0; i < 10; i++)
            {
                var localKey = (EKeyboardKey)Enum.Parse(typeof(EKeyboardKey), "NUM" + i);
                var key = (Key) Enum.Parse(typeof(Key), "Digit" + i);

                _keyMap.Add(localKey, key);
            }

            // add keys that match in name like alphabet keys
            foreach (var gtKey in Enum.GetNames(typeof(EKeyboardKey)))
            {
                var val = (EKeyboardKey) Enum.Parse(typeof(EKeyboardKey), gtKey);
                if(_keyMap.ContainsKey(val))continue;

                if (!Enum.TryParse(gtKey, true, out Key key)) continue;

                _keyMap.Add(val, key);
            }
        }

        internal class KeyHandler
        {
            public event Action OnClick;

            private readonly KeyControl _key;
            private bool _wasPressed;

            public KeyHandler(KeyControl key)
            {
                _key = key;
            }

            public void Fetch()
            {
                if (_key.isPressed && !_wasPressed)
                {
                    _wasPressed = true;
                    OnClick?.Invoke();
                }

                if (!_key.isPressed && _wasPressed)
                {
                    _wasPressed = false;
                }
            }
        }
    }
}