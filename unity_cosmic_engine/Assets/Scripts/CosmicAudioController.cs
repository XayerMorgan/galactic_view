using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>Two-deck instrumental playlist, independent narration, and narration ducking.</summary>
    public class CosmicAudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource narrationSource, musicSourceA, musicSourceB, sfxSource;
        public AudioClip narrationStage1, narrationStage2, narrationStage3, narrationStage4;
        public AudioClip acousticMovement1, acousticMovement2, acousticMovement3, soothingAcoustic;
        public AudioClip[] additionalMusic = Array.Empty<AudioClip>();
        public string[] additionalMusicTitles = Array.Empty<string>();
        public AudioClip gentleChimeSFX, lightPulseVoice;
        public bool IsAudioMuted { get; private set; }
        public bool IsMusicEnabled { get; private set; } = true;
        public bool IsNarratorAutoPlay { get; set; } = true;
        public bool ShuffleMusic { get; private set; } = true;
        public int currentTrackIndex { get; private set; }
        public int CurrentPlayingStage { get; private set; }
        public bool IsNarrationPlaying => narrationSource != null && narrationSource.isPlaying;
        public int TrackCount => playlist.Count;
        public IReadOnlyList<string> TrackTitles => titles;
        public bool AllSourcesMuted => musicSourceA.mute && musicSourceB.mute && narrationSource.mute && sfxSource.mute;
        public float musicTargetVolume = 0.45f;
        private readonly List<AudioClip> playlist = new List<AudioClip>();
        private readonly List<string> titles = new List<string>();
        private readonly List<int> shuffleBag = new List<int>();
        private readonly System.Random random = new System.Random();
        private AudioSource active, outgoing;
        private float transition = 1, level, outgoingGain = 1;
        private bool crossfading, activeNeedsStart, trackHasStarted;

        private AudioSource Configure(AudioSource source, float volume)
        {
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            source.volume = volume;
            return source;
        }
        private void Awake()
        {
            narrationSource = Configure(narrationSource, 1);
            musicSourceA = Configure(musicSourceA, 0);
            musicSourceB = Configure(musicSourceB, 0);
            sfxSource = Configure(sfxSource, 0.8f);
            active = musicSourceA;
            outgoing = musicSourceB;
            musicTargetVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Cosmic_Music_Volume", 0.45f));
            ShuffleMusic = PlayerPrefs.GetInt("Cosmic_Music_Shuffle", 1) == 1;
        }
        private void Add(AudioClip clip, string title)
        {
            if (clip == null) return;
            playlist.Add(clip);
            titles.Add(title);
        }
        private void Start()
        {
            // Lead with the new suite; retain the four original acoustic movements for variety.
            for (int i = 0; i < additionalMusic.Length; i++)
                Add(additionalMusic[i], i < additionalMusicTitles.Length ? additionalMusicTitles[i] : additionalMusic[i].name);
            Add(acousticMovement1, "Orion Fingerstyle Serenade");
            Add(acousticMovement2, "Celestial Harp & Warm Resonance");
            Add(acousticMovement3, "Deep Cosmic Acoustic Harmonics");
            Add(soothingAcoustic, "Tranquil Starlight Odyssey");
            StartAcousticMusic();
            StartCoroutine(DelayedLaunchNarration());
        }
        private IEnumerator DelayedLaunchNarration()
        {
            yield return new WaitForSeconds(1);
            PlayStageNarration(1);
        }
        private void Update()
        {
            float target = IsNarrationPlaying ? Mathf.Min(0.08f, musicTargetVolume) : musicTargetVolume;
            level = Mathf.MoveTowards(level, target, Time.unscaledDeltaTime * 0.35f);
            if (!IsMusicEnabled)
            {
                // Streaming Play requests may finish loading after a same-frame
                // Pause. Enforce pause when that request reaches the audio thread.
                if (active.isPlaying) active.Pause();
                if (outgoing.isPlaying) outgoing.Pause();
                return;
            }
            if (crossfading)
            {
                transition = Mathf.Min(1, transition + Time.unscaledDeltaTime / 2.2f);
                active.volume = Mathf.Sin(transition * Mathf.PI / 2) * level;
                outgoing.volume = Mathf.Cos(transition * Mathf.PI / 2) * level * outgoingGain;
                if (transition >= 1) { outgoing.Stop(); outgoing.volume = 0; crossfading = false; }
            }
            else active.volume = level;
            if (active.isPlaying) trackHasStarted = true;
            // A streamed clip is briefly not playing while it opens. Only a
            // clip that actually began can be considered finished.
            if (active.clip != null && trackHasStarted && !crossfading && (!active.isPlaying || active.clip.length - active.time <= 2.2f)) NextTrack();
        }
        public string GetCurrentTrackTitle() => currentTrackIndex < titles.Count ? titles[currentTrackIndex] : "Space instrumental suite";
        public void StartAcousticMusic()
        {
            if (playlist.Count == 0 || !IsMusicEnabled) return;
            active.clip = playlist[currentTrackIndex];
            active.time = 0;
            active.volume = 0;
            level = 0;
            active.Play();
            activeNeedsStart = false;
            trackHasStarted = false;
        }
        public void NextTrack()
        {
            if (playlist.Count == 0) return;
            int next = (currentTrackIndex + 1) % playlist.Count;
            if (ShuffleMusic && playlist.Count > 1)
            {
                if (shuffleBag.Count == 0)
                {
                    for (int i = 0; i < playlist.Count; i++) if (i != currentTrackIndex) shuffleBag.Add(i);
                    for (int i = shuffleBag.Count - 1; i > 0; i--)
                    {
                        int j = random.Next(i + 1);
                        int value = shuffleBag[i]; shuffleBag[i] = shuffleBag[j]; shuffleBag[j] = value;
                    }
                }
                next = shuffleBag[shuffleBag.Count - 1];
                shuffleBag.RemoveAt(shuffleBag.Count - 1);
            }
            CrossfadeToTrack(next);
        }
        public void CrossfadeToTrack(int index)
        {
            if (index < 0 || index >= playlist.Count) return;
            // Repeated skips replace the older outgoing deck, never leave a third voice running.
            outgoing.Stop();
            outgoingGain = level > 0.001f ? active.volume / level : 0;
            var previous = active;
            active = outgoing;
            outgoing = previous;
            currentTrackIndex = index;
            active.clip = playlist[index];
            active.volume = 0;
            activeNeedsStart = !IsMusicEnabled;
            trackHasStarted = false;
            if (IsMusicEnabled) active.Play();
            transition = 0;
            crossfading = true;
            if (!IsMusicEnabled) outgoing.Pause();
        }
        public void SetMusicVolume(float value)
        {
            musicTargetVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("Cosmic_Music_Volume", musicTargetVolume);
        }
        public void ToggleShuffle()
        {
            ShuffleMusic = !ShuffleMusic;
            shuffleBag.Clear();
            PlayerPrefs.SetInt("Cosmic_Music_Shuffle", ShuffleMusic ? 1 : 0);
        }
        public void ToggleMusic()
        {
            IsMusicEnabled = !IsMusicEnabled;
            if (IsMusicEnabled)
            {
                trackHasStarted = false;
                if (activeNeedsStart) { active.Play(); activeNeedsStart = false; }
                else active.UnPause();
                if (crossfading) outgoing.UnPause();
            }
            else { active.Pause(); outgoing.Pause(); }
        }
        public void ToggleAudioMute()
        {
            IsAudioMuted = !IsAudioMuted;
            musicSourceA.mute = musicSourceB.mute = narrationSource.mute = sfxSource.mute = IsAudioMuted;
        }
        public void PlayStageNarration(int stageIndex)
        {
            if (IsAudioMuted || !IsNarratorAutoPlay || (IsNarrationPlaying && CurrentPlayingStage == stageIndex)) return;
            AudioClip clip = stageIndex == 1 ? narrationStage1 : stageIndex == 2 ? narrationStage2 : stageIndex == 3 ? narrationStage3 : stageIndex == 4 ? narrationStage4 : null;
            if (clip == null) return;
            CurrentPlayingStage = stageIndex;
            narrationSource.Stop();
            narrationSource.clip = clip;
            narrationSource.Play();
        }
        public void StopNarration() { narrationSource.Stop(); CurrentPlayingStage = 0; }
        public void ToggleNarrator() { IsNarratorAutoPlay = !IsNarratorAutoPlay; if (!IsNarratorAutoPlay) StopNarration(); }
        public void PlaySoftChime() { if (!IsAudioMuted && gentleChimeSFX != null) sfxSource.PlayOneShot(gentleChimeSFX, 0.7f); }
        public void PlayLightPulseVoice() { if (!IsAudioMuted && lightPulseVoice != null) sfxSource.PlayOneShot(lightPulseVoice, 0.9f); }
    }
}
