﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using GitHubExtension.Widgets.COM;
using Microsoft.Windows.Widgets.Providers;
using WinRT;

namespace GitHubExtension.Widgets;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Templated class")]
public class WidgetProviderFactory<T> : IClassFactory
where T : IWidgetProvider
{
    private readonly Func<T> _createWidget;

    public WidgetProviderFactory(Func<T> createWidget)
    {
        _createWidget = createWidget;
    }

    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;

        if (pUnkOuter != IntPtr.Zero)
        {
            Marshal.ThrowExceptionForHR(CLASSENOAGGREGATION);
        }

        if (riid == typeof(T).GUID || riid == Guid.Parse(Guids.IUnknown))
        {
            // Create the instance of the .NET object
            ppvObject = MarshalInspectable<IWidgetProvider>.FromManaged(_createWidget());
        }
        else
        {
            // The object that ppvObject points to does not support the
            // interface identified by riid.
            Marshal.ThrowExceptionForHR(ENOINTERFACE);
        }

        return 0;
    }

    int IClassFactory.LockServer(bool fLock)
    {
        return 0;
    }

    private const int CLASSENOAGGREGATION = unchecked((int)0x80040110);
    private const int ENOINTERFACE = unchecked((int)0x80004002);
}
