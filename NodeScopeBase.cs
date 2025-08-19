using System.Linq;
using Fractural.Tasks;
using Fractural.Tasks.Triggers;
using Godot;
using NestedDIContainer.Godot.DefaultDependencies;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Godot;

public abstract partial class NodeScopeBase : Node, IScope, IChildSceneScopeFactory
{
    //[Export] protected List<ScriptableObjectExtendScope> _extendScopes;

    public ScopeId ScopeId { get; private set; }
    public IScope ParentScope => ScopeContainer.ParentScope;
    public ScopeContainer ScopeContainer { get; set; }


    void IScope.Construct(DependencyBinder binder, object config)
    {
        Construct(binder, config);
    }

    protected abstract void Construct(DependencyBinder binder, object config);

    internal void ConstructScope(ScopeId scopeId, ScopeContainer parentScopeContainer, object config = null, IExtendScope optionExtendScope = null)
    {
        ScopeId = scopeId;
        ScopeContainer = new ScopeContainer(this, parentScopeContainer);

        var childBinder = new DependencyBinder(ScopeContainer);
        if (optionExtendScope != null)
        {
            childBinder.ExtendScope(optionExtendScope);
        }
        // foreach (var extendScope in _extendScopes)
        // {
        //     Inject(extendScope, this);
        //     childBinder.ExtendScope(extendScope, this);
        // }

        ScopeContainer.Inject(this);
        IScope scope = this;
        scope.Construct(childBinder, config);

        var cancellationTokenOnDestroy = this.GetCancellationTokenOnDestroy();
        if (this is IAsyncInitializer asyncInitializer)
        {
            var parentScope = scope;
            IAsyncInitializer parentAsyncInitializer = null;
            ProjectScope.Initializers.Add(asyncInitializer);

            while (parentScope.ParentScope !=  ProjectScope.Scope.ParentScope)
            {
                parentScope = parentScope.ParentScope;
                if (parentScope is IAsyncInitializer parent)
                {
                    parentAsyncInitializer = parent;
                    break;
                }
            }

            this.ReadyAsync()
                .ContinueWith(async () =>
                {
                    if (parentAsyncInitializer != null && ProjectScope.Initializers.Any(x => x == parentAsyncInitializer))
                    {
                        await GDTask.WaitWhile(() => ProjectScope.Initializers.Any(x => x == parentAsyncInitializer), cancellationToken: cancellationTokenOnDestroy);
                    }
                    await asyncInitializer.InitializeAsync(cancellationTokenOnDestroy);
                    ProjectScope.Initializers.Remove(asyncInitializer);
                })
                .Forget();
        }

        var children = GetChildren();
        for (int i = 0; i < children.Count; i++)
        {
            InjectOrInitializeChildrenRecursive(children[i]);
        }
    }

    internal void ConstructScope(IScope targetScope, ScopeId scopeId, ScopeContainer parentScopeContainer, object config = null, IExtendScope optionExtendScope = null)
    {
        targetScope.ScopeContainer = new ScopeContainer(this, parentScopeContainer);
        var childBinder = new DependencyBinder(targetScope.ScopeContainer);
        if (optionExtendScope != null)
        {
            childBinder.ExtendScope(optionExtendScope);
        }
        // foreach (var extendScope in _extendScopes)
        // {
        //     GlobalProjectScope.Inject(extendScope, targetScope);
        //     childBinder.ExtendScope(extendScope);
        // }

        var targetNode = targetScope as Node;
        ScopeContainer.Inject(targetScope);
        if (targetScope != this)
        {
            targetScope.Construct(childBinder, config);
        }

        var cancellationTokenOnDestroy = targetNode.GetCancellationTokenOnDestroy();
        if (targetScope is IAsyncInitializer asyncInitializer)
        {
            var parentScope = targetScope;
            IAsyncInitializer parentAsyncInitializer = null;
            ProjectScope.Initializers.Add(asyncInitializer);

            while (parentScope.ParentScope != ProjectScope.Scope.ParentScope)
            {
                parentScope = parentScope.ParentScope;
                if (parentScope is IAsyncInitializer parent)
                {
                    parentAsyncInitializer = parent;
                    break;
                }
            }

            targetNode.ReadyAsync()
                .ContinueWith(async () =>
                {
                    if (parentAsyncInitializer != null && ProjectScope.Initializers.Any(x => x == parentAsyncInitializer))
                    {
                        await GDTask.WaitWhile(() => ProjectScope.Initializers.Any(x => x == parentAsyncInitializer), cancellationToken: cancellationTokenOnDestroy);
                    }
                    await asyncInitializer.InitializeAsync(cancellationTokenOnDestroy);
                    ProjectScope.Initializers.Remove(asyncInitializer);
                })
                .Forget();
        }

        var children = targetNode.GetChildren();
        for (int i = 0; i < children.Count; i++)
        {
            InjectOrInitializeChildrenRecursive(children[i]);
        }
    }

