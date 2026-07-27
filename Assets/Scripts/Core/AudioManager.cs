using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BGM、SE、ボイスをScene間で共有する再生基盤。
/// 音源が未導入の場合は無音のまま安全に動作する。
/// </summary>
[DefaultExecutionOrder(-9000)]
public sealed class AudioManager : MonoBehaviour
{
    private const float DefaultFadeDuration = 0.35f;
    public const string MainBgmId = "Main";
    public const string BattleBgmId = "Battle";
    public const string TrainingBgmId = "Training";

    private static AudioManager instance;

    private AudioSource bgmSource;
    private AudioSource seSource;
    private AudioSource voiceSource;
    private AudioClip preparedVoiceClip;
    private Coroutine bgmTransition;

    public static AudioManager Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public AudioClip CurrentBgm => bgmSource != null ? bgmSource.clip : null;
    public AudioClip CurrentVoice => voiceSource != null ? voiceSource.clip : null;
    public bool HasPreparedVoice => preparedVoiceClip != null;
    public bool CanReplayCurrentVoice =>
        CanReplayVoice(HasPreparedVoice, GameOptionsManager.VoiceMuted);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        AudioManager existing = Object.FindObjectOfType<AudioManager>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject managerObject = new GameObject("AudioManager");
        instance = managerObject.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateAudioSources();
        ApplyCurrentOptions();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ApplyCurrentOptions()
    {
        CreateAudioSources();
        GameOptionsData options = GameOptionsManager.GetCurrent();
        bgmSource.volume = options.bgmMuted ? 0f : options.bgmVolume;
        seSource.volume = options.seMuted ? 0f : options.seVolume;
        voiceSource.volume = options.voiceMuted ? 0f : options.voiceVolume;
    }

    public static void ApplyCurrentOptionsIfAvailable()
    {
        if (instance != null)
        {
            instance.ApplyCurrentOptions();
        }
    }

    public void PlayBgm(AudioClip clip, float fadeDuration = DefaultFadeDuration)
    {
        CreateAudioSources();
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            ApplyCurrentOptions();
            return;
        }

        if (bgmTransition != null)
        {
            StopCoroutine(bgmTransition);
        }

