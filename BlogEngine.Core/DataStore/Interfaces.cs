using System;

namespace BlogEngine.Core.DataStore
{
  /// <summary>
  /// Public interfaces and enums for DataStore
  /// ISettingsBehavior incapsulates saving and retreaving
  /// settings objects to and from data storage
  /// </summary>
  public interface ISettingsBehavior
  {
    /// <summary>
    /// Save settings interface
    /// </summary>
    /// <param name="exType">Extensio Type</param>
    /// <param name="exId">Extensio Id</param>
    /// <param name="settings">Settings object</param>
    /// <returns>True if saved</returns>
    bool SaveSettings(ExtensionType exType, string exId, object settings);

    /// <summary>
    /// Get settings interface
    /// </summary>
    /// <param name="exType">Extension Type</param>
    /// <param name="exId">Extension Id</param>
    /// <returns>Settings object</returns>
    object GetSettings(ExtensionType exType, string exId);
  }

  /// <summary>
  /// Type of extension
  /// </summary>
  public enum ExtensionType 
  { 
    /// <summary>
    /// Extension
    /// </summary>
    Extension, 
    /// <summary>
    /// Widget
    /// </summary>
    Widget,
    /// <summary>
    /// Theme
    /// </summary>
    Theme 
  }
}
