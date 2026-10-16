using System.Collections;
using UnityEngine;

namespace FoodSlicer
{
    public class LevelCompletePresentation : MonoBehaviour
    {
        public static LevelCompletePresentation Instance { get; private set; }

        [Header("Completion Animation Rig")]
        [SerializeField] private GameObject completionRig;
        [SerializeField] private GameObject pillow;
        [SerializeField] private Transform knifeRestPoint;
        [SerializeField] private Transform firstFlipApex;
        [SerializeField] private Transform secondFlipApex;
        [SerializeField] private ParticleSystem confettiLeft;
        [SerializeField] private ParticleSystem confettiRight;
        [SerializeField] private ParticleSystem celebrationVFX;
        [SerializeField] private GameObject levelCompletePanel;

        [Header("Phase 1: Initial Rest Hold (0.00s - 0.35s)")]
        [Tooltip("Hold time on cushion in diagonal rest pose before first flip starts.")]
        [SerializeField] private float initialRestDuration = 0.35f;

        [Header("Phase 2: First Large Knife Flip (0.35s - 0.95s)")]
        [Tooltip("Duration of first large celebratory 360 flip.")]
        [SerializeField] private float firstFlipDuration = 0.60f;
        [Tooltip("Peak arc height above the pillow for the first flip.")]
        [SerializeField] private float firstFlipHeight = 0.54f;
        [Tooltip("Rightward screen offset for the first flip arc.")]
        [SerializeField] private float firstFlipHorizontalOffset = 0.14f;
        [Tooltip("Total spin degrees end-over-end (360 degrees).")]
        [SerializeField] private float firstFlipSpinDegrees = 360f;
        [SerializeField] private AnimationCurve firstFlipPositionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve firstFlipRotationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Phase 3: Rest After First Flip (0.95s - 1.20s)")]
        [Tooltip("Total pause after first landing before second flip begins.")]
        [SerializeField] private float firstRestDuration = 0.25f;
        [Tooltip("Duration of cushion squash on first landing.")]
        [SerializeField] private float firstLandingDuration = 0.10f;

        [Header("Phase 4: Second Smaller Knife Flip (1.20s - 1.65s)")]
        [Tooltip("Duration of second celebratory flourish flip (70-80% height of first flip).")]
        [SerializeField] private float secondFlipDuration = 0.45f;
        [SerializeField] private float secondFlipHeight = 0.38f;
        [SerializeField] private float secondFlipHorizontalOffset = 0.09f;
        [SerializeField] private float secondFlipSpinDegrees = 360f;
        [SerializeField] private AnimationCurve secondFlipPositionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve secondFlipRotationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Phase 5: Final Landing / Final Rest (1.65s - 1.90s)")]
        [Tooltip("Duration for knife to settle and cushion to recover after second flip.")]
        [SerializeField] private float finalSettleDuration = 0.25f;

        [Header("Phase 6: Celebration Particles (1.90s - 2.70s)")]
        [Tooltip("Delay after final settle before particles burst.")]
        [SerializeField] private float confettiStartDelay = 0.00f;
        [Tooltip("Duration celebration particles and fanfare breathe before panel appears.")]
        [SerializeField] private float confettiDuration = 0.80f;

        [Header("Phase 7: Level Complete Panel Timing (2.70s - 3.10s)")]
        [Tooltip("Short breathing room before Level Complete UI panel entrance starts.")]
        [SerializeField] private float levelCompletePanelDelay = 0.15f;
        [Tooltip("Fade-in duration of Level Complete panel.")]
        [SerializeField] private float levelCompletePanelFadeDuration = 0.35f;
        [Tooltip("Scale pop duration of Level Complete panel (0.88 -> 1.04 -> 1.00).")]
        [SerializeField] private float levelCompletePanelScaleDuration = 0.35f;

        [Header("Scale Configurations")]
        [Tooltip("Knife scale when resting on ceremonial cushion.")]
        [SerializeField] private Vector3 completionKnifeScale = new Vector3(0.32f, 0.32f, 0.32f);
        [Tooltip("Normal knife scale during active cutting gameplay.")]
        [SerializeField] private Vector3 gameplayKnifeScale = Vector3.one;
        [Tooltip("Global multiplier applied to completionKnifeScale.")]
        [SerializeField] private float knifeCompletionScaleMultiplier = 1.0f;