    private void InjectOrInitializeChildrenRecursive(Node current)
    {
        DynamicInjectOrInitializeChildrenRecursive(current);
    }

    private void DynamicInjectOrInitializeChildrenRecursive(Node current)
    {
        var injectable = current as IInjectable;
        if (injectable != null)
        {
            bool needToInjectChildren = true;
            if (injectable is NodeScopeBase nodeScopeBase && nodeScopeBase != this)
            {
                var scopeId = ScopeId.Create();
                nodeScopeBase.ConstructScope(scopeId: scopeId, parentScopeContainer: ScopeContainer);
                needToInjectChildren = false;
            }
            else if (injectable is IScope scope)
            {
                ScopeContainer.Inject(injectable);
                ConstructScope(scope, scopeId: ScopeId.Create(), parentScopeContainer: ScopeContainer);
            }
            else
            {
                ScopeContainer.Inject(injectable);
            }
            if (!needToInjectChildren)
            {
                return;
            }
        }

        var children = current.GetChildren();
        for (int i = 0; i < children.Count; i++)
        {
            DynamicInjectOrInitializeChildrenRecursive(children[i]);
        }
    }

    // implement INodeScopeFactory
    public T Instantiate<T>(PackedScene packedScene, Node parent, object config = null) where T : Node
    {
        if (packedScene == null)
        {
            GD.PrintErr("Scene to instantiate is not set.");
            return null;
        }

        var instance = packedScene.Instantiate<T>();
        if (instance == null)
        {
            GD.PrintErr($"The scene does not contain a node of type {typeof(T).Name}.");
            return null;
        }

        if (instance is IScope scope)
        {
            ConstructScope(scope, ScopeId.Create(), ScopeContainer, config);
            parent.AddChild(instance);
        }
        else
        {
            ProjectScope.PushParentScope(this);
            ProjectScope.PushConfig(config);
            parent.AddChild(instance);
            ProjectScope.PopParentScope();
        }

        return instance;
    }

    /// <summary>
    /// Create an instance of the specified type from the given instance placeholder.
    /// NOTE: Call at the timing of _Ready as an error will occur if called at the timing of _EnterTree
    /// </summary>
    public T Instantiate<T>(InstancePlaceholder instancePlaceholder, object config = null) where T : Node
    {
        if (instancePlaceholder == null)
        {
            GD.PrintErr("Scene to instantiate is not set.");
            return null;
        }

        ProjectScope.PushParentScope(this);
        ProjectScope.PushConfig(config);
        var instance = instancePlaceholder.CreateInstance();
        if (instance == null)
        {
            GD.PrintErr($"The scene does not contain a node of type {typeof(T).Name}.");
            return null;
        }

        if (instance is IScope scope)
        {
            ConstructScope(scope, ScopeId.Create(), ScopeContainer, config);
        }

        ProjectScope.PopParentScope();
        return (T)instance.GetNode<T>(instance.GetPath());
    }

    public NodeScopeWithConfig<TConfig> InstantiateWithConfig<TConfig>(PackedScene packedScene, TConfig config, Node parent) where TConfig : class
    {
        return Instantiate<NodeScopeWithConfig<TConfig>>(packedScene, parent, config);
    }
}