﻿using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Shaders;
using SharpDX.Direct3D11;

namespace HelixToolkit.SharpDX.Model;

public sealed class BillboardMaterialCore : MaterialCore, IBillboardRenderParams
{
    private bool fixedSize = true;
    /// <summary>
    /// Gets or sets a value indicating whether [fixed size].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [fixed size]; otherwise, <c>false</c>.
    /// </value>
    public bool FixedSize
    {
        set
        {
            Set(ref fixedSize, value);
        }
        get
        {
            return fixedSize;
        }
    }

    private BillboardType type = BillboardType.SingleText;
    public BillboardType Type
    {
        set
        {
            Set(ref type, value);
        }
        get
        {
            return type;
        }
    }

    private SamplerStateDescription samplerDescription = DefaultSamplers.LinearSamplerClampAni1;
    /// <summary>
    /// Billboard texture sampler description
    /// </summary>
    public SamplerStateDescription SamplerDescription
    {
        set
        {
            Set(ref samplerDescription, value);
        }
        get
        {
            return samplerDescription;
        }
    }

    public override MaterialVariable CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique)
    {
        return new BillboardMaterialVariable(manager, technique, this);
    }
}
