using MajdataEdit_Neo.Assets.Langs;
using System.ComponentModel.DataAnnotations;

namespace MajdataEdit_Neo.Types.MajSetting;

public class MajVolumeSetting
{
    [Display(Name = nameof(Langs.Set_Answer))]
    [SettingControl(SettingControlType.Slider, Max = 1, Min = 0, Step = 0.01)]
    public float Answer { get; set; } = 0.8f;

    [Display(Name = nameof(Langs.Set_Break))]
    [SettingControl(SettingControlType.Slider, Max = 1, Min = 0, Step = 0.01)]
    public float Break { get; set; } = 0.7f;

    [Display(Name = nameof(Langs.Set_Slide))]
    [SettingControl(SettingControlType.Slider, Max = 1, Min = 0, Step = 0.01)]
    public float Slide { get; set; } = 0.3f;

    [Display(Name = nameof(Langs.Set_Tap))]
    [SettingControl(SettingControlType.Slider, Max = 1, Min = 0, Step = 0.01)]
    public float Tap { get; set; } = 0.45f;

    [Display(Name = nameof(Langs.Set_Touch))]
    [SettingControl(SettingControlType.Slider, Max = 1, Min = 0, Step = 0.01)]
    public float Touch { get; set; } = 0.7f;

    [Display(Name = nameof(Langs.Set_Track))]
    [SettingControl(SettingControlType.Slider, Max = 1, Min = 0, Step = 0.01)]
    public float Track { get; set; } = 0.9f;
}
