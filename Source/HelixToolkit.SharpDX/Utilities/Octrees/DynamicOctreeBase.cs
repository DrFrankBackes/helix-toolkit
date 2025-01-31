﻿using Microsoft.Extensions.Logging;
using SharpDX;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HelixToolkit.SharpDX;

/// <summary>
/// Base class template implementation for <see cref="IDynamicOctree"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class DynamicOctreeBase<T> : IDynamicOctree
{
    private static readonly ILogger logger = Logger.LogManager.Create<DynamicOctreeBase<T>>();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bound"></param>
    /// <param name="objects"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public delegate IDynamicOctree CreateNodeDelegate(ref BoundingBox bound, List<T> objects, IDynamicOctree parent);
    /// <summary>
    /// internal stack for tree traversal
    /// </summary>
    protected readonly Stack<KeyValuePair<int, IDynamicOctree[]>> stack;
    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<EventArgs>? Hit;
    /// <summary>
    /// The minumum size for enclosing region is a 1x1x1 cube.
    /// </summary>
    public float MIN_SIZE
    {
        get
        {
            return Parameter.MinimumOctantSize;
        }
    }
    /// <summary>
    /// <see cref="IOctreeBasic.TreeBuilt"/>
    /// </summary>
    public bool TreeBuilt
    {
        get
        {
            return treeBuilt;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    protected bool treeBuilt = false;       //there is no pre-existing tree yet.
    /// <summary>
    /// <see cref="IOctreeBasic.Parameter"/>
    /// </summary>
    public OctreeBuildParameter Parameter
    {
        private set; get;
    }

    private BoundingBox bound;

    /// <summary>
    /// <see cref="IOctreeBasic.Bound"/>
    /// </summary>
    public BoundingBox Bound
    {
        protected set
        {
            if (bound == value)
            {
                return;
            }
            bound = value;
            octants = CreateOctants(ref value, MIN_SIZE);
        }
        get
        {
            return bound;
        }
    }
    /// <summary>
    /// <see cref="DynamicOctreeBase{T}.Objects"/>
    /// </summary>
    public List<T>? Objects
    {
        protected set; get;
    }

    private readonly List<BoundingBox> hitPathBoundingBoxes = new();
    /// <summary>
    /// <see cref="IOctreeBasic.HitPathBoundingBoxes"/>
    /// </summary>
    public IList<BoundingBox> HitPathBoundingBoxes
    {
        get
        {
            return hitPathBoundingBoxes.AsReadOnly();
        }
    }
    /// <summary>
    /// These are all of the possible child octants for this node in the tree.
    /// </summary>
    private readonly IDynamicOctree[] childNodes = new IDynamicOctree[8];
    /// <summary>
    /// <see cref="IDynamicOctree.ChildNodes"/>
    /// </summary>
    public IDynamicOctree?[] ChildNodes
    {
        get
        {
            return childNodes;
        }
    }
    /// <summary>
    /// <see cref="IDynamicOctree.ActiveNodes"/>
    /// </summary>
    public byte ActiveNodes
    {
        set; get;
    }
    /// <summary>
    /// <see cref="IDynamicOctree.Parent"/>
    /// </summary>
    public IDynamicOctree? Parent
    {
        set; get;
    }

    private BoundingBox[]? octants = null;
    /// <summary>
    /// <see cref="IDynamicOctree.Octants"/>
    /// </summary>
    public BoundingBox[]? Octants
    {
        get
        {
            return octants;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    protected List<HitTestResult> modelHits = new();
    /// <summary>
    /// Gets the self array.
    /// </summary>
    /// <value>
    /// The self array.
    /// </value>
    public IDynamicOctree?[] SelfArray
    {
        get; private set;
    }
    /// <summary>
    /// Delete the octant if there is no object or child octant inside it.
    /// </summary>
    public bool AutoDeleteIfEmpty
    {
        get
        {
            return Parameter.AutoDeleteIfEmpty;
        }
        set
        {
            Parameter.AutoDeleteIfEmpty = value;
        }
    }

    private DynamicOctreeBase(OctreeBuildParameter? parameter, Stack<KeyValuePair<int, IDynamicOctree[]>>? stackCache)
    {
        SelfArray = new IDynamicOctree[] { this };
        stack = stackCache ?? new Stack<KeyValuePair<int, IDynamicOctree[]>>(64);

        if (stackCache == null && logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Stack cache is null");
        }
        if (parameter != null)
            Parameter = parameter;
        else
            Parameter = new OctreeBuildParameter();
    }


    /// <summary>
    /// Creates an oct tree which encloses the given region and contains the provided objects.
    /// </summary>
    /// <param name="bound">The bounding region for the oct tree.</param>
    /// <param name="objList">The list of objects contained within the bounding region</param>
    /// <param name="parent"></param>
    /// <param name="parameter"></param>
    /// <param name="stackCache"></param>
    protected DynamicOctreeBase(ref BoundingBox bound, List<T>? objList, IDynamicOctree? parent, OctreeBuildParameter? parameter, Stack<KeyValuePair<int, IDynamicOctree[]>>? stackCache)
        : this(parameter, stackCache)
    {
        Bound = bound;
        Objects = objList;
        Parent = parent;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="parameter"></param>
    /// <param name="stackCache"></param>
    protected DynamicOctreeBase(IDynamicOctree? parent, OctreeBuildParameter? parameter, Stack<KeyValuePair<int, IDynamicOctree[]>>? stackCache)
        : this(parameter, stackCache)
    {
        Objects = new List<T>();
        Bound = new BoundingBox(Vector3.Zero, Vector3.Zero);
        Parent = parent;
    }

    /// <summary>
    /// Creates an octTree with a suggestion for the bounding region containing the items.
    /// </summary>
    /// <param name="bound">The suggested dimensions for the bounding region. 
    /// Note: if items are outside this region, the region will be automatically resized.</param>
    /// <param name="parent"></param>
    /// <param name="parameter"></param>
    /// <param name="stackCache"></param>
    protected DynamicOctreeBase(ref BoundingBox bound, IDynamicOctree? parent, OctreeBuildParameter? parameter, Stack<KeyValuePair<int, IDynamicOctree[]>>? stackCache)
        : this(parent, parameter, stackCache)
    {
        Bound = bound;
    }

    private IDynamicOctree CreateNode(ref BoundingBox bound, List<T> objList)
    {
        return CreateNodeWithParent(ref bound, objList, this);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bound"></param>
    /// <param name="objList"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    protected abstract IDynamicOctree CreateNodeWithParent(ref BoundingBox bound, List<T> objList, IDynamicOctree parent);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bound"></param>
    /// <param name="Item"></param>
    /// <returns></returns>
    protected IDynamicOctree CreateNode(ref BoundingBox bound, T Item)
    {
        return CreateNode(ref bound, new List<T> { Item });
    }
    /// <summary>
    /// Build the octree
    /// </summary>
    public virtual void BuildTree()
    {
        if (Bound.Maximum == Bound.Minimum || !CheckDimension())
        {
            treeBuilt = false;
            return;
        }
        if (Parameter.Cubify)
        {
            Bound = FindEnclosingCube(ref bound);
        }
        BuildTree(this, this.stack);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="root"></param>
    /// <param name="stack"></param>
    public void BuildTree(IDynamicOctree root, Stack<KeyValuePair<int, IDynamicOctree[]>> stack)
    {
#if DEBUG
        var now = Stopwatch.GetTimestamp();
#endif
        TreeTraversal(root, stack, null, (node) => { node.BuildCurretNodeOnly(); }, null, Parameter.EnableParallelBuild);
#if DEBUG
        var elapsed = Stopwatch.GetTimestamp() - now;
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Buildtree time = {0} ms", (elapsed * 1e3) / Stopwatch.Frequency);
#endif
    }
    /// <summary>
    /// Common function to traverse the tree
    /// </summary>
    /// <param name="root"></param>
    /// <param name="stack"></param>
    /// <param name="criteria"></param>
    /// <param name="process"></param>
    /// <param name="breakCriteria"></param>
    /// <param name="useParallel"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TreeTraversal(IDynamicOctree root, Stack<KeyValuePair<int, IDynamicOctree[]>> stack, Func<IDynamicOctree, bool>? criteria, Action<IDynamicOctree> process,
        Func<bool>? breakCriteria = null, bool useParallel = false)
    {
        if (useParallel)
        {
            if (criteria == null || criteria(root))
            {
                process(root);
                if (breakCriteria != null && breakCriteria())
                {
                    return;
                }
                if (root.HasChildren)
                {
                    Parallel.ForEach(root.ChildNodes, (subTree) =>
                    {
                        if (subTree == null)
                        {
                            return;
                        }
                        TreeTraversal(subTree, new Stack<KeyValuePair<int, IDynamicOctree[]>>(), criteria, process, breakCriteria, false);
                    });
                }
            }
        }
        else
        {
            var i = -1;
            var treeArray = root.SelfArray;
            while (true)
            {
                while (++i < treeArray.Length)
                {
                    var tree = treeArray[i];
                    if (tree != null && (criteria == null || criteria(tree)))
                    {
                        process(tree);
                        if (breakCriteria != null && breakCriteria())
                        {
                            break;
                        }
                        if (tree.HasChildren)
                        {
                            stack.Push(new KeyValuePair<int, IDynamicOctree[]>(i, treeArray!));
                            treeArray = tree.ChildNodes;
                            i = -1;
                        }
                    }
                }
                if (stack.Count == 0)
                {
                    break;
                }
                var pair = stack.Pop();
                i = pair.Key;
                treeArray = pair.Value;
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public void BuildCurretNodeOnly()
    {
        /*I think I can just directly insert items into the tree instead of using a stack.*/
        if (!treeBuilt)
        {            //terminate the recursion if we're a leaf node
            if (Objects is null || Objects.Count <= 1)   //doubt: is this really right? needs testing.
            {
                treeBuilt = true;
                return;
            }
            BuildSubTree();
            treeBuilt = true;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="box"></param>
    /// <param name="minSize"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoundingBox[] CreateOctants(ref BoundingBox box, float minSize)
    {
        var dimensions = box.Maximum - box.Minimum;
        if (dimensions == Vector3.Zero || (dimensions.X < minSize && dimensions.Y < minSize && dimensions.Z < minSize))
        {
            return Array.Empty<BoundingBox>();
        }
        var half = dimensions / 2.0f;
        var center = box.Minimum + half;
        var minimum = box.Minimum;
        var maximum = box.Maximum;
        //Create subdivided regions for each octant
        return new BoundingBox[8] {
                new BoundingBox(minimum, center),
                new BoundingBox(new Vector3(center.X, minimum.Y, minimum.Z), new Vector3(maximum.X, center.Y, center.Z)),
                new BoundingBox(new Vector3(center.X, minimum.Y, center.Z), new Vector3(maximum.X, center.Y, maximum.Z)),
                new BoundingBox(new Vector3(minimum.X, minimum.Y, center.Z), new Vector3(center.X, center.Y, maximum.Z)),
                new BoundingBox(new Vector3(minimum.X, center.Y, minimum.Z), new Vector3(center.X, maximum.Y, center.Z)),
                new BoundingBox(new Vector3(center.X, center.Y, minimum.Z), new Vector3(maximum.X, maximum.Y, center.Z)),
                new BoundingBox(center, maximum),
                new BoundingBox(new Vector3(minimum.X, center.Y, center.Z), new Vector3(center.X, maximum.Y, maximum.Z))
                };
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckDimension()
    {
        var dimensions = Bound.Maximum - Bound.Minimum;

        if (dimensions == Vector3.Zero)
        {
            Bound = FindEnclosingBox();
        }
        dimensions = Bound.Maximum - Bound.Minimum;
        //Check to see if the dimensions of the box are greater than the minimum dimensions
        if (dimensions.X < MIN_SIZE && dimensions.Y < MIN_SIZE && dimensions.Z < MIN_SIZE)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    /// <summary>
    /// Build sub tree nodes
    /// </summary>
    protected virtual void BuildSubTree()
    {
        if (!CheckDimension() || Octants is null || Objects is null || Objects.Count < this.Parameter.MinObjectSizeToSplit)
        {
            treeBuilt = true;
            return;
        }

        //This will contain all of our objects which fit within each respective octant.
        var octList = new List<T>[8];
        for (var i = 0; i < 8; ++i)
            octList[i] = new List<T>(Objects.Count / 8);

        var count = Objects.Count;
        for (var i = Objects.Count - 1; i >= 0; --i)
        {
            var obj = Objects[i];
            for (var x = 0; x < 8; ++x)
            {
                if (IsContains(Octants[x], obj))
                {
                    octList[x].Add(obj);
                    Objects[i] = Objects[--count]; //Disard the existing object from location i, replaced with last valid object.
                    break;
                }
            }
        }

        Objects.RemoveRange(count, Objects.Count - count);
        Objects.TrimExcess();

        //Create child nodes where there are items contained in the bounding region
        for (var i = 0; i < 8; ++i)
        {
            if (octList[i].Count != 0)
            {
                ChildNodes[i] = CreateNode(ref octants![i], octList[i]);
                ActiveNodes |= (byte)(1 << i);
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    protected abstract BoundingBox GetBoundingBoxFromItem(T item);

    /// <summary>
    /// This finds the dimensions of the bounding box necessary to tightly enclose all items in the object list.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected BoundingBox FindEnclosingBox()
    {
        if (Objects is null || Objects.Count == 0)
        {
            return Bound;
        }
        var b = GetBoundingBoxFromItem(Objects[0]);
        for (var i = 0; i < Objects.Count; ++i)
        {
            var bound = GetBoundingBoxFromItem(Objects[i]);
            BoundingBox.Merge(ref b, ref bound, out b);
        }
        return b;
    }
    /// <summary>
    /// This finds the smallest enclosing cube which is a power of 2, for all objects in the list.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoundingBox FindEnclosingCube(ref BoundingBox bound)
    {
        var v = (bound.Maximum - bound.Minimum) / 2 + bound.Minimum;
        bound = new BoundingBox(bound.Minimum - v, bound.Maximum - v);
        var max = Math.Max(bound.Maximum.X, Math.Max(bound.Maximum.Y, bound.Maximum.Z));
        return new BoundingBox(new Vector3(-max, -max, -max) + v, new Vector3(max, max, max) + v);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static int SigBit(int x)
    {
        if (x >= 0)
        {
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(x) / Math.Log(2)));
        }
        else
        {
            x = Math.Abs(x);
            return -(int)Math.Pow(2, Math.Ceiling(Math.Log(x) / Math.Log(2)));
        }
    }
    /// <summary>
    /// <see cref="IDynamicOctree.Clear"/>
    /// </summary>
    public virtual void Clear()
    {
        Objects?.Clear();
        for (var i = 0; i < ChildNodes.Length; ++i)
        {
            ChildNodes[i]?.Clear();
        }
        Array.Clear(ChildNodes, 0, ChildNodes.Length);
    }
    /// <summary>
    ///
    /// </summary>
    /// <param name="context"></param>
    /// <param name="model"></param>
    /// <param name="geometry"></param>
    /// <param name="modelMatrix"></param>
    /// <param name="hits"></param>
    /// <returns></returns>
    public bool HitTest(HitTestContext? context, object? model, Geometry3D? geometry, Matrix modelMatrix, ref List<HitTestResult> hits)
    {
        return HitTest(context, model, geometry, modelMatrix, ref hits, 0);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="model">The model.</param>
    /// <param name="geometry">The geometry.</param>
    /// <param name="modelMatrix">The model matrix.</param>
    /// <param name="returnMultiple">Not used</param>
    /// <param name="hits">The hits.</param>
    /// <returns></returns>
    public bool HitTest(HitTestContext? context, object? model, Geometry3D? geometry, Matrix modelMatrix,
        bool returnMultiple, ref List<HitTestResult> hits)
    {
        return HitTest(context, model, geometry, modelMatrix, ref hits, 0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="model"></param>
    /// <param name="geometry"></param>
    /// <param name="modelMatrix"></param>
    /// <param name="hits"></param>
    /// <param name="hitThickness"></param>
    /// <returns></returns>
    public virtual bool HitTest(HitTestContext? context, object? model, Geometry3D? geometry, Matrix modelMatrix,
        ref List<HitTestResult> hits, float hitThickness)
    {
        return HitTest(context, model, geometry, modelMatrix, false, ref hits, hitThickness);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="model"></param>
    /// <param name="geometry"></param>
    /// <param name="modelMatrix"></param>
    /// <param name="returnMultiple"></param>
    /// <param name="hits"></param>
    /// <param name="hitThickness"></param>
    /// <returns></returns>
    public virtual bool HitTest(HitTestContext? context, object? model, Geometry3D? geometry, Matrix modelMatrix,
        bool returnMultiple,
        ref List<HitTestResult> hits, float hitThickness)
    {
        if (context is null)
        {
            return false;
        }

        if (hits == null)
        {
            hits = new List<HitTestResult>();
        }
        hitPathBoundingBoxes.Clear();
        var hitStack = stack;
        var isHit = false;
        modelHits.Clear();
        var modelInv = modelMatrix.Inverted();
        if (modelInv == MatrixHelper.Zero)
        {
            return false;
        }//Cannot be inverted
        var rayWS = context.RayWS;
        var rayModel = new Ray(Vector3Helper.TransformCoordinate(rayWS.Position, modelInv), Vector3.Normalize(Vector3.TransformNormal(rayWS.Direction, modelInv)));
        var treeArray = SelfArray;
        var i = -1;
        while (true)
        {
            while (++i < treeArray.Length)
            {
                var node = treeArray[i];
                if (node == null)
                {
                    continue;
                }
                var isIntersect = false;
                var nodeHit = node.HitTestCurrentNodeExcludeChild(context, model, geometry, modelMatrix,
                    ref rayModel, ref modelHits, ref isIntersect, hitThickness);
                isHit |= nodeHit;
                if (isIntersect && node.HasChildren)
                {
                    hitStack.Push(new KeyValuePair<int, IDynamicOctree[]>(i, treeArray!));
                    treeArray = node.ChildNodes;
                    i = -1;
                }
                if (Parameter.RecordHitPathBoundingBoxes && nodeHit)
                {
                    var n = node;
                    while (n != null)
                    {
                        hitPathBoundingBoxes.Add(n.Bound);
                        n = n.Parent;
                    }
                }
            }
            if (hitStack.Count == 0)
            {
                break;
            }
            var pair = hitStack.Pop();
            i = pair.Key;
            treeArray = pair.Value;
        }
        if (!isHit)
        {
            hitPathBoundingBoxes.Clear();
        }
        else
        {
            hits.AddRange(modelHits);
            Hit?.Invoke(this, EventArgs.Empty);
        }
        return isHit;
    }

    /// <summary>
    /// Hit test for current node.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="model"></param>
    /// <param name="geometry"></param>
    /// <param name="modelMatrix"></param>
    /// <param name="rayModel"></param>
    /// <param name="hits"></param>
    /// <param name="isIntersect"></param>
    /// <param name="hitThickness"></param>
    /// <returns></returns>
    public abstract bool HitTestCurrentNodeExcludeChild(HitTestContext? context, object? model, Geometry3D? geometry, Matrix modelMatrix, ref Ray rayModel,
        ref List<HitTestResult> hits, ref bool isIntersect, float hitThickness);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="sphere"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    public virtual bool FindNearestPointBySphere(HitTestContext? context, ref BoundingSphere sphere, ref List<HitTestResult> points)
    {
        if (points == null)
        {
            points = new List<HitTestResult>();
        }
        var hitStack = stack;
        var isHit = false;
        var treeArray = SelfArray;
        var i = -1;
        while (true)
        {
            while (++i < treeArray.Length)
            {
                var node = treeArray[i];
                if (node == null)
                {
                    continue;
                }
                var isIntersect = false;
                var nodeHit = node.FindNearestPointBySphereExcludeChild(context, ref sphere, ref points, ref isIntersect);
                isHit |= nodeHit;
                if (isIntersect && node.HasChildren)
                {
                    hitStack.Push(new KeyValuePair<int, IDynamicOctree[]>(i, treeArray!));
                    treeArray = node.ChildNodes;
                    i = -1;
                }
            }
            if (hitStack.Count == 0)
            {
                break;
            }
            var pair = hitStack.Pop();
            i = pair.Key;
            treeArray = pair.Value;
        }
        return isHit;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="point"></param>
    /// <param name="results"></param>
    /// <param name="heuristicSearchFactor"></param>
    /// <returns></returns>
    public virtual bool FindNearestPointFromPoint(HitTestContext? context, ref Vector3 point, ref List<HitTestResult>? results, float heuristicSearchFactor = 1f)
    {
        results ??= new List<HitTestResult>();
        var hitStack = stack;

        var sphere = new BoundingSphere(point, float.MaxValue);
        var isIntersect = false;
        var isHit = false;
        heuristicSearchFactor = Math.Min(1.0f, Math.Max(0.1f, heuristicSearchFactor));
        var treeArray = SelfArray;
        var i = -1;
        while (true)
        {
            while (++i < treeArray.Length)
            {
                var node = treeArray[i];
                if (node == null)
                {
                    continue;
                }
                isHit |= node.FindNearestPointBySphereExcludeChild(context, ref sphere, ref results, ref isIntersect);

                if (isIntersect)
                {
                    if (results.Count > 0)
                    {
                        sphere.Radius = (float)results[0].Distance * heuristicSearchFactor;
                    }
                    if (node.HasChildren)
                    {
                        hitStack.Push(new KeyValuePair<int, IDynamicOctree[]>(i, treeArray!));
                        treeArray = node.ChildNodes;
                        i = -1;
                    }
                }
            }
            if (hitStack.Count == 0)
            {
                break;
            }
            var pair = hitStack.Pop();
            i = pair.Key;
            treeArray = pair.Value;
        }
        return isHit;
    }
    /// <summary>
    /// Find nearest point by sphere on current node only.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="sphere"></param>
    /// <param name="points"></param>
    /// <param name="isIntersect"></param>
    /// <returns></returns>
    public abstract bool FindNearestPointBySphereExcludeChild(HitTestContext? context, ref BoundingSphere sphere,
        ref List<HitTestResult> points, ref bool isIntersect);
    /// <summary>
    /// <see cref="DynamicOctreeBase{T}.Add(T)"/>
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Add(T item)
    {
        return Add(item, out IDynamicOctree? octant);
    }
    /// <summary>
    /// <see cref="DynamicOctreeBase{T}.Add(T, out IDynamicOctree)"/>
    /// </summary>
    /// <param name="item"></param>
    /// <param name="octant"></param>
    /// <returns></returns>
    public virtual bool Add(T item, out IDynamicOctree? octant)
    {
        var bound = GetBoundingBoxFromItem(item);
        var node = FindSmallestNodeContainsBoundingBox(ref bound, item);
        octant = node;
        if (node == null)
        {
            return false;
        }
        else
        {
            var nodeBase = node as DynamicOctreeBase<T>;
            if (nodeBase?.Objects is not null)
            {
                nodeBase.Objects.Add(item);
                if (nodeBase.Objects.Count > Parameter.MinObjectSizeToSplit)
                {
                    var index = nodeBase.Objects.Count - 1;
                    PushExistingToChild(nodeBase, index, IsContains, CreateNodeWithParent, out octant);
                }
            }
            return true;
        }
    }
    /// <summary>
    /// Push one of object belongs to current node into its child octant
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool PushExistingToChild(int index)
    {
        return PushExistingToChild(index, out IDynamicOctree? octant);
    }
    /// <summary>
    /// Push one of object belongs to current node into its child octant
    /// </summary>
    /// <param name="index"></param>
    /// <param name="octant"></param>
    /// <returns></returns>
    public virtual bool PushExistingToChild(int index, out IDynamicOctree? octant)
    {
        octant = this;
        if (this.Objects is not null && this.Objects.Count > Parameter.MinObjectSizeToSplit)
        {
            return PushExistingToChild(this, index, IsContains, CreateNodeWithParent, out octant);
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Push existing item into child
    /// </summary>
    /// <param name="node"></param>
    /// <param name="index"></param>
    /// <param name="isContains"></param>
    /// <param name="createNodeFunc"></param>
    /// <param name="octant"></param>
    /// <returns>True: Pushed into child. Otherwise false.</returns>
    public static bool PushExistingToChild(DynamicOctreeBase<T> node, int index, Func<BoundingBox, T, bool> isContains,
        CreateNodeDelegate createNodeFunc, out IDynamicOctree? octant)
    {
        if (node.Objects is null || node.Octants is null)
        {
            octant = node;
            return false;
        }

        var item = node.Objects[index];
        octant = node;
        var pushToChild = false;
        for (var i = 0; i < node.Octants.Length; ++i)
        {
            if (isContains(node.Octants[i], item))
            {
                node.Objects.RemoveAt(index);
                if (node.ChildNodes[i] != null)
                {
                    (node.ChildNodes[i] as DynamicOctreeBase<T>)?.Objects?.Add(item);
                    octant = node.ChildNodes[i];
                }
                else
                {
                    node.ChildNodes[i] = createNodeFunc(ref node.Octants[i], new List<T>() { item }, node);
                    node.ActiveNodes |= (byte)(1 << i);
                    node.ChildNodes[i]?.BuildTree();
                    var idx = -1;
                    octant = (node.ChildNodes[i] as DynamicOctreeBase<T>)?.FindChildByItemBound(item, out idx);
                }
                pushToChild = true;
                break;
            }
        }
        return pushToChild;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="targetObj"></param>
    /// <returns></returns>
    protected bool IsContains(BoundingBox source, T targetObj)
    {
        var bound = GetBoundingBoxFromItem(targetObj);
        return IsContains(source, bound, targetObj);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="targetObj"></param>
    /// <returns></returns>
    protected virtual bool IsContains(BoundingBox source, BoundingBox target, T targetObj)
    {
        return source.Contains(ref target) == ContainmentType.Contains;
    }
    /// <summary>
    /// Return new root
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public virtual IDynamicOctree? Expand(ref Vector3 direction)
    {
        return Expand(this, ref direction, CreateNodeWithParent);
    }

    private readonly static Vector3 epsilon = new(float.Epsilon, float.Epsilon, float.Epsilon);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private static float CorrectFloatError(float value)
    //{
    //    if (value > 0)
    //    {
    //        return value + float.Epsilon;
    //    }
    //    else if (value < 0)
    //    {
    //        return value - float.Epsilon;
    //    }else
    //    {
    //        return value;
    //    }
    //}
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private static void CorrectFloatError(ref Vector3 v)
    //{
    //    v.X = CorrectFloatError(v.X);
    //    v.Y = CorrectFloatError(v.Y);
    //    v.Z = CorrectFloatError(v.Z);
    //}
    /// <summary>
    /// Return new root
    /// </summary>
    /// <param name="oldRoot"></param>
    /// <param name="direction"></param>
    /// <param name="createNodeFunc"></param>
    /// <returns></returns>
    public static IDynamicOctree? Expand(IDynamicOctree oldRoot, ref Vector3 direction, CreateNodeDelegate createNodeFunc)
    {
        if (oldRoot.Parent != null)
        {
            throw new ArgumentException("Input node is not root node");
        }
        var rootBound = oldRoot.Bound;
        var xDirection = direction.X >= 0 ? 1 : -1;
        var yDirection = direction.Y >= 0 ? 1 : -1;
        var zDirection = direction.Z >= 0 ? 1 : -1;
        var dimension = rootBound.Maximum - rootBound.Minimum;
        var half = dimension / 2 + epsilon;
        var center = rootBound.Minimum + half;
        var newSize = dimension * 2;
        var newCenter = center + new Vector3(xDirection * Math.Abs(half.X), yDirection * Math.Abs(half.Y), zDirection * Math.Abs(half.Z));
        var bound = new BoundingBox(newCenter - dimension, newCenter + dimension);
        BoundingBox.Merge(ref rootBound, ref bound, out bound);
        var newRoot = createNodeFunc(ref bound, new List<T>(), oldRoot);
        newRoot.Parent = null;
        newRoot.BuildTree();
        var succ = false;
        if (!oldRoot.IsEmpty)
        {
            var idx = -1;
            var diff = float.MaxValue;
            for (var i = 0; i < newRoot.Octants!.Length; ++i)
            {
                var d = (newRoot.Octants[i].Minimum - rootBound.Minimum).LengthSquared();
                if (d < diff)
                {
                    diff = d;
                    idx = i;
                    if (diff < 10e-8)
                    {
                        break;
                    }
                }
            }
            if (idx >= 0 && idx < newRoot.Octants.Length)
            {
                newRoot.ChildNodes[idx] = oldRoot;
                newRoot.Octants[idx] = oldRoot.Bound;
                newRoot.ActiveNodes |= (byte)(1 << idx);
                oldRoot.Parent = newRoot;
                succ = true;
            }

            if (!succ)
            {
                throw new Exception("Expand failed.");
            }
        }
        return newRoot;
    }

    /// <summary>
    /// Shrink the root bound to contains all items inside, return new root
    /// </summary>
    /// <returns></returns>
    public virtual IDynamicOctree? Shrink()
    {
        return Shrink(this);
    }
    /// <summary>
    /// Shrink the root bound to contains all items inside
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public static IDynamicOctree? Shrink(IDynamicOctree root)
    {
        if (root.Parent != null)
        {
            throw new ArgumentException("Input node is not a root node.");
        }
        if (root.IsEmpty)
        {
            return root;
        }
        else if ((root as DynamicOctreeBase<T>)?.Objects?.Count == 0 && (root.ActiveNodes & (root.ActiveNodes - 1)) == 0)
        {
            for (var i = 0; i < root.ChildNodes.Length; ++i)
            {
                if (root.ChildNodes[i] != null)
                {
                    var newRoot = root.ChildNodes[i];
                    newRoot!.Parent = null;
                    root.ChildNodes[i] = null;
                    return newRoot;
                }
            }
            return null;
        }
        else
        {
            return root;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bound"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public IDynamicOctree? FindSmallestNodeContainsBoundingBox(ref BoundingBox bound, T item)
    {
        return FindSmallestNodeContainsBoundingBox<T>(bound, item, IsContains, this, this.stack);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="bound"></param>
    /// <param name="item"></param>
    /// <param name="isContains"></param>
    /// <param name="root"></param>
    /// <param name="stackCache"></param>
    /// <returns></returns>
    private static IDynamicOctree? FindSmallestNodeContainsBoundingBox<E>(BoundingBox bound, E item, Func<BoundingBox, E, bool> isContains, DynamicOctreeBase<E> root,
        Stack<KeyValuePair<int, IDynamicOctree[]>> stackCache)
    {
        IDynamicOctree? result = null;
        TreeTraversal(root, stackCache,
            (node) => { return isContains(node.Bound, item); },
            (node) => { result = node; });
        return result;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public IDynamicOctree? FindChildByItem(T item, out int index)
    {
        return FindChildByItem<T>(item, this, this.stack, out index);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="item"></param>
    /// <param name="root"></param>
    /// <param name="stackCache"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static IDynamicOctree? FindChildByItem<E>(E item, DynamicOctreeBase<E> root, Stack<KeyValuePair<int, IDynamicOctree[]>> stackCache, out int index)
    {
        IDynamicOctree? result = null;
        var idx = -1;
        TreeTraversal(root, stackCache, null,
            (node) =>
            {
                idx = (node as DynamicOctreeBase<E>)?.Objects?.IndexOf(item) ?? -1;
                result = idx != -1 ? node : null;
            },
            () => { return idx != -1; });
        index = idx;
        return result;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="bound"></param>
    /// <returns></returns>
    public virtual bool RemoveByBound(T item, ref BoundingBox bound)
    {
        var node = FindChildByItemBound(item, ref bound, out int index);
        if (node == null)
        {
#if DEBUG
            if (!RemoveSafe(item))
            {
                throw new Exception("item not found using bound.");
            }
            return true;
#else
                return RemoveSafe(item);
#endif
        }
        else
        {
            var nodeBase = node as DynamicOctreeBase<T>;
            nodeBase?.Objects?.RemoveAt(index);
            if (nodeBase is not null && nodeBase.IsEmpty && nodeBase.AutoDeleteIfEmpty)
            {
                nodeBase.RemoveSelf();
            }
            return true;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public virtual bool RemoveByBound(T item)
    {
        var bound = GetBoundingBoxFromItem(item);
        return RemoveByBound(item, ref bound);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public virtual bool RemoveSafe(T item)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Remove safe.");
        }
        var node = FindChildByItem(item, out var index);
        if (node != null)
        {
            (node as DynamicOctreeBase<T>)?.Objects?.RemoveAt(index);
            if (node.IsEmpty && node.AutoDeleteIfEmpty)
            {
                node.RemoveSelf();
            }
            return true;
        }
        return false;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public virtual bool RemoveAt(int index)
    {
        if (index < 0 || index >= (this.Objects?.Count ?? 0))
        {
            return false;
        }
        this.Objects?.RemoveAt(index);
        if (this.IsEmpty && this.AutoDeleteIfEmpty)
        {
            this.RemoveSelf();
        }
        return true;
    }
    /// <summary>
    /// 
    /// </summary>
    public virtual void RemoveSelf()
    {
        if (Parent == null)
        {
            return;
        }

        Clear();
        Parent.RemoveChild(this);
        Parent = null;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="child"></param>
    public void RemoveChild(IDynamicOctree child)
    {
        for (var i = 0; i < ChildNodes.Length; ++i)
        {
            if (ChildNodes[i] == child)
            {
                ChildNodes[i] = null;
                ActiveNodes ^= (byte)(1 << i);
                break;
            }
        }
        if (IsEmpty && AutoDeleteIfEmpty)
        {
            RemoveSelf();
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public virtual IDynamicOctree? FindChildByItemBound(T item, out int index)
    {
        return FindChildByItemBound(item, ref bound, out index);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="bound"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public virtual IDynamicOctree? FindChildByItemBound(T item, ref BoundingBox bound, out int index)
    {
        return FindChildByItemBound<T>(item, bound, IsContains, this, this.stack, out index);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="item"></param>
    /// <param name="bound"></param>
    /// <param name="isContains"></param>
    /// <param name="root"></param>
    /// <param name="stackCache"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static IDynamicOctree? FindChildByItemBound<E>(E item, BoundingBox bound, Func<BoundingBox, BoundingBox, E, bool> isContains, DynamicOctreeBase<E> root, Stack<KeyValuePair<int, IDynamicOctree[]>> stackCache, out int index)
    {
        var idx = -1;
        IDynamicOctree? result = null;
        DynamicOctreeBase<E>? lastNode = null;
        TreeTraversal(root, stackCache,
            (node) => { return isContains(node.Bound, bound, item); },
            (node) =>
            {
                lastNode = node as DynamicOctreeBase<E>;
                idx = lastNode?.Objects?.IndexOf(item) ?? -1;
                result = idx != -1 ? node : null;
            },
            () => { return idx != -1; });
        index = idx;
        //If not found, traverse from bottom to top to find the item.
        if (result == null)
        {
            while (lastNode != null)
            {
                index = lastNode?.Objects?.IndexOf(item) ?? -1;
                if (index == -1)
                {
                    lastNode = lastNode?.Parent as DynamicOctreeBase<E>;
                }
                else
                {
                    result = lastNode;
                    break;
                }
            }
        }
        return result;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IDynamicOctree FindRoot(IDynamicOctree node)
    {
        while (node.Parent != null)
        {
            node = node.Parent;
        }
        return node;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="point"></param>
    /// <param name="radius"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool FindNearestPointByPointAndSearchRadius(HitTestContext? context, ref Vector3 point, float radius, ref List<HitTestResult> result)
    {
        var sphere = new BoundingSphere(point, radius);
        return FindNearestPointBySphere(context, ref sphere, ref result);
    }

    #region Accessors
    /// <summary>
    /// <see cref="IDynamicOctree.IsRoot"/>
    /// </summary>
    public bool IsRoot
    {
        //The root node is the only node without a parent.
        get
        {
            return Parent == null;
        }
    }
    /// <summary>
    /// <see cref="IDynamicOctree.HasChildren"/>
    /// </summary>
    public bool HasChildren
    {
        get
        {
            return ActiveNodes != 0;
        }
    }
    /// <summary>
    /// <see cref="IDynamicOctree.IsEmpty"/>
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            return !HasChildren && (Objects is null || Objects.Count == 0);
        }
    }
    #endregion

    public LineGeometry3D CreateOctreeLineModel()
    {
        return OctreeHelper.CreateOctreeLineModel(this);
    }
}
