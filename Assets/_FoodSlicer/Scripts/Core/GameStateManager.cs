using System;
using UnityEngine;

namespace FoodSlicer
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] [Tooltip("Current state of the game loop")]
        private GameState currentState = GameState.Playing;

        public GameState CurrentState => currentState;

        public event Action<GameState, GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ResetState(GameState newState = GameState.Playing)
        {
            GameState oldState = currentState;
            currentState = newState;
            OnStateChanged?.Invoke(newState, oldState);
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            // Prevent transitions out of terminal states unless reloading/menu
            if ((currentState == GameState.LevelComplete || currentState == GameState.LevelFailed) &&
                (newState == GameState.Playing || newState == GameState.Paused))
            {
                return;
            }

            GameState oldState = currentState;
            currentState = newState;

            OnStateChanged?.Invoke(newState, oldState);
        }

        public bool IsPlaying() => currentState == GameState.Playing;
        public bool IsGameOver() => currentState == GameState.LevelComplete || currentState == GameState.LevelFailed;
    }
}