        [Tooltip("Base 3D scale applied to ceremonial cushion.")]
        [SerializeField] private Vector3 pillowScale = new Vector3(1.35f, 1.15f, 1.35f);
        [Tooltip("Additional width multiplier for fine-tuning.")]
        [SerializeField] private float pillowWidthScale = 1.0f;
        [Tooltip("Additional height multiplier for fine-tuning.")]
        [SerializeField] private float pillowHeightScale = 1.0f;
        [Tooltip("Additional depth multiplier for fine-tuning.")]
        [SerializeField] private float pillowDepthScale = 1.0f;

        [Header("Pillow Intro Timing (Pre-Sequence)")]
        [Tooltip("Duration for cushion to pop up with overshoot bounce and knife to move onto pillow.")]
        [SerializeField] private float pillowAppearDuration = 0.30f;

        private Coroutine presentationCoroutine;
        private KnifeController presentationKnife;

        public GameObject Pillow => pillow;
        public Transform KnifeRestPoint => knifeRestPoint;
        public Transform FirstFlipApex => firstFlipApex;
        public Transform SecondFlipApex => secondFlipApex;

        public float LevelCompletePanelFadeDuration => levelCompletePanelFadeDuration;
        public float LevelCompletePanelScaleDuration => levelCompletePanelScaleDuration;

        public Vector3 EffectivePillowScale
        {
            get
            {
                return new Vector3(
                    pillowScale.x * pillowWidthScale,
                    pillowScale.y * pillowHeightScale,
                    pillowScale.z * pillowDepthScale
                );
            }
        }

