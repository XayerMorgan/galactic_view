using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Controls voice narrations generated via ElevenLabs and the soothing acoustic space music score.
    /// Handles intelligent audio ducking, zero-pop volume ramps, and playlist crossfading.
    /// </summary>
    public class CosmicAudioController : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource narrationSource;
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;
        [SerializeField] private AudioSource sfxSource;

        [Header("ElevenLabs Stage Narrations")]
        public AudioClip narrationStage1;
        public AudioClip narrationStage2;
        public AudioClip narrationStage3;
        public AudioClip narrationStage4;

        [Header("Soothing Acoustic Space Music Suite")]
        public AudioClip acousticMovement1;
        public AudioClip acousticMovement2;
        public AudioClip acousticMovement3;
        public AudioClip soothingAcoustic;

        [Header("Soft Chime SFX")]
        public AudioClip gentleChimeSFX;
        public AudioClip lightPulseVoice;

        public bool IsAudioMuted { get; private set; } = false;
        public bool IsMusicEnabled { get; private set; } = true;
        public bool IsNarratorAutoPlay { get; set; } = true;

        private List<AudioClip> playlist = new List<AudioClip>();
        private List<string> playlistTitles = new List<string>();
        public int currentTrackIndex { get; private set; } = 0;

        private AudioSource activeMusicSource;
        private AudioSource inactiveMusicSource;
        private Coroutine narrationRoutine;
        private Coroutine musicCrossfadeRoutine;

        public float musicTargetVolume = 0.32f;
        private bool isDucked = false;

        private void Awake()
        {
            if (musicSourceA == null)
            {
                musicSourceA = gameObject.AddComponent<AudioSource>();
                musicSourceA.playOnAwake = false;
                musicSourceA.loop = false;
                musicSourceA.volume = 0f;
            }
            if (musicSourceB == null)
            {
                musicSourceB = gameObject.AddComponent<AudioSource>();
                musicSourceB.playOnAwake = false;
                musicSourceB.loop = false;
                musicSourceB.volume = 0f;
            }
            if (narrationSource == null)
            {
                narrationSource = gameObject.AddComponent<AudioSource>();
                narrationSource.playOnAwake = false;
                narrationSource.volume = 0.95f;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.volume = 0.6f;
            }

            activeMusicSource = musicSourceA;
            inactiveMusicSource = musicSourceB;
        }

        private void Start()
        {
            // Populate Acoustic Playlist
            if (acousticMovement1 != null) { playlist.Add(acousticMovement1); playlistTitles.Add("Movement I: Orion Fingerstyle Serenade"); }
            if (acousticMovement2 != null) { playlist.Add(acousticMovement2); playlistTitles.Add("Movement II: Celestial Harp & Warm Resonance"); }
            if (acousticMovement3 != null) { playlist.Add(acousticMovement3); playlistTitles.Add("Movement III: Deep Cosmic Acoustic Harmonics"); }
            if (soothingAcoustic != null) { playlist.Add(soothingAcoustic); playlistTitles.Add("Movement IV: Tranquil Starlight Odyssey"); }

            if (playlist.Count > 0 && IsMusicEnabled && !IsAudioMuted)
            {
                StartAcousticMusic();
            }
        }

        private void Update()
        {
            // Monitor active music source for seamless crossfade near track end
            if (IsMusicEnabled && !IsAudioMuted && activeMusicSource != null && activeMusicSource.isPlaying)
            {
                float remainingTime = activeMusicSource.clip.length - activeMusicSource.time;
                if (remainingTime <= 2.2f && musicCrossfadeRoutine == null)
                {
                    NextTrack();
                }
            }
        }

        public string GetCurrentTrackTitle()
        {
            if (playlistTitles.Count > currentTrackIndex)
                return playlistTitles[currentTrackIndex];
            return "Acoustic Space Suite";
        }

        public void StartAcousticMusic()
        {
            if (playlist.Count == 0) return;
            activeMusicSource.clip = playlist[currentTrackIndex];
            activeMusicSource.time = 0f;
            activeMusicSource.volume = 0f;
            activeMusicSource.Play();
            StartCoroutine(FadeSource(activeMusicSource, 0f, isDucked ? 0.08f : musicTargetVolume, 1.8f));
        }

        public void NextTrack()
        {
            if (playlist.Count == 0) return;
            int nextIndex = (currentTrackIndex + 1) % playlist.Count;
            CrossfadeToTrack(nextIndex);
        }

        public void CrossfadeToTrack(int index)
        {
            if (musicCrossfadeRoutine != null) StopCoroutine(musicCrossfadeRoutine);
            musicCrossfadeRoutine = StartCoroutine(CrossfadeMusicRoutine(index));
        }

        private IEnumerator CrossfadeMusicRoutine(int nextIndex)
        {
            currentTrackIndex = nextIndex;
            inactiveMusicSource.clip = playlist[nextIndex];
            inactiveMusicSource.time = 0f;
            inactiveMusicSource.volume = 0f;
            inactiveMusicSource.Play();

            float targetVol = isDucked ? 0.08f : musicTargetVolume;
            float duration = 2.2f;
            float elapsed = 0f;

            float fromVol = activeMusicSource.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);

                inactiveMusicSource.volume = Mathf.Lerp(0f, targetVol, ease);
                activeMusicSource.volume = Mathf.Lerp(fromVol, 0f, ease);
                yield return null;
            }

            activeMusicSource.Stop();
            activeMusicSource.volume = 0f;

            // Swap decks
            AudioSource temp = activeMusicSource;
            activeMusicSource = inactiveMusicSource;
            inactiveMusicSource = temp;

            musicCrossfadeRoutine = null;
        }

        public void PlayStageNarration(int stageIndex)
        {
            if (IsAudioMuted || !IsNarratorAutoPlay) return;

            AudioClip clip = stageIndex switch
            {
                1 => narrationStage1,
                2 => narrationStage2,
                3 => narrationStage3,
                4 => narrationStage4,
                _ => null
            };

            if (clip == null) return;

            if (narrationRoutine != null) StopCoroutine(narrationRoutine);
            narrationRoutine = StartCoroutine(NarrationSequence(clip));
        }

        private IEnumerator NarrationSequence(AudioClip clip)
        {
            // Duck acoustic music gracefully
            isDucked = true;
            if (activeMusicSource != null && activeMusicSource.isPlaying)
            {
                StartCoroutine(FadeSource(activeMusicSource, activeMusicSource.volume, 0.08f, 0.6f));
            }

            narrationSource.Stop();
            narrationSource.clip = clip;
            narrationSource.Play();

            while (narrationSource.isPlaying)
            {
                yield return null;
            }

            // Unduck acoustic music back to target volume
            isDucked = false;
            if (activeMusicSource != null && activeMusicSource.isPlaying)
            {
                StartCoroutine(FadeSource(activeMusicSource, activeMusicSource.volume, musicTargetVolume, 1.2f));
            }

            narrationRoutine = null;
        }

        private IEnumerator FadeSource(AudioSource src, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
                src.volume = Mathf.Lerp(from, to, ease);
                yield return null;
            }
            src.volume = to;
        }

        public void PlaySoftChime()
        {
            if (IsAudioMuted || sfxSource == null || gentleChimeSFX == null) return;
            sfxSource.PlayOneShot(gentleChimeSFX, 0.7f);
        }

        public void PlayLightPulseVoice()
        {
            if (IsAudioMuted || sfxSource == null || lightPulseVoice == null) return;
            sfxSource.PlayOneShot(lightPulseVoice, 0.9f);
        }

        public void ToggleAudioMute()
        {
            IsAudioMuted = !IsAudioMuted;
            if (IsAudioMuted)
            {
                if (activeMusicSource != null) activeMusicSource.Pause();
                if (narrationSource != null) narrationSource.Pause();
            }
            else
            {
                if (IsMusicEnabled && activeMusicSource != null) activeMusicSource.UnPause();
            }
        }

        public void ToggleMusic()
        {
            IsMusicEnabled = !IsMusicEnabled;
            if (IsMusicEnabled)
            {
                if (activeMusicSource != null && !activeMusicSource.isPlaying)
                {
                    StartAcousticMusic();
                }
                else if (activeMusicSource != null)
                {
                    activeMusicSource.UnPause();
                }
            }
            else
            {
                if (activeMusicSource != null) activeMusicSource.Pause();
            }
        }

        public void ToggleNarrator()
        {
            IsNarratorAutoPlay = !IsNarratorAutoPlay;
            if (!IsNarratorAutoPlay && narrationSource != null && narrationSource.isPlaying)
            {
                narrationSource.Stop();
                isDucked = false;
                if (activeMusicSource != null) activeMusicSource.volume = musicTargetVolume;
            }
        }
    }
}
