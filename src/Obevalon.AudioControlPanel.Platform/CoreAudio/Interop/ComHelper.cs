// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Obevalon.AudioControlPanel.Platform.CoreAudio.Interop;

internal static partial class ComHelper
{
    private static readonly StrategyBasedComWrappers Wrappers = new();

    /// <summary>Creates a COM object and returns its interface <typeparamref name="T"/>.</summary>
    public static T Create<T>(Guid clsid)
        where T : class
    {
        var iid = typeof(T).GUID;
        Marshal.ThrowExceptionForHR(CoCreateInstance(clsid, 0, ComConstants.ClsCtxInprocServer, iid, out var pointer));
        return Wrap<T>(pointer);
    }

    /// <summary>
    /// Wraps a raw interface pointer in a unique (non-cached) wrapper, so that <see cref="Release"/>
    /// frees it immediately, and releases the caller's reference to the pointer.
    /// </summary>
    public static T Wrap<T>(nint pointer)
        where T : class
    {
        try
        {
            return (T)Wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    /// <summary>Activates an interface on an audio endpoint, e.g. IAudioEndpointVolume.</summary>
    public static T Activate<T>(IMMDevice device)
        where T : class
    {
        var iid = typeof(T).GUID;
        return Wrap<T>(device.Activate(iid, ComConstants.ClsCtxAll, 0));
    }

    /// <summary>
    /// Releases a wrapper created by <see cref="Create{T}"/>, <see cref="Activate{T}"/> or <see cref="Wrap{T}"/>
    /// right away. Wrappers returned from generated COM calls are cached and shared; for them this is a no-op
    /// and the garbage collector releases them.
    /// </summary>
    public static void Release(object? comObject)
    {
        if (comObject is ComObject wrapper)
        {
            wrapper.FinalRelease();
        }
    }

    /// <summary>Reads a string property; returns null when it is missing or not a string.</summary>
    public static string? GetStringProperty(IPropertyStore store, PropertyKey key)
    {
        store.GetValue(key, out var value);
        try
        {
            return value.VarType == ComConstants.VtLpwstr && value.Value != 0
                ? Marshal.PtrToStringUni(value.Value)
                : null;
        }
        finally
        {
            _ = PropVariantClear(ref value);
        }
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid clsid, nint outer, uint clsContext, in Guid iid, out nint pointer);

    [LibraryImport("ole32.dll")]
    private static partial int PropVariantClear(ref PropVariant value);
}
