using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FoodSlicer
{
    public class KnifeInputController : MonoBehaviour
    {
        public static KnifeInputController Instance { get; private set; }

        public event Action OnChopPressed;
        public event Action OnChopHeld;
        public event Action OnChopReleased;

        private bool isHolding = false;
        private bool inputEnabled = true;
        private KnifeController activeKnife;

        // Cached to avoid per-frame GC allocations in IsPointerOverUI
        private PointerEventData cachedEventData;
        private readonly System.Collections.Generic.List<RaycastResult> cachedRaycastResults = new System.Collections.Generic.List<RaycastResult>();

        public KnifeController ActiveKnife => activeKnife;
        public bool IsInputActive => inputEnabled;

        public void SetActiveKnife(KnifeController newKnife)
        {
            if (activeKnife != null && activeKnife != newKnife)
            {
                activeKnife.ResetKnifeToIdle();
            }
            activeKnife = newKnife;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            inputEnabled = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            activeKnife = null;
        }

        public void SetInputActive(bool active)
        {
            inputEnabled = active;
            if (!active && isHolding)
            {
                isHolding = false;
                OnChopReleased?.Invoke();
                if (activeKnife != null)
                {
                    activeKnife.HandleChopReleased();
                }
            }
        }

        private void Update()
        {
            if (!inputEnabled || (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying()))
            {
                if (isHolding)
                {
                    isHolding = false;
                    OnChopReleased?.Invoke();
                    if (activeKnife != null)
                    {
                        activeKnife.HandleChopReleased();
                    }
                }
                return;
            }

            // Mobile touch input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (IsPointerOverUI(touch.position))
                {
                    return;
                }

                if (touch.phase == TouchPhase.Began)
                {
                    isHolding = true;
                    OnChopPressed?.Invoke();
                    if (activeKnife != null)
                    {
                        activeKnife.HandleChopPressed();
                    }
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (isHolding)
                    {
                        OnChopHeld?.Invoke();
                        if (activeKnife != null)
                        {
                            activeKnife.HandleChopHeld();
                        }
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (isHolding)
                    {
                        isHolding = false;
                        OnChopReleased?.Invoke();
                        if (activeKnife != null)
                        {
                            activeKnife.HandleChopReleased();
                        }
                    }
                }
            }
            // Mouse input for Unity Editor & Standalone
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!IsPointerOverUI(Input.mousePosition))
                    {
                        isHolding = true;
                        OnChopPressed?.Invoke();
                        if (activeKnife != null)
                        {
                            activeKnife.HandleChopPressed();
                        }
                    }
                }
                else if (Input.GetMouseButton(0))
                {
                    if (isHolding)
                    {
                        OnChopHeld?.Invoke();
                        if (activeKnife != null)
                        {
                            activeKnife.HandleChopHeld();
                        }
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    if (isHolding)
                    {
                        isHolding = false;
                        OnChopReleased?.Invoke();
                        if (activeKnife != null)
                        {
                            activeKnife.HandleChopReleased();
                        }
                    }
                }
            }
        }

        private bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;

            if (cachedEventData == null)
            {
                cachedEventData = new PointerEventData(EventSystem.current);
            }
            else
            {
                cachedEventData.Reset();
            }
            cachedEventData.position = screenPos;

            cachedRaycastResults.Clear();
            EventSystem.current.RaycastAll(cachedEventData, cachedRaycastResults);
            return cachedRaycastResults.Count > 0;
        }
    }
}
