// ---------------------------------------------------------------------
//
// Copyright (c) 2018 Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Creator Agreement, located
// here: https://id.magicleap.com/creator-terms
//
// ---------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Events;
using System;

#if PLATFORM_LUMIN
using UnityEngine.XR.MagicLeap;
using MagicLeap;
#endif

namespace MagicLeapTools.UserInput
{
    /// <summary>
    /// Wrapper around the controller and its calibration components.
    /// </summary>
    public class MLControllerInput : MonoBehaviour
    {
        public bool IgnoreSwipe = false;
#if PLATFORM_LUMIN
        [SerializeField]
        MLControllerConnectionHandlerBehavior controller;

        public UnityEvent HandleTriggerDown;
        public UnityEvent HandleTriggerUp;
        public UnityEvent HandleBumperDown;
        public UnityEvent HandleBumperUp;
        public UnityEvent HandleHomeTap;

        public enum GestureType
        {
            None,
            Swipe
        }

        public enum GestureDirection
        {
            None,
            Left,
            Right,
            Up,
            Down
        }

        public delegate void GestureResponseCallback(MLControllerInput controllerInput, GestureType type, GestureDirection direction);
        public static GestureResponseCallback OnGestureDetected;

        // ------ Public  Members ------

        bool triggerIsDown = false;
        bool mlInputStartedHere = false;

        public Vector2 TouchPosition
        {
            get
            {
                return controller.ConnectedController.Touch1PosAndForce;
            }
        }

        public float TouchForce
        {
            get
            {
                return controller.ConnectedController.Touch1PosAndForce.z;
            }
        }

        public bool TouchActive
        {
            get
            {
                return controller.ConnectedController.Touch1Active;
            }
        }

        public bool BumperDown
        {
            get
            {
                return _bumperDown;
            }
        }

        public bool TriggerDown
        {
            get
            {
                return triggerIsDown;
            }
        }

        // ------ Private Members ------
        private bool _bumperDown;
        private const float TriggerThresh = 0.2f;

        static int index = 0;
        // ------ MonoBehaviour Methods ------       
        private void Start()
        {
            this.name = index++ + " - " + this.name;
            InitializeController();
        }

#if UNITY_EDITOR
        [SerializeField]
        KeyCode KeyCodeTrigger = KeyCode.LeftBracket;

        [SerializeField]
        KeyCode KeyCodeBumper = KeyCode.RightBracket;

        [SerializeField]
        KeyCode KeyCodeHomeTap = KeyCode.Equals;
#endif

        bool tapEnabled = true;
        int lastFrameProcessed = 0;

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCodeTrigger) &&
                HandleTriggerDown != null)
            {
                HandleTriggerDown.Invoke();
            }

            if (Input.GetKeyDown(KeyCodeBumper) &&
                HandleBumperDown != null)
            {
                HandleBumperDown.Invoke();
            }

            if (Input.GetKeyUp(KeyCodeTrigger) &&
                HandleTriggerUp != null)
            {
                HandleTriggerUp.Invoke();
            }

            if (Input.GetKeyUp(KeyCodeBumper) &&
                HandleBumperUp != null)
            {
                HandleBumperUp.Invoke();
            }

            if (Input.GetKeyUp(KeyCodeHomeTap) &&
                HandleHomeTap != null)
            {
                HandleHomeTap.Invoke();
            }
#else
            if (!MLInput.IsStarted)
            {
                return;
            }

            if (controller == null)
            {
                InitializeController();
                return;
            }
#endif
            // Check TouchPad
            if (OnGestureDetected != null && controller != null && controller.ConnectedController != null)
            {
                if (controller.ConnectedController.Touch1Active &&
                    controller.ConnectedController.Touch1PosAndForce.z > 0.5f)
                {
                    if (tapEnabled)
                    {
                        int thisFrame = Time.frameCount;
                        if (thisFrame - lastFrameProcessed > 5)
                        {
                            if (controller.ConnectedController.Touch1PosAndForce.y > 0.5f)
                            {
                                OnGestureDetected(this, GestureType.Swipe, GestureDirection.Up);
                            }
                            else if (controller.ConnectedController.Touch1PosAndForce.y < -0.5f)
                            {
                                OnGestureDetected(this, GestureType.Swipe, GestureDirection.Down);
                            }
                            if (controller.ConnectedController.Touch1PosAndForce.x > 0.5f)
                            {
                                OnGestureDetected(this, GestureType.Swipe, GestureDirection.Right);
                            }
                            else if (controller.ConnectedController.Touch1PosAndForce.x < -0.5f)
                            {
                                OnGestureDetected(this, GestureType.Swipe, GestureDirection.Left);
                            }
                            lastFrameProcessed = thisFrame;
                        }
                        tapEnabled = controller.ConnectedController.Touch1PosAndForce.z > 0.7f;
                        return;
                    }
                }

                if (!controller.ConnectedController.Touch1Active ||
                    controller.ConnectedController.Touch1PosAndForce.z < 0.5f)
                {
                    tapEnabled = true;
                }
            }
        }

        // ------ Private Methods ------
        private void InitializeController()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<MLControllerConnectionHandlerBehavior>();
                if (controller == null)
                {
                    Debug.LogError(name + ": MLControllerInput needs a controller in the scene");
                }
            }
