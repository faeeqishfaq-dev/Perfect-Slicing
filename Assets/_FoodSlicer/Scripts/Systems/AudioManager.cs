using System.Collections.Generic;
using UnityEngine;

namespace FoodSlicer
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] [Tooltip("Primary audio source for sound effects")]
        private AudioSource sfxSource;

        [SerializeField] [Tooltip("Secondary audio source for pitch-shifted slice sound effects")]
        private AudioSource sliceSource;

        [SerializeField] [Tooltip("Music audio source")]
        private AudioSource musicSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip whooshClip;
        [SerializeField] private AudioClip fruitSliceClip;
        [SerializeField] private AudioClip veggieChopClip;
        [SerializeField] private AudioClip cakeCutClip;
        [SerializeField] private AudioClip breadCutClip;
        [SerializeField] private AudioClip perfectSliceClip;
        [SerializeField] private AudioClip comboIncreaseClip;
        [SerializeField] private AudioClip boardHitClip;
        [SerializeField] private AudioClip levelCompleteClip;
        [SerializeField] private AudioClip levelFailClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip coinGainClip;

        [Header("Volume Controls")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.6f;

        private bool soundEnabled = true;
        private bool musicEnabled = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (sliceSource == null) sliceSource = gameObject.AddComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
            }

            // Ensure procedural clips exist if clips were not assigned
            EnsureProceduralClips();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (SaveManager.Instance != null)
            {
                soundEnabled = SaveManager.Instance.IsSoundEnabled();
                musicEnabled = SaveManager.Instance.IsMusicEnabled();
            }
            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            if (sfxSource != null) sfxSource.volume = soundEnabled ? (sfxVolume * masterVolume) : 0f;
            if (sliceSource != null) sliceSource.volume = soundEnabled ? (sfxVolume * masterVolume) : 0f;
            if (musicSource != null) musicSource.volume = musicEnabled ? (musicVolume * masterVolume) : 0f;
        }

        public void SetSoundEnabled(bool enabled)
        {
            soundEnabled = enabled;
            if (SaveManager.Instance != null) SaveManager.Instance.SetSoundEnabled(enabled);
            UpdateVolumes();
        }

        public void SetMusicEnabled(bool enabled)
        {
            musicEnabled = enabled;
            if (SaveManager.Instance != null) SaveManager.Instance.SetMusicEnabled(enabled);
            UpdateVolumes();
        }

        public bool IsSoundEnabled() => soundEnabled;
        public bool IsMusicEnabled() => musicEnabled;

        public void PlayWhoosh()
        {
            PlaySFX(whooshClip, Random.Range(0.95f, 1.05f), 0.7f);
        }

        public void PlaySlice(FoodType foodType)
        {
            AudioClip clip = fruitSliceClip;
            switch (foodType)
            {
                case FoodType.Carrot:
                case FoodType.BellPepper:
                case FoodType.Potato:
                    clip = veggieChopClip != null ? veggieChopClip : fruitSliceClip;
                    break;
                case FoodType.ChocolateCake:
                case FoodType.CreamCake:
                case FoodType.SwissRoll:
                    clip = cakeCutClip != null ? cakeCutClip : fruitSliceClip;
                    break;
                case FoodType.Bread:
                case FoodType.Pie:
                    clip = breadCutClip != null ? breadCutClip : fruitSliceClip;
                    break;
                default:
                    clip = fruitSliceClip;
                    break;
            }

            PlaySliceSound(clip, Random.Range(0.9f, 1.15f), 1f);
        }

        public void PlayPerfectSlice()
        {
            PlaySFX(perfectSliceClip, 1.1f, 1f);
        }

        public void PlayComboIncrease(int comboMultiplier)
        {
            float pitch = Mathf.Clamp(1.0f + (comboMultiplier * 0.1f), 1f, 1.8f);
            PlaySFX(comboIncreaseClip, pitch, 0.9f);
        }

        public void PlayBoardHit()
        {
            PlaySFX(boardHitClip, Random.Range(0.85f, 0.95f), 1f);
        }

        public void PlayLevelComplete()
        {
            PlaySFX(levelCompleteClip, 1f, 1f);
        }

        public void PlayLevelFail()
        {
            PlaySFX(levelFailClip, 1f, 1f);
        }

        public void PlayButtonClick()
        {
            PlaySFX(buttonClickClip, 1.2f, 0.8f);
        }

        public void PlayCoinGain()
        {
            PlaySFX(coinGainClip, Random.Range(1.0f, 1.25f), 0.9f);
        }

        private void PlaySFX(AudioClip clip, float pitch = 1f, float volume = 1f)
        {
            if (!soundEnabled || clip == null || sfxSource == null) return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume * masterVolume * sfxVolume);
        }

        private void PlaySliceSound(AudioClip clip, float pitch = 1f, float volume = 1f)
        {
            if (!soundEnabled || clip == null || sliceSource == null) return;
            sliceSource.pitch = pitch;
            sliceSource.PlayOneShot(clip, volume * masterVolume * sfxVolume);
        }

        // Procedural Audio Generation for immediate out-of-the-box working sound effects
        private void EnsureProceduralClips()
        {
            if (whooshClip == null) whooshClip = CreateNoiseClip("Whoosh", 0.12f, 300f, 80f);
            if (fruitSliceClip == null) fruitSliceClip = CreateToneClip("FruitSlice", 0.08f, 650f, 250f, 0.7f);
            if (veggieChopClip == null) veggieChopClip = CreateToneClip("VeggieChop", 0.09f, 380f, 140f, 0.8f);
            if (cakeCutClip == null) cakeCutClip = CreateToneClip("CakeCut", 0.07f, 520f, 300f, 0.5f);
            if (breadCutClip == null) breadCutClip = CreateToneClip("BreadCut", 0.08f, 440f, 180f, 0.7f);
            if (perfectSliceClip == null) perfectSliceClip = CreateHarmonicClip("PerfectSlice", 0.18f, new float[] { 880f, 1320f, 1760f });
            if (comboIncreaseClip == null) comboIncreaseClip = CreateHarmonicClip("ComboIncrease", 0.14f, new float[] { 587f, 880f });
            if (boardHitClip == null) boardHitClip = CreateToneClip("BoardHit", 0.15f, 160f, 60f, 1.0f);
            if (levelCompleteClip == null) levelCompleteClip = CreateHarmonicClip("LevelComplete", 0.45f, new float[] { 523f, 659f, 784f, 1046f });
            if (levelFailClip == null) levelFailClip = CreateToneClip("LevelFail", 0.35f, 350f, 120f, 0.9f);
            if (buttonClickClip == null) buttonClickClip = CreateToneClip("ButtonClick", 0.04f, 900f, 600f, 0.6f);
            if (coinGainClip == null) coinGainClip = CreateHarmonicClip("CoinGain", 0.12f, new float[] { 987f, 1318f });
        }

        private AudioClip CreateToneClip(string name, float duration, float startFreq, float endFreq, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float envelope = (1f - t);
                envelope = envelope * envelope; // exponential decay
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * (i / (float)sampleRate)) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateHarmonicClip(string name, float duration, float[] freqs)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Exp(-t * 5f);
                float sample = 0f;
                foreach (float f in freqs)
                {
                    sample += Mathf.Sin(2 * Mathf.PI * f * (i / (float)sampleRate));
                }
                data[i] = (sample / freqs.Length) * envelope * 0.8f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateNoiseClip(string name, float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Sin(t * Mathf.PI); // swelling then fading
                float noise = (Random.value * 2f - 1f) * 0.5f;
                float tone = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(startFreq, endFreq, t) * (i / (float)sampleRate)) * 0.5f;
                data[i] = (noise + tone) * envelope * 0.7f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
