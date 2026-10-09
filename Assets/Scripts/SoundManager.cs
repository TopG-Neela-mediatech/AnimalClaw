using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.claw
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioMapper audioMapper;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private SfxSound[] sfxSounds;
        [SerializeField] private AudioSource animalSource;
        [SerializeField] private AnimalSound[] animalSounds;
        [SerializeField] private Button animalSoundButton;

        private Coroutine levelStartRoutine;

        private void Awake()
        {
            animalSoundButton.onClick.AddListener(OnAnimalSoundButtonClicked);
        }

        // Stops the level-start sequence, the animal sound and any voice-over. Music and SFX are left alone.
        public void StopVoiceAndAnimal()
        {
            StopLevelStartSequence();
            animalSource.Stop();
            RuntimeAudioLoader.Instance.StopCommonAudioSource();
        }

        public void PlaySfx(SfxType sfxType)
        {
            foreach (SfxSound sfxSound in sfxSounds)
            {
                if (sfxSound.sfxType != sfxType) continue;
                sfxSource.PlayOneShot(sfxSound.clip);
                return;
            }
            Debug.LogWarning("SoundManager: no SFX assigned for " + sfxType);
        }

        public float PlayIntroSlide(int slideIndex)
        {
            return RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioMapper.introSlides[slideIndex]);
        }
        public float PlayLevelIntro()
        {
            int rand = Random.Range(0, audioMapper.levelIntros.Length);
            return RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioMapper.levelIntros[rand]);
        }
        public float PlayLevelPrompt()
        {
            int rand = Random.Range(0, audioMapper.levelPrompts.Length);
            return RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioMapper.levelPrompts[rand]);
        }
        // "Yay! You found the <animal>!" — key is outro_<animal name in lowercase>.
        public float PlayAnimalOutro(Objects animal)
        {
            return RuntimeAudioLoader.Instance.PlayRuntimeAudio("outro_" + animal.ToString().ToLower());
        }
        public void PlayFinalOutro()
        {
            RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioMapper.outro);
        }

        // Level start: "Let's hear the sound" -> animal sound -> "Can you find it and press the button?"
        public void PlayLevelStartSequence(bool isTutorial)
        {
            StopLevelStartSequence();
            levelStartRoutine = StartCoroutine(LevelStartRoutine(isTutorial));
        }

        private IEnumerator LevelStartRoutine(bool isTutorial)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, PlayLevelIntro()));
            yield return new WaitForSeconds(Mathf.Max(0f, PlayAnimalSound()));
            if (isTutorial)
            {
                RuntimeAudioLoader.Instance.PlayRuntimeAudio(audioMapper.tutorialPrompt);
            }
            else
            {
                PlayLevelPrompt();
            }
            levelStartRoutine = null;
        }

        private void StopLevelStartSequence()
        {
            if (levelStartRoutine != null)
            {
                StopCoroutine(levelStartRoutine);
                levelStartRoutine = null;
            }
        }

        // Plays the current level's correct animal on its own source; returns the clip length (or -1).
        public float PlayAnimalSound()
        {
            Objects correct = GameManager.Instance.LevelManager.CorrectObject;
            foreach (AnimalSound animalSound in animalSounds)
            {
                if (animalSound.objectType != correct) continue;
                animalSource.clip = animalSound.clip;
                animalSource.Play();
                return animalSound.clip.length;
            }
            Debug.LogWarning("SoundManager: no animal sound assigned for " + correct);
            return -1f;
        }

        public void StopAnimalSound() => animalSource.Stop();

        // Replay button: only plays the animal sound, and is ignored while it is already playing.
        private void OnAnimalSoundButtonClicked()
        {
            if (animalSource.isPlaying) return;
            PlayAnimalSound();
        }
    }

    public enum SfxType
    {
        Correct,
        Incorrect
    }

    [System.Serializable]
    public class SfxSound
    {
        public SfxType sfxType;
        public AudioClip clip;
    }

    [System.Serializable]
    public class AnimalSound
    {
        public Objects objectType;
        public AudioClip clip;
    }

    [System.Serializable]
    public class AudioMapper
    {
        public string[] introSlides = { "slide1", "slide2", "slide3" };
        public string[] levelIntros = { "levelintro1", "levelintro2", "levelintro3" };
        public string[] levelPrompts = { "levelprompt1", "levelprompt2", "levelprompt3" };
        public string tutorialPrompt = "tutorialprompt";
        public string outro = "finaloutro";
    }
}
