// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Obevalon.AudioControlPanel.Platform.CoreAudio.Interop;

// Source-generated COM interfaces. Method order must match the native vtables exactly.
// Methods return HRESULTs natively; a failure HRESULT is thrown as an exception.

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
internal partial interface IMMDeviceEnumerator
{
    IMMDeviceCollection EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask);

    IMMDevice GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role);

    IMMDevice GetDevice(string id);

    void RegisterEndpointNotificationCallback(IMMNotificationClient client);

    void UnregisterEndpointNotificationCallback(IMMNotificationClient client);
}

[GeneratedComInterface]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
internal partial interface IMMDeviceCollection
{
    uint GetCount();

    IMMDevice Item(uint index);
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
internal partial interface IMMDevice
{
    /// <summary>Returns a raw interface pointer; wrap it with <see cref="ComHelper.Wrap{T}"/>.</summary>
    nint Activate(in Guid iid, uint clsCtx, nint activationParams);

    IPropertyStore OpenPropertyStore(uint storageAccess);

    string GetId();

    uint GetState();
}

[GeneratedComInterface]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
internal partial interface IPropertyStore
{
    uint GetCount();

    PropertyKey GetAt(uint index);

    void GetValue(in PropertyKey key, out PropVariant value);

    void SetValue(in PropertyKey key, in PropVariant value);

    void Commit();
}

/// <summary>Implemented by us to receive device notifications.</summary>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0")]
internal partial interface IMMNotificationClient
{
    void OnDeviceStateChanged(string deviceId, uint newState);

    void OnDeviceAdded(string deviceId);

    void OnDeviceRemoved(string deviceId);

    void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? defaultDeviceId);

    void OnPropertyValueChanged(string deviceId, PropertyKey key);
}

[GeneratedComInterface]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
internal partial interface IAudioEndpointVolume
{
    void RegisterControlChangeNotify(IAudioEndpointVolumeCallback callback);

    void UnregisterControlChangeNotify(IAudioEndpointVolumeCallback callback);

    uint GetChannelCount();

    void SetMasterVolumeLevel(float levelDb, in Guid eventContext);

    void SetMasterVolumeLevelScalar(float level, in Guid eventContext);

    float GetMasterVolumeLevel();

    float GetMasterVolumeLevelScalar();

    void SetChannelVolumeLevel(uint channel, float levelDb, in Guid eventContext);

    void SetChannelVolumeLevelScalar(uint channel, float level, in Guid eventContext);

    float GetChannelVolumeLevel(uint channel);

    float GetChannelVolumeLevelScalar(uint channel);

    void SetMute(int mute, in Guid eventContext);

    int GetMute();

    void GetVolumeStepInfo(out uint step, out uint stepCount);

    void VolumeStepUp(in Guid eventContext);

    void VolumeStepDown(in Guid eventContext);

    uint QueryHardwareSupport();

    void GetVolumeRange(out float minDb, out float maxDb, out float incrementDb);
}

/// <summary>Implemented by us to receive volume and mute changes.</summary>
[GeneratedComInterface]
[Guid("657804FA-D6AD-4496-8A60-352752AF4F89")]
internal partial interface IAudioEndpointVolumeCallback
{
    /// <param name="notifyData">Pointer to <see cref="AudioVolumeNotificationData"/>.</param>
    void OnNotify(nint notifyData);
}

/// <summary>
/// Undocumented interface used to change the default audio endpoint (Windows 7 – 11).
/// Only <see cref="SetDefaultEndpoint"/> is used; the other methods exist to keep the vtable layout
/// and are declared with opaque pointer parameters.
/// </summary>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
internal partial interface IPolicyConfig
{
    void GetMixFormat(string deviceId, nint format);

    void GetDeviceFormat(string deviceId, int useDefault, nint format);

    void ResetDeviceFormat(string deviceId);

    void SetDeviceFormat(string deviceId, nint endpointFormat, nint mixFormat);

    void GetProcessingPeriod(string deviceId, int useDefault, nint defaultPeriod, nint minimumPeriod);

    void SetProcessingPeriod(string deviceId, nint period);

    void GetShareMode(string deviceId, nint mode);

    void SetShareMode(string deviceId, nint mode);

    void GetPropertyValue(string deviceId, nint key, nint value);

    void SetPropertyValue(string deviceId, nint key, nint value);

    void SetDefaultEndpoint(string deviceId, ERole role);

    void SetEndpointVisibility(string deviceId, int visible);
}
