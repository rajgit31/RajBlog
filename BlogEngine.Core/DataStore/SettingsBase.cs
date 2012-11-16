using System;
using System.Xml;

namespace BlogEngine.Core.DataStore
{
  /// <summary>
  /// Base class for extension settings
  /// </summary>
  public abstract class SettingsBase
  {
    #region Behaviors
    private ISettingsBehavior _settingsBehavior;
    /// <summary>
    /// Settings behavior
    /// </summary>
    public ISettingsBehavior SettingsBehavior
    {
      get
      {
        return _settingsBehavior;
      }
      set
      {
        _settingsBehavior = value;
      }
    }
    #endregion

    /// <summary>
    /// Default constructor
    /// </summary>
    public SettingsBase()
    {
    }

    ExtensionType _type = ExtensionType.Extension;
    /// <summary>
    /// Type of extension (extension, widget or theme)
    /// </summary>
    public ExtensionType ExType { get { return _type; } set { _type = value; } }

    string _settingId = string.Empty;
    /// <summary>
    /// Setting ID
    /// </summary>
    public string SettingID { get { return _settingId; } set { _settingId = value; } }

    /// <summary>
    /// Saves setting object to data storage
    /// </summary>
    /// <param name="settings">Settings</param>
    /// <returns>True if saved</returns>
    public bool SaveSettings(object settings)
    {
      return _settingsBehavior.SaveSettings(_type, _settingId, settings);
    }

    /// <summary>
    /// Get settings object from data storage
    /// </summary>
    /// <returns>Stream representing extension object</returns>
    public object GetSettings()
    {
      return _settingsBehavior.GetSettings(_type, _settingId);
    }
  }
}
