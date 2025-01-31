﻿namespace HelixToolkit.SharpDX.Animations;

public class AnimationGroupUpdater : IAnimationUpdater
{
    public string Name
    {
        set; get;
    } = string.Empty;

    private List<IAnimationUpdater> children = new();
    public IList<IAnimationUpdater> Children => children;

    private AnimationRepeatMode repeatMode = AnimationRepeatMode.PlayOnce;
    public AnimationRepeatMode RepeatMode
    {
        get => repeatMode;
        set
        {
            repeatMode = value;
            foreach (var updater in Children)
            {
                updater.RepeatMode = value;
            }
        }
    }

    public float StartTime { get; }

    public float EndTime { get; }

    public AnimationGroupUpdater(string name = StringHelper.EmptyStr)
    {
        Name = name;
    }

    public AnimationGroupUpdater(IEnumerable<IAnimationUpdater> updaters, string name = StringHelper.EmptyStr)
        : this(name)
    {
        children.AddRange(updaters);
        foreach (var updater in Children)
        {
            StartTime = Math.Min(StartTime, updater.StartTime);
            EndTime = Math.Max(EndTime, updater.EndTime);
        }
    }

    public void Reset()
    {
        foreach (var updater in Children)
        {
            updater.Reset();
        }
    }

    public void Update(float timeStamp, long frequency)
    {
        foreach (var updater in Children)
        {
            updater.Update(timeStamp, frequency);
        }
    }
}
