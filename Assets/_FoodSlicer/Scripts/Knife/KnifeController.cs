using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodSlicer
{
    public enum KnifeState
    {
        Idle,
        Chopping,
        Impact,
        Recovering,
        Disabled
    }

    public class KnifeController : MonoBehaviour
    {
        [Header("Mode & Controls")]
        [SerializeField] [Tooltip("Tap mode or hold-to-repeat chopping")]
        private KnifeInputMode inputMode = KnifeInputMode.HoldRepeat;

        [Header("Poses (Local Space)")]
        [SerializeField] [Tooltip("Local position of the knife when hovering at rest")]
        private Vector3 idleLocalPosition = Vector3.zero;

        [UnityEngine.Serialization.FormerlySerializedAs("chopOffset")]
        [SerializeField] [Tooltip("Local position of the knife at maximum downward cut")]
        private Vector3 cutLocalPosition = new Vector3(0f, -0.06f, 0f);

        [SerializeField] [Tooltip("Local rotation angles when idle")]
        private Vector3 idleLocalEuler = new Vector3(-2f, 0f, -14f);

        [UnityEngine.Serialization.FormerlySerializedAs("impactLocalEuler")]
        [SerializeField] [Tooltip("Local rotation angles at maximum downward cut")]
        private Vector3 cutLocalEuler = new Vector3(-6f, 0f, 6f);

        [Header("Timing (Inspector Adjustable)")]
        [UnityEngine.Serialization.FormerlySerializedAs("attackDuration")]
        [SerializeField] [Range(0.02f, 0.3f)] [Tooltip("Base attack duration before speed multiplier")]
        private float baseAttackDuration = 0.08f;

        [UnityEngine.Serialization.FormerlySerializedAs("returnDuration")]
        [SerializeField] [Range(0.02f, 0.3f)] [Tooltip("Base return duration before speed multiplier")]
        private float baseReturnDuration = 0.09f;

        [SerializeField] [Range(0.01f, 0.1f)] [Tooltip("Minimum attack duration clamp for stability")]
        private float minimumAttackDuration = 0.04f;

        [SerializeField] [Range(0.01f, 0.1f)] [Tooltip("Minimum return duration clamp for stability")]
        private float minimumReturnDuration = 0.04f;

        [SerializeField] [Range(1f, 3f)] [Tooltip("Current knife speed multiplier")]
        private float currentKnifeSpeedMultiplier = 1f;

        [SerializeField] [Range(0.05f, 1f)] [Tooltip("Duration over which speed multiplier smoothly transitions")]
        private float speedTransitionDuration = 0.25f;

        [SerializeField] [Range(0.005f, 0.05f)] [Tooltip("Pause at bottom of cut stroke")]
        private float impactPauseDuration = 0.015f;

        [Header("Speed Progression Settings")]
        [SerializeField] private float speedIncreaseInterval = 3f;
        [SerializeField] private float firstIncreasePercent = 10f;
        [SerializeField] private float increaseStepPercent = 5f;
        [SerializeField] private float maxIncreasePercent = 50f;

        [Header("Animation Curves")]
        [UnityEngine.Serialization.FormerlySerializedAs("attackCurve")]
        [SerializeField] private AnimationCurve attackPositionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve attackRotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [UnityEngine.Serialization.FormerlySerializedAs("returnCurve")]
        [SerializeField] private AnimationCurve returnPositionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve returnRotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Slicing Volume & Collision")]
        [SerializeField] [Tooltip("Transform representing the blade edge center")]
        private Transform bladeEdgeCenter;

        [SerializeField] [Tooltip("Dimensions of slicing detection box")]
        private Vector3 sliceBoxSize = new Vector3(1.8f, 0.4f, 0.6f);

        [SerializeField] [Tooltip("Layer mask containing cuttable foods")]
        private LayerMask foodLayerMask = ~0;

        [SerializeField] [Tooltip("Trail renderer attached to the blade")]
        private TrailRenderer bladeTrail;

        [SerializeField] [Tooltip("Audio source on the knife")]
        private AudioSource knifeAudioSource;

        // Routine tracking state
        private Coroutine chopRoutine;
        private bool isChopping = false;
        private bool inputHeld = false;
        private KnifeState currentState = KnifeState.Idle;

        // Speed target set by FoodSpawner (single authority for speed progression)
        private float targetKnifeSpeedMultiplier = 1f;

        // Slicing state
        private bool sliceDetectionEnabled = true;
        private bool hasSlicedFoodInCurrentChop = false;
        private readonly HashSet<FoodItem> slicedFoodsInCurrentChop = new HashSet<FoodItem>();
        private Coroutine restCoroutine;

        // Cached local rotations
        public Quaternion IdleLocalRotation => Quaternion.Euler(idleLocalEuler);
        public Quaternion CutLocalRotation => Quaternion.Euler(cutLocalEuler);

        // Public properties
        public KnifeState CurrentState => currentState;
        public KnifeInputMode InputMode { get => inputMode; set => inputMode = value; }
        public Coroutine ChopRoutine => chopRoutine;
        public bool IsChopping => isChopping;
        public bool InputHeld => inputHeld;
        public float CurrentKnifeSpeedMultiplier { get => currentKnifeSpeedMultiplier; set => currentKnifeSpeedMultiplier = value; }
        public float BaseAttackDuration { get => baseAttackDuration; set => baseAttackDuration = value; }
        public float BaseReturnDuration { get => baseReturnDuration; set => baseReturnDuration = value; }
        public float MinimumAttackDuration { get => minimumAttackDuration; set => minimumAttackDuration = value; }
        public float MinimumReturnDuration { get => minimumReturnDuration; set => minimumReturnDuration = value; }
        public float SpeedTransitionDuration { get => speedTransitionDuration; set => speedTransitionDuration = value; }
        public AnimationCurve AttackPositionCurve { get => attackPositionCurve; set => attackPositionCurve = value; }
        public AnimationCurve AttackRotationCurve { get => attackRotationCurve; set => attackRotationCurve = value; }
        public AnimationCurve ReturnPositionCurve { get => returnPositionCurve; set => returnPositionCurve = value; }
        public AnimationCurve ReturnRotationCurve { get => returnRotationCurve; set => returnRotationCurve = value; }
        public bool SliceDetectionEnabled => sliceDetectionEnabled;

        // Compatibility properties
        public float CurrentSpeedMultiplier => currentKnifeSpeedMultiplier;
        public float OriginalAttackDuration => baseAttackDuration;
        public float OriginalReturnDuration => baseReturnDuration;
        public float AttackDuration => baseAttackDuration / currentKnifeSpeedMultiplier;
        public float ReturnDuration => baseReturnDuration / currentKnifeSpeedMultiplier;

        private void Awake()
        {
            if (bladeEdgeCenter == null) bladeEdgeCenter = transform;
            if (bladeTrail == null) bladeTrail = GetComponentInChildren<TrailRenderer>();
            if (knifeAudioSource == null) knifeAudioSource = GetComponent<AudioSource>();

            // Ensure no Animator fights procedural control (Requirement 12)
            Animator anim = GetComponent<Animator>();
            if (anim != null) anim.enabled = false;

            // Enforce consistent local space idle pose (Requirement 10, 11)
            transform.localPosition = idleLocalPosition;
            transform.localRotation = IdleLocalRotation;

            if (bladeTrail != null) bladeTrail.emitting = false;
        }

        private void Start()
        {
            if (KnifeInputController.Instance != null)
            {
                if (KnifeInputController.Instance.ActiveKnife == null)
                {
                    KnifeInputController.Instance.SetActiveKnife(this);
                }
            }
        }

        private void OnDisable()
        {
            StopChopRoutine();
            ResetSpeed();
            if (KnifeInputController.Instance != null && KnifeInputController.Instance.ActiveKnife == this)
            {
                KnifeInputController.Instance.SetActiveKnife(null);
            }
            UnsubscribeInput();
        }

        private void OnDestroy()
        {
            StopChopRoutine();
            ResetSpeed();
            if (KnifeInputController.Instance != null && KnifeInputController.Instance.ActiveKnife == this)
            {
                KnifeInputController.Instance.SetActiveKnife(null);
            }
            UnsubscribeInput();
        }

        private void UnsubscribeInput()
        {
            if (KnifeInputController.Instance != null)
            {
                KnifeInputController.Instance.OnChopPressed -= HandleChopPressed;
                KnifeInputController.Instance.OnChopHeld -= HandleChopHeld;
                KnifeInputController.Instance.OnChopReleased -= HandleChopReleased;
            }
        }

        public void EnableSliceDetection(bool enable)
        {
            sliceDetectionEnabled = enable;
        }

        private void Update()
        {
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
            {
                if (currentState != KnifeState.Disabled)
                {
                    currentState = KnifeState.Disabled;
                }
                return;
            }

            // Smoothly transition multiplier toward the target set by FoodSpawner.
            // Speed progression is driven exclusively by FoodSpawner.TimedSpeedProgressionRoutine()
            // which calls SetSpeedMultiplier(). We only handle the smooth interpolation here.
            if (!Mathf.Approximately(currentKnifeSpeedMultiplier, targetKnifeSpeedMultiplier))
            {
                float speedRate = 1f / Mathf.Max(0.01f, speedTransitionDuration);
                currentKnifeSpeedMultiplier = Mathf.MoveTowards(currentKnifeSpeedMultiplier, targetKnifeSpeedMultiplier, speedRate * Time.deltaTime);
            }

            // Fallback check for held input
            if (inputHeld && !isChopping && currentState != KnifeState.Disabled)
            {
                StartChopRoutine();
            }
        }

        public void HandleChopPressed()
        {
            inputHeld = true;
            if (!isChopping && (GameStateManager.Instance == null || GameStateManager.Instance.IsPlaying()))
            {
                StartChopRoutine();
            }
        }

        public void HandleChopHeld()
        {
            inputHeld = true;
            if (!isChopping && (GameStateManager.Instance == null || GameStateManager.Instance.IsPlaying()))
            {
                StartChopRoutine();
            }
        }

        public void HandleChopReleased()
        {
            inputHeld = false;
            // The running ChopSequenceRoutine will finish its current cycle, return smoothly to idle, and set isChopping = false
        }

        public void ExecuteChop()
        {
            inputHeld = true;
            if (!isChopping)
            {
                StartChopRoutine();
            }
        }

        private void StartChopRoutine()
        {
            if (isChopping) return; // Strict: only ONE active chop routine (Requirement 2, 3)
            if (currentState == KnifeState.Disabled) return;

            if (chopRoutine != null)
            {
                StopCoroutine(chopRoutine);
                chopRoutine = null;
            }

            chopRoutine = StartCoroutine(ChopSequenceRoutine());
        }

        private IEnumerator ChopSequenceRoutine()
        {
            isChopping = true;

            // Press/hold continuously performs complete chop cycles (Requirement 9)
            while (inputHeld)
            {
                if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
                {
                    break;
                }

                currentState = KnifeState.Chopping;

                // Calculate and cache cycle durations at the START of THIS chop cycle (Requirements 5, 6, 8)
                float cycleAttackDuration = Mathf.Max(minimumAttackDuration, baseAttackDuration / currentKnifeSpeedMultiplier);
                float cycleReturnDuration = Mathf.Max(minimumReturnDuration, baseReturnDuration / currentKnifeSpeedMultiplier);

                hasSlicedFoodInCurrentChop = false;
                slicedFoodsInCurrentChop.Clear();

                if (bladeTrail != null) bladeTrail.emitting = true;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayWhoosh();

                // 1. Downward Attack Stroke: Ready/idle pose -> cutting pose (Requirements 9, 10, 11)
                // Uses unscaledDeltaTime so HitStopManager's timeScale changes don't freeze the knife
                float elapsed = 0f;
                while (elapsed < cycleAttackDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / cycleAttackDuration);
                    float posT = EvaluateCurve(attackPositionCurve, t);
                    float rotT = EvaluateCurve(attackRotationCurve, t);

                    transform.localPosition = Vector3.Lerp(idleLocalPosition, cutLocalPosition, posT);
                    transform.localRotation = Quaternion.Slerp(IdleLocalRotation, CutLocalRotation, rotT);

                    CheckSliceHits();
                    yield return null;
                }

                // Force exact cutting pose at impact (Requirement 10)
                transform.localPosition = cutLocalPosition;
                transform.localRotation = CutLocalRotation;
                currentState = KnifeState.Impact;
                CheckSliceHits();

                if (!hasSlicedFoodInCurrentChop)
                {
                    TriggerBoardImpact();
                }

                if (impactPauseDuration > 0f)
                {
                    float scaledPause = impactPauseDuration / currentKnifeSpeedMultiplier;
                    float pauseElapsed = 0f;
                    while (pauseElapsed < scaledPause)
                    {
                        pauseElapsed += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }

                // 2. Upward Return Stroke: Cutting pose -> Ready/idle pose (Requirements 9, 10, 11)
                currentState = KnifeState.Recovering;
                if (bladeTrail != null) bladeTrail.emitting = false;

                elapsed = 0f;
                while (elapsed < cycleReturnDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / cycleReturnDuration);
                    float posT = EvaluateCurve(returnPositionCurve, t);
                    float rotT = EvaluateCurve(returnRotationCurve, t);

                    transform.localPosition = Vector3.Lerp(cutLocalPosition, idleLocalPosition, posT);
                    transform.localRotation = Quaternion.Slerp(CutLocalRotation, IdleLocalRotation, rotT);

                    yield return null;
                }

                // Force exact idle pose at end of return stroke (Requirement 10)
                transform.localPosition = idleLocalPosition;
                transform.localRotation = IdleLocalRotation;
                currentState = KnifeState.Idle;

                // Loop immediately proceeds to next cycle if inputHeld is still true!
            }

            // Exiting routine: release finishes cycle and smoothly returns to exact idle pose (Requirements 9, 10)
            transform.localPosition = idleLocalPosition;
            transform.localRotation = IdleLocalRotation;
            if (bladeTrail != null) bladeTrail.emitting = false;
            currentState = KnifeState.Idle;
            isChopping = false;
            chopRoutine = null;
        }

        private float EvaluateCurve(AnimationCurve curve, float t)
        {
            return (curve != null && curve.length > 0) ? curve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
        }

        public void StopChopRoutine()
        {
            if (chopRoutine != null)
            {
                StopCoroutine(chopRoutine);
                chopRoutine = null;
            }
            isChopping = false;
            inputHeld = false;
            transform.localPosition = idleLocalPosition;
            transform.localRotation = IdleLocalRotation;
            if (bladeTrail != null) bladeTrail.emitting = false;
        }

        public void ResetKnifeToIdle()
        {
            StopChopRoutine();
            ResetSpeed();
            if (restCoroutine != null)
            {
                StopCoroutine(restCoroutine);
                restCoroutine = null;
            }

            transform.localPosition = idleLocalPosition;
            transform.localRotation = IdleLocalRotation;
            transform.localScale = Vector3.one;
            currentState = KnifeState.Idle;
            sliceDetectionEnabled = true;
            if (bladeTrail != null) bladeTrail.emitting = false;
        }

        public void ResetSpeed()
        {
            targetKnifeSpeedMultiplier = 1f;
            currentKnifeSpeedMultiplier = 1f;
        }

        public void SetSpeedMultiplier(float speedMultiplier)
        {
            if (speedMultiplier <= 0.01f) speedMultiplier = 1f;
            targetKnifeSpeedMultiplier = speedMultiplier;
            if (!Application.isPlaying)
            {
                currentKnifeSpeedMultiplier = speedMultiplier;
            }
        }

        public void PrepareForCompletion()
        {
            StopChopRoutine();
            if (restCoroutine != null)
            {
                StopCoroutine(restCoroutine);
                restCoroutine = null;
            }

            currentState = KnifeState.Disabled;
            sliceDetectionEnabled = false;
            inputHeld = false;
            if (bladeTrail != null) bladeTrail.emitting = false;
        }

        public void AnimateToRestPoint(Vector3 targetWorldPos, Quaternion targetWorldRot, float duration, System.Action onComplete)
        {
            StopChopRoutine();
            if (restCoroutine != null)
            {
                StopCoroutine(restCoroutine);
                restCoroutine = null;
            }

            currentState = KnifeState.Disabled;
            if (bladeTrail != null) bladeTrail.emitting = false;

            restCoroutine = StartCoroutine(RestAnimationRoutine(targetWorldPos, targetWorldRot, duration, onComplete));
        }

        private IEnumerator RestAnimationRoutine(Vector3 targetWorldPos, Quaternion targetWorldRot, float duration, System.Action onComplete)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = Mathf.SmoothStep(0f, 1f, t);

                float arcY = Mathf.Sin(t * Mathf.PI) * 0.15f;
                Vector3 currentPos = Vector3.Lerp(startPos, targetWorldPos, curvedT);
                currentPos.y += arcY;

                transform.position = currentPos;
                transform.rotation = Quaternion.Slerp(startRot, targetWorldRot, curvedT);
                yield return null;
            }

            transform.position = targetWorldPos;
            transform.rotation = targetWorldRot;
            restCoroutine = null;
            onComplete?.Invoke();
        }

        private void CheckSliceHits()
        {
            if (!sliceDetectionEnabled) return;

            Vector3 center = bladeEdgeCenter != null ? bladeEdgeCenter.position : transform.position;
            Collider[] hits = Physics.OverlapBox(center, sliceBoxSize * 0.5f, transform.rotation, foodLayerMask, QueryTriggerInteraction.Collide);

            if (hits != null && hits.Length > 0)
            {
                foreach (Collider hit in hits)
                {
                    FoodItem food = hit.GetComponentInParent<FoodItem>();
                    if (food != null && !slicedFoodsInCurrentChop.Contains(food) && !food.IsCompleted)
                    {
                        slicedFoodsInCurrentChop.Add(food);
                        Vector3 closestPoint = hit.ClosestPoint(center);

                        bool cutSuccess = food.Slice(closestPoint, out bool isPerfect);
                        if (cutSuccess)
                        {
                            hasSlicedFoodInCurrentChop = true;
                        }
                    }
                }
            }
        }

        private void TriggerBoardImpact()
        {
            if (!sliceDetectionEnabled) return;

            Vector3 contactPoint = bladeEdgeCenter != null ? bladeEdgeCenter.position : transform.position;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayBoardHit();
            if (CameraFeedback.Instance != null) CameraFeedback.Instance.PlayBoardHitShake();
            if (HapticManager.Instance != null) HapticManager.Instance.TriggerBoardHit();
            if (VFXManager.Instance != null) VFXManager.Instance.PlayBoardHitEffect(contactPoint);

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RegisterBoardHit();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = bladeEdgeCenter != null ? bladeEdgeCenter.position : transform.position;
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, sliceBoxSize);
        }
    }
}