#if !UNITY_EDITOR
            MLResult result = MLInput.Start();
            if (result.IsOk)
            {
                if (!IgnoreSwipe) {
                    MLInput.OnControllerTouchpadGestureEnd += MLInput_OnControllerTouchpadGestureEnd;
                }

                MLInput.OnControllerButtonDown += MLInput_OnControllerButtonDown;
                MLInput.OnControllerButtonUp += MLInput_OnControllerButtonUp;
                MLInput.OnTriggerDown += MLInput_OnTriggerDown;
                MLInput.OnTriggerUp += MLInput_OnTriggerUp;

                mlInputStartedHere = true;
            }
#endif
        }


        private void MLInput_OnTriggerDown(byte controllerId, float triggerValue)
        {
            if (controller.ConnectedController.Id != controllerId)
            {
                return;
            }
            if (triggerValue > 0.5f && !triggerIsDown)
            {
                if (HandleTriggerDown != null)
                {
                    HandleTriggerDown.Invoke();
                }
                triggerIsDown = true;
            }
        }

        private void MLInput_OnTriggerUp(byte controllerId, float triggerValue)
        {
            if (controller.ConnectedController.Id != controllerId)
            {
                return;
            }
            triggerIsDown = false;
        }

        private void MLInput_OnControllerButtonUp(byte controllerId, MLInput.Controller.Button button)
        {
            if (controller.ConnectedController.Id != controllerId)
            {
                return;
            }

            switch (button)
            {
                case MLInput.Controller.Button.Bumper:
                    _bumperDown = false;
                    if (HandleBumperUp != null)
                    {
                        HandleBumperUp.Invoke();
                    }
                    break;

                case MLInput.Controller.Button.HomeTap:
                    if (HandleHomeTap != null)
                    {
                        HandleHomeTap.Invoke();
                    }
                    break;
            }
        }

        private void MLInput_OnControllerButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            if (controller.ConnectedController.Id != controllerId)
            {
                return;
            }

            switch (button)
            {
                case MLInput.Controller.Button.Bumper:
                    _bumperDown = true;
                    if (HandleBumperDown != null)
                    {
                        HandleBumperDown.Invoke();
                    }
                    break;

                case MLInput.Controller.Button.HomeTap:
                    // HomeTap calls button down and button up simultaneously whenever
                    // it is pressed and released. HomeTap should be ignored on
                    // ButtonDown and only handled on ButtonUp, or vice versa.
                    break;
            }

        }

        void MLInput_OnControllerTouchpadGestureEnd(byte controller_id, MLInput.Controller.TouchpadGesture gesture)
        {
            if (controller.ConnectedController.Id == controller_id)
            {
                if (OnGestureDetected != null)
                {
                    GestureType gestureType = GestureType.None;
                    GestureDirection gestureDirection = GestureDirection.None;
                    switch (gesture.Type)
                    {
                        case MLInput.Controller.TouchpadGesture.GestureType.Swipe:
                            gestureType = GestureType.Swipe;
                            switch (gesture.Direction)
                            {
                                case MLInput.Controller.TouchpadGesture.GestureDirection.Left:
                                    gestureDirection = GestureDirection.Left;
                                    break;
                                case MLInput.Controller.TouchpadGesture.GestureDirection.Right:
                                    gestureDirection = GestureDirection.Right;
                                    break;
                                case MLInput.Controller.TouchpadGesture.GestureDirection.Up:
                                    gestureDirection = GestureDirection.Up;
                                    break;
                                case MLInput.Controller.TouchpadGesture.GestureDirection.Down:
                                    gestureDirection = GestureDirection.Down;
                                    break;
                            }
                            break;
                    }
                    if (gestureType != GestureType.None)
                    {
                        OnGestureDetected(this, gestureType, gestureDirection);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (mlInputStartedHere)
            {
                if (!IgnoreSwipe)
                {
                    MLInput.OnControllerTouchpadGestureEnd -= MLInput_OnControllerTouchpadGestureEnd;
                }
                MLInput.OnControllerButtonDown -= MLInput_OnControllerButtonDown;
                MLInput.OnControllerButtonUp -= MLInput_OnControllerButtonUp;
                MLInput.OnTriggerDown -= MLInput_OnTriggerDown;
                MLInput.OnTriggerUp -= MLInput_OnTriggerUp;
                MLInput.Stop();

                mlInputStartedHere = false;
            }
        }
#endif
    }
}