﻿namespace HelixToolkit.SharpDX.Model;

/// <summary>
/// 
/// </summary>
public sealed class EffectAttributes : IEffectAttributes
{
    public string EffectName
    {
        private set; get;
    }
    private readonly Dictionary<string, object> attributes = new();
    /// <summary>
    /// Initializes a new instance of the <see cref="EffectAttributes"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    public EffectAttributes(string name)
    {
        EffectName = name;
    }
    /// <summary>
    /// Adds the attribute.
    /// </summary>
    /// <param name="attName">Name of the att.</param>
    /// <param name="parameter">The parameter.</param>
    public void AddAttribute(string attName, object parameter)
    {
        if (attributes.ContainsKey(attName))
        {
            return;
        }
        attributes.Add(attName, parameter);
    }
    /// <summary>
    /// Removes the attribute.
    /// </summary>
    /// <param name="attName">Name of the att.</param>
    public void RemoveAttribute(string attName)
    {
        attributes.Remove(attName);
    }
    /// <summary>
    /// Gets the attribute.
    /// </summary>
    /// <param name="attName">Name of the att.</param>
    /// <returns></returns>
    public object? GetAttribute(string attName)
    {
        if (attributes.TryGetValue(attName, out var obj))
        {
            return obj;
        }
        else
        {
            return null;
        }
    }
    /// <summary>
    /// Tries the get attribute.
    /// </summary>
    /// <param name="attName">Name of the att.</param>
    /// <param name="attribute">The attribute.</param>
    /// <returns></returns>
    public bool TryGetAttribute(string attName, out object? attribute)
    {
        return attributes.TryGetValue(attName, out attribute);
    }
    /// <summary>
    /// Parses the specified att string.
    /// </summary>
    /// <param name="attString">The att string.</param>
    /// <returns></returns>
    public static EffectAttributes[] Parse(string attString)
    {
        return Parse(attString, EffectParserConfiguration.Parser);
    }
    /// <summary>
    /// Parses the specified att string.
    /// </summary>
    /// <param name="attString">The att string.</param>
    /// <param name="parser">The parser.</param>
    /// <returns></returns>
    public static EffectAttributes[] Parse(string attString, IEffectAttributeParser parser)
    {
        return parser.Parse(attString);
    }
}
