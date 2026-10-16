using System.Collections;
using UnityEngine;

namespace FoodSlicer
{
    public class HitStopManager : MonoBehaviour
    {
        public static HitStopManager Instance { get; private set; }

        [Header("Default Hit Stop Settings")]
        [SerializeField] [Range(0.01f, 0.2f)] [Tooltip("Default hit-stop freeze duration in seconds")]
        private float defaultDuration = 0.035f;

        [SerializeField] [Range(0f, 0.5f)] [Tooltip("Time scale during hit-stop (0 is complete freeze)")]
        private float freezeTimeScale = 0.05f;

        private Coroutine hitStopCoroutine;
        private bool isPausedByGame = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Paused)
            {
                isPausedByGame = true;
                if (hitStopCoroutine != null)
                {
                    StopCoroutine(hitStopCoroutine);
                    hitStopCoroutine = null;
                }
                Time.timeScale = 0f;
            }
            else if (oldState == GameState.Paused && newState == GameState.Playing)
            {
                isPausedByGame = false;
                Time.timeScale = 1f;
            }
            else if (newState == GameState.LevelComplete || newState == GameState.LevelFailed)
            {
                isPausedByGame = false;
                if (hitStopCoroutine != null)
                {
                    StopCoroutine(hitStopCoroutine);
                    hitStopCoroutine = null;
                }
                Time.timeScale = 1f;
            }
        }

        public void RequestHitStop()
        {
            RequestHitStop(defaultDuration);
        }

        public void RequestHitStop(float duration)
        {
            if (!Application.isPlaying) return;
            if (isPausedByGame) return;
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying()) return;

            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }

            hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = freezeTimeScale;
            yield return new WaitForSecondsRealtime(duration);

            if (!isPausedByGame && (GameStateManager.Instance == null || GameStateManager.Instance.IsPlaying()))
            {
                Time.timeScale = 1f;
            }
            hitStopCoroutine = null;
        }
    }
}