        public Vector3 EffectiveCompletionKnifeScale
        {
            get
            {
                return completionKnifeScale * knifeCompletionScaleMultiplier;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (pillow != null)
            {
                pillow.transform.localScale = EffectivePillowScale;
                pillow.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ResetPresentation()
        {
            if (presentationCoroutine != null)
            {
                StopCoroutine(presentationCoroutine);
                presentationCoroutine = null;
            }

            if (presentationKnife != null)
            {
                presentationKnife.transform.localScale = gameplayKnifeScale;
                presentationKnife = null;
            }

            if (pillow != null)
            {
                pillow.transform.localScale = EffectivePillowScale;
                pillow.SetActive(false);
            }

            if (confettiLeft != null)
            {
                confettiLeft.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (confettiRight != null)
            {
                confettiRight.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (celebrationVFX != null)
            {
                celebrationVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public IEnumerator PlayCompletionRoutine(KnifeController knife)
        {
            ResetPresentation();
            presentationKnife = knife;

            // Target rest anchors (captured in world space before any scaling)
            Vector3 restPos = knifeRestPoint != null ? knifeRestPoint.position : new Vector3(0f, 0.10f, 0.65f);
            Quaternion restRot = knifeRestPoint != null ? knifeRestPoint.rotation : Quaternion.Euler(14f, 42f, -18f);

            Vector3 targetPillowScale = EffectivePillowScale;
            Vector3 targetKnifeScale = EffectiveCompletionKnifeScale;

            // ----------------------------------------------------
            // PRE-SEQUENCE: PILLOW INTRO POP & KNIFE ARRIVAL ONTO PILLOW
            // ----------------------------------------------------
            yield return StartCoroutine(PlayPillowIntroRoutine(knife, restPos, restRot, targetPillowScale, targetKnifeScale));

            // Ensure exact resting pose at start of post-pillow sequence
            if (pillow != null)
            {
                pillow.transform.localScale = targetPillowScale;
            }
            if (knife != null)
            {
                knife.transform.position = restPos;
                knife.transform.rotation = restRot;
                knife.transform.localScale = targetKnifeScale;
            }

            // ----------------------------------------------------
            // POST-PILLOW SEQUENCE
            // ----------------------------------------------------
            yield return StartCoroutine(PlayPostPillowSequenceRoutine(knife, restPos, restRot, targetPillowScale, targetKnifeScale));
        }

        private IEnumerator PlayPillowIntroRoutine(
            KnifeController knife, 
            Vector3 restPos, 
            Quaternion restRot, 
            Vector3 targetPillowScale, 
            Vector3 targetKnifeScale)
        {
            if (pillow != null)
            {
                pillow.SetActive(true);
                pillow.transform.localScale = Vector3.zero;
            }

            Vector3 knifeStartPos = knife != null ? knife.transform.position : restPos;
            Quaternion knifeStartRot = knife != null ? knife.transform.rotation : restRot;
            Vector3 knifeStartScale = knife != null ? knife.transform.localScale : gameplayKnifeScale;

            float pDuration = Mathf.Max(0.05f, pillowAppearDuration);
            float elapsed = 0f;

            while (elapsed < pDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / pDuration);

                // Overshoot bounce scale for pillow: 0 -> 1.08 -> 1.00 of targetPillowScale
                if (pillow != null)
                {
                    float pFactor = t < 0.75f 
                        ? Mathf.Lerp(0f, 1.08f, t / 0.75f) 
                        : Mathf.Lerp(1.08f, 1.00f, (t - 0.75f) / 0.25f);
                    pillow.transform.localScale = targetPillowScale * pFactor;
                }

                // Smooth knife arrival onto pillow with slight parabolic lift & scale transition
                if (knife != null)
                {
                    float curvedT = Mathf.SmoothStep(0f, 1f, t);
                    float arcY = Mathf.Sin(t * Mathf.PI) * 0.12f;
                    Vector3 curPos = Vector3.Lerp(knifeStartPos, restPos, curvedT);
                    curPos.y += arcY;

                    knife.transform.position = curPos;
                    knife.transform.rotation = Quaternion.Slerp(knifeStartRot, restRot, curvedT);
                    knife.transform.localScale = Vector3.Lerp(knifeStartScale, targetKnifeScale, curvedT);
                }

                yield return null;
            }
        }

        public IEnumerator PlayPostPillowSequenceRoutine(
            KnifeController knife, 
            Vector3 restPos, 
            Quaternion restRot, 
            Vector3 targetPillowScale, 
            Vector3 targetKnifeScale)
        {
            // End-over-end somersault flip axis:
            // In the rest pose, the knife's blade extends along local negative X.
            // World vector along the blade:
            Vector3 bladeDir = (restRot * Vector3.left).normalized;
            // Cross product of blade direction and world up creates the transverse pitch axis:
            // Rotating with positive angle pitches blade UP toward vertical, over apex, down through vertical, and back to rest.
            Vector3 flipAxis = Vector3.Cross(bladeDir, Vector3.up).normalized;
            if (flipAxis.sqrMagnitude < 0.001f)
            {
                flipAxis = Vector3.forward;
            }

            // ====================================================
            // PHASE 1 — INITIAL REST HOLD (0.00s - 0.35s)
            // ====================================================
            // Keep knife clearly visible in diagonal rest pose on pillow
            if (knife != null)
            {
                knife.transform.position = restPos;
                knife.transform.rotation = restRot;
                knife.transform.localScale = targetKnifeScale;
            }
            yield return new WaitForSeconds(initialRestDuration);

            // ====================================================
            // PHASE 2 — FIRST LARGE KNIFE FLIP (0.35s - 0.95s)
            // ====================================================
            Vector3 firstApexPos = firstFlipApex != null 
                ? firstFlipApex.position 
                : restPos + new Vector3(firstFlipHorizontalOffset, firstFlipHeight, 0.04f);

            float elapsed = 0f;
            float fDuration = Mathf.Max(0.1f, firstFlipDuration);

            while (elapsed < fDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fDuration);

                if (knife != null)
                {
                    // Smooth curved parabolic arc above pillow (stays screen-visible, no forward depth drift)
                    float posT = firstFlipPositionCurve != null ? firstFlipPositionCurve.Evaluate(t) : t;
                    float heightFactor = 4f * posT * (1f - posT);
                    knife.transform.position = restPos + (firstApexPos - restPos) * heightFactor;

                    // End-over-end somersault flip:
                    // 0% -> starts diagonal, 25% -> vertical (blade up), 50% -> near horizontal, 75% -> vertical (blade down), 100% -> diagonal rest
                    float rotT = firstFlipRotationCurve != null ? firstFlipRotationCurve.Evaluate(t) : t;
                    float spinAngle = firstFlipSpinDegrees * rotT;
                    Quaternion spinRot = Quaternion.AngleAxis(spinAngle, flipAxis);
                    knife.transform.rotation = spinRot * restRot;
                }

                yield return null;
            }

            // Land cleanly back on pillow in rest pose
            if (knife != null)
            {
                knife.transform.position = restPos;
                knife.transform.rotation = restRot;
                knife.transform.localScale = targetKnifeScale;
            }

            // ====================================================
            // PHASE 3 — SHORT REST AFTER FIRST FLIP (0.95s - 1.20s)
            // ====================================================
            float landElapsed = 0f;
            float landDuration = Mathf.Max(0.04f, firstLandingDuration);
            while (landElapsed < landDuration)
            {
                landElapsed += Time.deltaTime;
                float lt = Mathf.Clamp01(landElapsed / landDuration);
                // Subtle cushion squash on landing: 1.00 -> 0.93 -> 1.00
                if (pillow != null)
                {
                    float squashY = lt < 0.5f 
                        ? Mathf.Lerp(1.00f, 0.93f, lt / 0.5f) 
                        : Mathf.Lerp(0.93f, 1.00f, (lt - 0.5f) / 0.5f);
                    pillow.transform.localScale = new Vector3(targetPillowScale.x, targetPillowScale.y * squashY, targetPillowScale.z);
                }
                yield return null;
            }
            if (pillow != null) pillow.transform.localScale = targetPillowScale;

            float remainingFirstRest = Mathf.Max(0f, firstRestDuration - landDuration);
            if (remainingFirstRest > 0f)
            {
                yield return new WaitForSeconds(remainingFirstRest);
            }

            // ====================================================
            // PHASE 4 — SECOND SMALLER KNIFE FLIP (1.20s - 1.65s)
            // ====================================================
            Vector3 secondApexPos = secondFlipApex != null 
                ? secondFlipApex.position 
                : restPos + new Vector3(secondFlipHorizontalOffset, secondFlipHeight, 0.03f);

            elapsed = 0f;
            float sDuration = Mathf.Max(0.1f, secondFlipDuration);

            while (elapsed < sDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / sDuration);

                if (knife != null)
                {
                    float posT = secondFlipPositionCurve != null ? secondFlipPositionCurve.Evaluate(t) : t;
                    float heightFactor = 4f * posT * (1f - posT);
                    knife.transform.position = restPos + (secondApexPos - restPos) * heightFactor;

                    float rotT = secondFlipRotationCurve != null ? secondFlipRotationCurve.Evaluate(t) : t;
                    float spinAngle = secondFlipSpinDegrees * rotT;
                    Quaternion spinRot = Quaternion.AngleAxis(spinAngle, flipAxis);
                    knife.transform.rotation = spinRot * restRot;
                }

                yield return null;
            }

            if (knife != null)
            {
                knife.transform.position = restPos;
                knife.transform.rotation = restRot;
                knife.transform.localScale = targetKnifeScale;
            }

            // ====================================================
            // PHASE 5 — FINAL LANDING / FINAL REST (1.65s - 1.90s)
            // ====================================================
            float settleElapsed = 0f;
            float settleSquashDuration = 0.10f;
            while (settleElapsed < settleSquashDuration)
            {
                settleElapsed += Time.deltaTime;
                float st = Mathf.Clamp01(settleElapsed / settleSquashDuration);
                // Soft final cushion squash: 1.00 -> 0.95 -> 1.00
                if (pillow != null)
                {
                    float squashY = st < 0.5f 
                        ? Mathf.Lerp(1.00f, 0.95f, st / 0.5f) 
                        : Mathf.Lerp(0.95f, 1.00f, (st - 0.5f) / 0.5f);
                    pillow.transform.localScale = new Vector3(targetPillowScale.x, targetPillowScale.y * squashY, targetPillowScale.z);
                }
                yield return null;
            }
            if (pillow != null) pillow.transform.localScale = targetPillowScale;

            float remainingSettle = Mathf.Max(0f, finalSettleDuration - settleSquashDuration);
            if (remainingSettle > 0f)
            {
                yield return new WaitForSeconds(remainingSettle);
            }

            // Ensure stable ceremonial rest pose
            if (knife != null)
            {
                knife.transform.position = restPos;
                knife.transform.rotation = restRot;
                knife.transform.localScale = targetKnifeScale;
            }

            // ====================================================
            // PHASE 6 — CELEBRATION PARTICLES (1.90s - 2.70s)
            // ====================================================
            if (confettiStartDelay > 0f)
            {
                yield return new WaitForSeconds(confettiStartDelay);
            }

            if (confettiLeft != null) confettiLeft.Play();
            if (confettiRight != null) confettiRight.Play();
            if (celebrationVFX != null) celebrationVFX.Play();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLevelComplete();
            }

            yield return new WaitForSeconds(confettiDuration);

            // ====================================================
            // PHASE 7 — LEVEL COMPLETE PANEL APPEARANCE (2.70s - 3.10s)
            // ====================================================
            if (levelCompletePanelDelay > 0f)
            {
                yield return new WaitForSeconds(levelCompletePanelDelay);
            }
        }
    }
}
