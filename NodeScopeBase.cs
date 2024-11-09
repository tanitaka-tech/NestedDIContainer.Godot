using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using NestedDIContainer.Unity.Runtime;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Godot;

public abstract partial class NodeScopeBase : Node, IScope
{
    private const BindingFlags MemberBindingFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    public ScopeId? ParentScopeId { get; set; }
    public ScopeId ScopeId { get; set; }

    void IScope.Construct(DependencyBinder binder, object config)
    {
        Construct(binder, config);
        RecursiveConstruct(this, binder, this);

        void RecursiveConstruct(Node node, DependencyBinder binder, IScope parentScope)
        {
            foreach (var child in node.GetChildren())
            {
                bool isNotNodeScopeBase = !(child is NodeScopeBase);
                if (child is IScope scope && isNotNodeScopeBase && scope.ParentScopeId == null)
                {
                    scope.ParentScopeId = parentScope.ScopeId;
                    Inject(scope, scope.ParentScopeId.Value);
                    scope.Construct(binder, null);
                    RecursiveConstruct(child, binder, scope);
                }
                else if (isNotNodeScopeBase)
                {
                    RecursiveConstruct(child, binder, parentScope);
                }
            }
        }
    }

    protected abstract void Construct(DependencyBinder binder, object config);

    void IScope.Initialize()
    {
        Initialize();
        RecursiveInitialize(this);

        void RecursiveInitialize(Node node)
        {
            foreach (var child in node.GetChildren())
            {
                bool isNotNodeScopeBase = !(child is NodeScopeBase);
                if (child is IScope scope && isNotNodeScopeBase)
                {
                    scope.Initialize();
                }
                if (isNotNodeScopeBase)
                {
                    RecursiveInitialize(child);
                }
            }
        }
    }

    protected virtual void Initialize() { }

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
            InitializeScope(scope, ScopeId.Create(), ScopeId, config);
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
            InitializeScope(scope, ScopeId.Create(), ScopeId, config);
        }

        ProjectScope.SetTemporaryParentScopeId(null);
        return (T)instance.GetNode<T>(instance.GetPath());
    }

    public NodeScopeWithConfig<TConfig> InstantiateWithConfig<TConfig>(PackedScene packedScene, TConfig config,
        Node parent) where TConfig : class
    {
        return Instantiate<NodeScopeWithConfig<TConfig>>(packedScene, parent, config);
    }

    internal void InitializeScope(ScopeId scopeId, ScopeId parentScopeId, object config = null)
    {
        InitializeScope(this, scopeId, parentScopeId, config);
    }

    internal void InitializeScope(IScope scope, ScopeId scopeId, ScopeId parentScopeId, object config = null)
    {
        scope.ScopeId = scopeId;
        scope.ParentScopeId = parentScopeId;
        var childBoundTypes = new List<Type>();
        var childBinder = new DependencyBinder(ProjectScope.Modules, scopeId, ref childBoundTypes);

        ProjectScope.NestedScopes.Add(scopeId, this);
        Inject(scope, parentScopeId);
        scope.Construct(childBinder, config);
        scope.Initialize();

        // Nodeが削除された時にScopeを削除する
        TreeExited += () =>
        {
            ProjectScope.NestedScopes.Remove(scopeId);
            childBoundTypes.ForEach(type => ProjectScope.Modules.Remove(scopeId, type));
            childBoundTypes.Clear();
        };
    }

    private void Inject(IScope scope, ScopeId scopeId)
    {
        var type = scope.GetType();
        var fields = type.GetFields(MemberBindingFlags);
        foreach (var field in fields)
        {
            var injectAttr = field.GetCustomAttribute<InjectAttribute>();
            if (injectAttr != null)
            {
                field.SetValue(scope, ProjectScope.Modules.Resolve(field.FieldType, scopeId));
            }
        }

        var props = type.GetProperties(MemberBindingFlags);
        foreach (var prop in props)
        {
            var injectAttr = prop.GetCustomAttribute<InjectAttribute>();
            if (injectAttr != null)
            {
                prop.SetValue(scope, ProjectScope.Modules.Resolve(prop.PropertyType, scopeId));
            }
        }
    }
}
