using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    public Slider menumusicSlider;
    public Slider menusfxSlider;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource[] sfxSources;

    private bool isUpdating = false; // tránh vòng lặp khi đồng bộ

    private void Start()
    {
        // Lấy giá trị đã lưu
        float savedMusicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float savedSFXVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Gán giá trị cho tất cả slider
        if (musicSlider != null) musicSlider.value = savedMusicVol;
        if (menumusicSlider != null) menumusicSlider.value = savedMusicVol;

        if (sfxSlider != null) sfxSlider.value = savedSFXVol;
        if (menusfxSlider != null) menusfxSlider.value = savedSFXVol;

        ApplyVolumes();

        // Đăng ký sự kiện thay đổi cho cả 4 slider
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        if (menumusicSlider != null) menumusicSlider.onValueChanged.AddListener(OnMenuMusicVolumeChanged);
        if (menusfxSlider != null) menusfxSlider.onValueChanged.AddListener(OnMenuSFXVolumeChanged);
    }

    // ================= MUSIC =================
    public void OnMusicVolumeChanged(float value)
    {
        if (isUpdating) return;
        isUpdating = true;

        PlayerPrefs.SetFloat("MusicVolume", value);
        if (menumusicSlider != null) menumusicSlider.value = value; // đồng bộ sang menu
        ApplyVolumes();

        isUpdating = false;
    }

    public void OnMenuMusicVolumeChanged(float value)
    {
        if (isUpdating) return;
        isUpdating = true;

        PlayerPrefs.SetFloat("MusicVolume", value);
        if (musicSlider != null) musicSlider.value = value; // đồng bộ sang gameplay
        ApplyVolumes();

        isUpdating = false;
    }

    // ================= SFX =================
    public void OnSFXVolumeChanged(float value)
    {
        if (isUpdating) return;
        isUpdating = true;

        PlayerPrefs.SetFloat("SFXVolume", value);
        if (menusfxSlider != null) menusfxSlider.value = value; // đồng bộ sang menu
        ApplyVolumes();

        isUpdating = false;
    }

    public void OnMenuSFXVolumeChanged(float value)
    {
        if (isUpdating) return;
        isUpdating = true;

        PlayerPrefs.SetFloat("SFXVolume", value);
        if (sfxSlider != null) sfxSlider.value = value; // đồng bộ sang gameplay
        ApplyVolumes();

        isUpdating = false;
    }

    // ================= APPLY =================
    private void ApplyVolumes()
    {
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (musicSource != null)
            musicSource.volume = musicVol;

        foreach (var s in sfxSources)
            if (s != null)
                s.volume = sfxVol;
    }
}
