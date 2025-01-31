﻿using HelixToolkit.SharpDX.Cameras;
using HelixToolkit.SharpDX.Model.Scene;
using HelixToolkit.SharpDX.Model.Scene2D;
using SharpDX;

namespace HelixToolkit.SharpDX;

/// <summary>
/// 
/// </summary>
public interface IViewport3DX : IDisposable
{
    /// <summary>
    /// Attaches the specified host.
    /// </summary>
    /// <param name="host">The host.</param>
    void Attach(IRenderHost host);

    /// <summary>
    /// Detaches this instance.
    /// </summary>
    void Detach();

    /// <summary>
    /// Gets the render host.
    /// </summary>
    /// <value>
    /// The render host.
    /// </value>
    IRenderHost? RenderHost
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether this instance is shadow mapping enabled.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is shadow mapping enabled; otherwise, <c>false</c>.
    /// </value>
    bool IsShadowMappingEnabled
    {
        get;
    }

    /// <summary>
    /// Gets or sets the effects manager.
    /// </summary>
    /// <value>
    /// The effects manager.
    /// </value>
    IEffectsManager? EffectsManager
    {
        set; get;
    }

    /// <summary>
    /// Gets the camera core.
    /// </summary>
    /// <value>
    /// The camera core.
    /// </value>
    CameraCore? CameraCore
    {
        get;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeStamp"></param>
    //DeferredRenderer DeferredRenderer { get; set; }
    void Update(TimeSpan timeStamp);

    /// <summary>
    /// Gets the renderables.
    /// </summary>
    /// <value>
    /// The renderables.
    /// </value>
    IEnumerable<SceneNode> Renderables
    {
        get;
    }

    /// <summary>
    /// Gets the d2 d renderables.
    /// </summary>
    /// <value>
    /// The d2 d renderables.
    /// </value>
    IEnumerable<SceneNode2D> D2DRenderables
    {
        get;
    }

    /// <summary>
    /// Invalidates the render.
    /// </summary>
    void InvalidateRender();

    /// <summary>
    /// Invalidates the scene graph.
    /// </summary>
    void InvalidateSceneGraph();

    /// <summary>
    /// Gets the viewport rectangle.
    /// </summary>
    /// <value>
    /// The viewport rectangle.
    /// </value>
    Rectangle ViewportRectangle
    {
        get;
    }
}
