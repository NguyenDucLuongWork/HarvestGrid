using UIGen;
using UnityEngine;
using UnityEngine.UIElements;

public class UISettingPanel : BindedPanel
{
    [SerializeField]
    public SettingViewSO so;

    protected override object DataSource => so != null ? so.setting : null;

    [UIAction("reset")]
    public void ResetDefaults()
    {
        if (so != null)
        {
            so.ResetDefaults();
            Refresh();
        }
    }

    protected override void OnBound()
    {
        base.OnBound();
        if (so != null)
        {
            so.LoadFromSystem();
            Refresh();
        }

        // Keep system audio levels synchronized when user moves the sliders
        RegisterSlider("in-masterAudioScale", v => { if (so != null) { so.setting.masterAudioScale = v; so.ApplyToSystem(); } });
        RegisterSlider("in-musicAudioScale", v => { if (so != null) { so.setting.musicAudioScale = v; so.ApplyToSystem(); } });
        RegisterSlider("in-sfxAudioScale", v => { if (so != null) { so.setting.sfxAudioScale = v; so.ApplyToSystem(); } });
        RegisterSlider("in-sens", v => { if (so != null) { so.setting.sens = v; } });
    }

    private void RegisterSlider(string sliderName, System.Action<float> onValueChanged)
    {
        if (root == null) return;
        var slider = root.Q<Slider>(sliderName);
        if (slider != null)
        {
            slider.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
        }
    }
}
