// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;

namespace Obevalon.AudioControlPanel.Platform.CoreAudio.Interop;

// Native types from mmdeviceapi.h, endpointvolume.h, propsys.h and functiondiscoverykeys_devpkey.h.

internal enum EDataFlow
{
    Render = 0,
    Capture = 1,
    All = 2,
}

internal enum ERole
{
    Console = 0,
    Multimedia = 1,
    Communications = 2,
}

internal static class DeviceStates
{
    public const uint Active = 0x1;
    public const uint Disabled = 0x2;
    public const uint NotPresent = 0x4;
    public const uint Unplugged = 0x8;
}

internal static class ComConstants
{
    public const uint ClsCtxInprocServer = 0x1;
    public const uint ClsCtxAll = 0x17;
    public const uint StgmRead = 0x0;

    /// <summary>HRESULT_FROM_WIN32(ERROR_NOT_FOUND), returned when there is no default device.</summary>
    public const int ENotFound = unchecked((int)0x80070490);

    public const ushort VtLpwstr = 31;
}

internal static class ComIds
{
    public static readonly Guid MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");

    /// <summary>Undocumented CPolicyConfigClient (implements IPolicyConfig).</summary>
    public static readonly Guid PolicyConfigClient = new("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropertyKey(Guid formatId, uint propertyId) : IEquatable<PropertyKey>
{
    public Guid FormatId = formatId;
    public uint PropertyId = propertyId;

    public readonly bool Equals(PropertyKey other) => FormatId == other.FormatId && PropertyId == other.PropertyId;

    public override readonly bool Equals(object? obj) => obj is PropertyKey other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(FormatId, PropertyId);
}

internal static class PropertyKeys
{
    /// <summary>PKEY_Device_FriendlyName, e.g. "Speakers (Realtek(R) Audio)".</summary>
    public static readonly PropertyKey DeviceFriendlyName = new(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);

    /// <summary>PKEY_Device_DeviceDesc, e.g. "Speakers".</summary>
    public static readonly PropertyKey DeviceDescription = new(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 2);

    /// <summary>PKEY_DeviceInterface_FriendlyName, e.g. "Realtek(R) Audio".</summary>
    public static readonly PropertyKey DeviceInterfaceFriendlyName = new(new Guid("026E516E-B814-414B-83CD-856D6FEF4822"), 2);
}

/// <summary>PROPVARIANT (only the parts we read: the type and a pointer value).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct PropVariant
{
    public ushort VarType;
    public ushort Reserved1;
    public ushort Reserved2;
    public ushort Reserved3;
    public nint Value;
    public nint Value2;
}

/// <summary>AUDIO_VOLUME_NOTIFICATION_DATA header (channel volumes follow and are not needed).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioVolumeNotificationData
{
    public Guid EventContext;
    public int Muted;
    public float MasterVolume;
    public uint Channels;
}
