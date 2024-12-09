﻿using HelixToolkit.SharpDX;
#if WINUI
using Microsoft.UI.Input;
#else
using System.Windows;
using System.Windows.Input;
#endif

#if WINUI
namespace HelixToolkit.WinUI.SharpDX.Elements2D;
#else
namespace HelixToolkit.Wpf.SharpDX.Elements2D;
#endif

public class Mouse2DEventArgs : RoutedEventArgs
{
#if WINUI
    public RoutedEvent RoutedEvent
    {
        get; private set;
    }
#endif

    public HitTest2DResult? HitTest2DResult
    {
        get; private set;
    }

    public Viewport3DX? Viewport
    {
        get; private set;
    }

    public Point Position
    {
        get; private set;
    }

    public InputEventArgs? InputArgs
    {
        get; private set;
    }

    public Mouse2DEventArgs(RoutedEvent routedEvent, object? source, HitTest2DResult? hitTestResult, Point position, Viewport3DX? viewport = null, InputEventArgs? inputArgs = null)
#if WPF
        : base(routedEvent, source)
#endif
    {
#if WINUI
        this.RoutedEvent = routedEvent;
#endif

        this.HitTest2DResult = hitTestResult;
        this.Position = position;
        this.Viewport = viewport;
        InputArgs = inputArgs;
    }

    public Mouse2DEventArgs(RoutedEvent routedEvent, object? source, Viewport3DX? viewport = null)
#if WPF
        : base(routedEvent, source)
#endif
    {
#if WINUI
        this.RoutedEvent = routedEvent;
#endif

        this.Viewport = viewport;
    }
}