        bgmTransition = StartCoroutine(
            TransitionBgm(clip, Mathf.Max(0f, fadeDuration)));
    }

    public void PlayBgmFromResources(
        string resourcePath,
        float fadeDuration = DefaultFadeDuration)
    {
        AudioClip clip = string.IsNullOrWhiteSpace(resourcePath)
            ? null
            : Resources.Load<AudioClip>(resourcePath);
        PlayBgm(clip, fadeDuration);
    }

    public void PlayBgmById(
        string bgmId,
        float fadeDuration = DefaultFadeDuration)
    {
        PlayBgmFromResources(BuildBgmResourcePath(bgmId), fadeDuration);
    }

    public static string BuildBgmResourcePath(string bgmId)
    {
        if (string.IsNullOrWhiteSpace(bgmId))
        {
            return string.Empty;
        }

        string normalizedBgmId = bgmId.Trim().Trim('/');
        return normalizedBgmId.StartsWith("Audio/Bgm/")
            ? normalizedBgmId
            : "Audio/Bgm/" + normalizedBgmId;
    }

    public void StopBgm(float fadeDuration = DefaultFadeDuration)
    {
        PlayBgm(null, fadeDuration);
    }

    public void PlaySe(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        CreateAudioSources();
        ApplyCurrentOptions();
        seSource.PlayOneShot(clip);
    }

    public void PlaySeFromResources(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return;
        }

        PlaySe(Resources.Load<AudioClip>(resourcePath));
    }

    public bool PlaySeById(string seId)
    {
        if (!CanPlaySe(seId, GameOptionsManager.SeMuted))
        {
            return false;
        }

        string resourcePath = BuildSeResourcePath(seId);
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            return false;
        }

        PlaySe(clip);
        return true;
    }

    public static bool CanPlaySe(string seId, bool seMuted)
    {
        return !seMuted && !string.IsNullOrWhiteSpace(seId);
    }

    public static string BuildSeResourcePath(string seId)
    {
        if (string.IsNullOrWhiteSpace(seId))
        {
            return string.Empty;
        }

        string normalizedSeId = seId.Trim().Trim('/');
        return normalizedSeId.StartsWith("Audio/SE/")
            ? normalizedSeId
            : "Audio/SE/" + normalizedSeId;
    }

    public bool PlayVoice(AudioClip clip, bool respectAutoPlay = true)
    {
        CreateAudioSources();
        voiceSource.Stop();
        voiceSource.clip = null;
        preparedVoiceClip = clip;

        GameOptionsData options = GameOptionsManager.GetCurrent();
        if (clip == null || (respectAutoPlay && !options.voiceAutoPlay))
        {
            return false;
        }

        voiceSource.volume = options.voiceMuted ? 0f : options.voiceVolume;
        voiceSource.clip = clip;
        voiceSource.Play();
        return true;
    }

    public bool PlayVoiceFromResources(
        string resourcePath,
        bool respectAutoPlay = true)
    {
        AudioClip clip = string.IsNullOrWhiteSpace(resourcePath)
            ? null
            : Resources.Load<AudioClip>(resourcePath);
        return PlayVoice(clip, respectAutoPlay);
    }

    public bool PlayVoiceById(
        string heroineId,
        string voiceId,
        bool respectAutoPlay = true)
    {
        return PlayVoiceFromResources(
            BuildVoiceResourcePath(heroineId, voiceId),
            respectAutoPlay);
    }

    public bool ReplayCurrentVoice()
    {
        if (!CanReplayCurrentVoice)
        {
            return false;
        }

        return PlayVoice(preparedVoiceClip, false);
    }

    public static bool CanReplayVoice(bool hasPreparedVoice, bool voiceMuted)
    {
        return hasPreparedVoice && !voiceMuted;
    }

    public static string BuildVoiceResourcePath(string heroineId, string voiceId)
    {
        if (string.IsNullOrWhiteSpace(voiceId))
        {
            return string.Empty;
        }

        string normalizedVoiceId = voiceId.Trim().Trim('/');
        if (normalizedVoiceId.StartsWith("Audio/Voice/"))
        {
            return normalizedVoiceId;
        }

        string normalizedHeroineId = string.IsNullOrWhiteSpace(heroineId)
            ? string.Empty
            : heroineId.Trim().Trim('/');
        return string.IsNullOrEmpty(normalizedHeroineId)
            ? "Audio/Voice/" + normalizedVoiceId
            : "Audio/Voice/" + normalizedHeroineId + "/" + normalizedVoiceId;
    }

    public void StopVoice()
    {
        if (voiceSource == null)
        {
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = null;
        preparedVoiceClip = null;
    }

    public static void StopVoiceIfAvailable()
    {
        if (instance != null)
        {
            instance.StopVoice();
        }
    }

    private void CreateAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }

        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
            seSource.playOnAwake = false;
            seSource.loop = false;
        }

        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
        }
    }

    private IEnumerator TransitionBgm(AudioClip nextClip, float duration)
    {
        float startVolume = bgmSource.volume;

        if (bgmSource.isPlaying && duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(
                    startVolume,
                    0f,
                    Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.clip = nextClip;

        if (nextClip == null)
        {
            bgmSource.volume = GetTargetBgmVolume();
            bgmTransition = null;
            yield break;
        }

        bgmSource.volume = duration > 0f ? 0f : GetTargetBgmVolume();
        bgmSource.Play();

        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(
                    0f,
                    GetTargetBgmVolume(),
                    Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
        }

        bgmSource.volume = GetTargetBgmVolume();
        bgmTransition = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBgmFromResources(GetSceneBgmResourcePath(scene.name));
        StartCoroutine(InstallUiSeAfterSceneLoad());
    }

    private IEnumerator InstallUiSeAfterSceneLoad()
    {
        // Scene内のStart()で生成されるボタンも対象にする。
        yield return null;
        UiSePlayer.InstallSceneButtons();
    }

    private static string GetSceneBgmResourcePath(string sceneName)
    {
        switch (sceneName)
        {
            case "TitleScene":
                return "Audio/Bgm/Title";
            case "MainScene":
                return BuildBgmResourcePath(MainBgmId);
            case "EndingScene":
                return "Audio/Bgm/Ending";
            default:
                return string.Empty;
        }
    }

    private static float GetTargetBgmVolume()
    {
        GameOptionsData options = GameOptionsManager.GetCurrent();
        return options.bgmMuted ? 0f : options.bgmVolume;
    }
}
