using System.Collections;
using UnityEngine;

namespace CosmicZoom
{
    /// <summary>
    /// Controls voice narrations generated via ElevenLabs and space sound effects.
    /// Handles audio ducking and smooth crossfades between stage transitions.
    /// </summary>
    public class CosmicAudioController : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource narrationSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("ElevenLabs Stage Narrations")]
        [SerializeField] private AudioClip narrationStage1;
        [SerializeField] private AudioClip narrationStage2;
        [SerializeField] private AudioClip narrationStage3;
        [SerializeField] private AudioClip narrationStage4;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip warpWhooshSFX;
        [SerializeField] private AudioClip ambientSpaceDrone;
        [SerializeField] private AudioClip uiPingSFX;

        public bool IsAudioMuted { get; private set; } = false;
        public bool IsNarratorAutoPlay { get; set; } = true;

        private Coroutine narrationRoutine;

        private void Awake()
        {
            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.volume = 0.35f;
            }
            if (narrationSource == null)
            {
                narrationSource = gameObject.AddComponent<AudioSource>();
                narrationSource.playOnAwake = false;
                narrationSource.volume = 1.0f;
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            if (ambientSpaceDrone != null)
            {
                ambientSource.clip = ambientSpaceDrone;
                ambientSource.Play();
            }
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
            narrationRoutine = StartCoroutine(CrossfadeNarration(clip));
        }

        private IEnumerator CrossfadeNarration(AudioClip newClip)
        {
            // Duck ambient sound while narration speaks
            float targetDuckedVol = 0.1f;
            ambientSource.volume = targetDuckedVol;

            narrationSource.Stop();
            narrationSource.clip = newClip;
            narrationSource.Play();

            while (narrationSource.isPlaying)
            {
                yield return null;
            }

            // Restore ambient volume
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.8f;
                ambientSource.volume = Mathf.Lerp(targetDuckedVol, 0.35f, t);
                yield return null;
            }
        }

        public void PlayWarpTransition()
        {
            if (IsAudioMuted || warpWhooshSFX == null) return;
            sfxSource.PlayOneShot(warpWhooshSFX, 0.85f);
        }

        public void PlayUIPing()
        {
            if (IsAudioMuted || uiPingSFX == null) return;
            sfxSource.PlayOneShot(uiPingSFX, 0.6f);
        }

        public void ToggleMute()
        {
            IsAudioMuted = !IsAudioMuted;
            ambientSource.mute = IsAudioMuted;
            narrationSource.mute = IsAudioMuted;
            sfxSource.mute = IsAudioMuted;
        }
    }
}
