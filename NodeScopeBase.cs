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

    public ScopeId? ParentScopeId { get; set; }
    public ScopeId ScopeId { get; set; }

    void IScope.Construct(DependencyBinder binder, object config)
    {
        Construct(binder, config);
    }

    protected abstract void Construct(DependencyBinder binder, object config);

    internal void ConstructScope(ScopeId scopeId, ScopeId parentScopeId, object config = null, IExtendScope optionExtendScope = null)
    {
        ScopeId = scopeId;
        ParentScopeId = parentScopeId;
        GlobalProjectScope.Scopes.Add(scopeId, this);

        var childBinder = new DependencyBinder(scopeId);
        if (optionExtendScope != null)
        {
            childBinder.ExtendScope(optionExtendScope);
        }
        // foreach (var extendScope in _extendScopes)
        // {
        //     Inject(extendScope, this);
        //     childBinder.ExtendScope(extendScope, this);
        // }

        GlobalProjectScope.Inject(this, this);
        IScope scope = this;
        scope.Construct(childBinder, config);

        var cancellationTokenOnDestroy = this.GetCancellationTokenOnDestroy();
        if (this is IAsyncInitializer asyncInitializer)
        {
            var parentScope = scope;
            IAsyncInitializer parentAsyncInitializer = null;
            ProjectScope.Initializers.Add(asyncInitializer);

            while (!parentScope.ParentScopeId.Equals(ProjectScope.Scope.ParentScopeId))
            {
                GlobalProjectScope.Scopes.TryGetValue(parentScope.ParentScopeId.Value, out parentScope);
                if (parentScope is IAsyncInitializer parent)
                {
                    parentAsyncInitializer = parent;
                    break;
                }
            }

            this.ReadyAsync()
                .ContinueWith(async () =>
                {
                    if (parentAsyncInitializer != null)
                    {
                        await GDTask.WaitWhile(() => ProjectScope.Initializers.Any(x => x == parentAsyncInitializer),
                            cancellationToken: cancellationTokenOnDestroy);
                    }

                    await asyncInitializer.InitializeAsync(cancellationTokenOnDestroy);
                    ProjectScope.Initializers.Remove(asyncInitializer);
                })
                .Forget();
        }

        cancellationTokenOnDestroy.Register(() =>
        {
            GlobalProjectScope.Scopes.Remove(scopeId);
            GlobalProjectScope.Modules.RemoveScope(scopeId);
        });

        InjectOrInitializeChildrenRecursive(this);
    }

    internal void ConstructScope(IScope targetScope, ScopeId scopeId, ScopeId parentScopeId, object config = null, IExtendScope optionExtendScope = null)
    {
        targetScope.ScopeId = scopeId;
        targetScope.ParentScopeId = parentScopeId;
        GlobalProjectScope.Scopes.Add(scopeId, targetScope);

        var childBinder = new DependencyBinder(scopeId);
        if (optionExtendScope != null)
        {
            childBinder.ExtendScope(optionExtendScope);
        }
        // foreach (var extendScope in _extendScopes)
        // {
        //     GlobalProjectScope.Inject(extendScope, targetScope);
        //     childBinder.ExtendScope(extendScope);
        // }

        GlobalProjectScope.Inject(targetScope, targetScope);
        targetScope.Construct(childBinder, config);
        var targetNode = targetScope as Node;

        var cancellationTokenOnDestroy = targetNode.GetCancellationTokenOnDestroy();
        if (targetScope is IAsyncInitializer asyncInitializer)
        {
            var parentScope = targetScope;
            IAsyncInitializer parentAsyncInitializer = null;
            ProjectScope.Initializers.Add(asyncInitializer);

            while (!parentScope.ParentScopeId.Equals(ProjectScope.Scope.ParentScopeId))
            {
                GlobalProjectScope.Scopes.TryGetValue(parentScope.ParentScopeId.Value, out parentScope);
                if (parentScope is IAsyncInitializer parent)
                {
                    parentAsyncInitializer = parent;
                    break;
                }
            }

            targetNode.ReadyAsync()
                .ContinueWith(async () =>
                {
                    if (parentAsyncInitializer != null)
                    {
                        await GDTask.WaitWhile(() => ProjectScope.Initializers.Any(x => x == parentAsyncInitializer),
                            cancellationToken: cancellationTokenOnDestroy);
                    }

                    await asyncInitializer.InitializeAsync(cancellationTokenOnDestroy);
                    ProjectScope.Initializers.Remove(asyncInitializer);
                })
                .Forget();
        }

        cancellationTokenOnDestroy.Register(() =>
        {
            GlobalProjectScope.Scopes.Remove(scopeId);
            GlobalProjectScope.Modules.RemoveScope(scopeId);
        });

        var children = targetNode.GetChildren();
        for (int i = 0; i < children.Count; i++)
        {
            InjectOrInitializeChildrenRecursive(children[i]);
        }
    }

    private void InjectOrInitializeChildrenRecursive(Node current)
    {
        var injectable = current as IInjectable;
        if (injectable == null)
        {
            return;
        }

        if (injectable is IScope scope && scope != this)
        {
            var scopeId = ScopeId.Create();
            ConstructScope(scope, scopeId: scopeId, parentScopeId: ScopeId);
            return;
        }
        else
        {
            GlobalProjectScope.Inject(injectableObject: injectable, scope: this);
        }

        var children = current.GetChildren();
        for (int i = 0; i < children.Count; i++)
        {
            InjectOrInitializeChildrenRecursive(children[i]);
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
            ConstructScope(scope, ScopeId.Create(), ScopeId, config);
            parent.AddChild(instance);
        }
        else
        {
            ProjectScope.SetTemporaryParentScopeId(ScopeId);
            ProjectScope.PushConfig(config);
            parent.AddChild(instance);
            ProjectScope.SetTemporaryParentScopeId(null);
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

        ProjectScope.SetTemporaryParentScopeId(ScopeId);
        ProjectScope.PushConfig(config);
        var instance = instancePlaceholder.CreateInstance();
        if (instance == null)
        {
            GD.PrintErr($"The scene does not contain a node of type {typeof(T).Name}.");
            return null;
        }

        if (instance is IScope scope)
        {
            ConstructScope(scope, ScopeId.Create(), ScopeId, config);
        }

        ProjectScope.SetTemporaryParentScopeId(null);
        return (T)instance.GetNode<T>(instance.GetPath());
    }

    public NodeScopeWithConfig<TConfig> InstantiateWithConfig<TConfig>(PackedScene packedScene, TConfig config, Node parent) where TConfig : class
    {
        return Instantiate<NodeScopeWithConfig<TConfig>>(packedScene, parent, config);
    }
}