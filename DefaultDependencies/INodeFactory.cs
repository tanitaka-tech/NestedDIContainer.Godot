using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using NestedDIContainer.Unity.Runtime;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Godot.DefaultDependencies;

public interface INodeFactory
{
    T Instantiate<T>(ScopeId parentScopeId, PackedScene packedScene, Node parent, object config = null) where T : Node;

    /// <summary>
    /// Create an instance of the specified type from the given instance placeholder.
    /// NOTE: Call at the timing of _Ready as an error will occur if called at the timing of _EnterTree
    /// </summary>
    T Instantiate<T>(ScopeId parentScopeId, InstancePlaceholder instancePlaceholder, object config = null) where T : Node;
}

internal class NodeFactory : INodeFactory
{
    private const BindingFlags MemberBindingFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    public T Instantiate<T>(ScopeId parentScopeId, PackedScene packedScene, Node parent, object config = null) where T : Node
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
            InitializeScope(scope, ScopeId.Create(), parentScopeId, config);
            parent.AddChild(instance);
        }
        else
        {
            ProjectScope.SetTemporaryParentScopeId(parentScopeId);
            ProjectScope.PushConfig(config);
            parent.AddChild(instance);
            ProjectScope.SetTemporaryParentScopeId(null);
        }

        return instance;
    }

    public T Instantiate<T>(ScopeId parentScopeId, InstancePlaceholder instancePlaceholder, object config = null) where T : Node
    {
        if (instancePlaceholder == null)
        {
            GD.PrintErr("Scene to instantiate is not set.");
            return null;
        }

        ProjectScope.SetTemporaryParentScopeId(parentScopeId);
        ProjectScope.PushConfig(config);
        var instance = instancePlaceholder.CreateInstance();
        if (instance == null)
        {
            GD.PrintErr($"The scene does not contain a node of type {typeof(T).Name}.");
            return null;
        }
        if (instance is IScope scope)
        {
            InitializeScope(scope, ScopeId.Create(), parentScopeId, config);
        }

        ProjectScope.SetTemporaryParentScopeId(null);
        return (T)instance.GetNode<T>(instance.GetPath());
    }

    public NodeScopeWithConfig<TConfig> InstantiateWithConfig<TConfig>(ScopeId parentScopeId, PackedScene packedScene, TConfig config, Node parent) where TConfig : class
    {
        return Instantiate<NodeScopeWithConfig<TConfig>>(parentScopeId, packedScene, parent, config);
    }

    private void InitializeScope(IScope scope, ScopeId scopeId, ScopeId parentScopeId, object config = null)
    {
        scope.ScopeId = scopeId;
        scope.ParentScopeId = parentScopeId;
        var childBoundTypes = new List<Type>();
        var childBinder = new DependencyBinder(ProjectScope.Modules, scopeId, ref childBoundTypes);

        ProjectScope.NestedScopes.Add(scopeId, scope);
        Inject(scope);
        scope.Construct(childBinder, config);
        scope.Initialize();

        if (scope is Node node)
        {
            node.TreeExited += () =>
            {
                ProjectScope.NestedScopes.Remove(scopeId);
                childBoundTypes.ForEach(type => ProjectScope.Modules.Remove(scopeId, type));
                childBoundTypes.Clear();
            };
        }
    }

    private void Inject(IScope scope)
    {
        var type = scope.GetType();
        var parentScopeId = scope.ParentScopeId.Value;
        var fields = type.GetFields(MemberBindingFlags);
        foreach (var field in fields)
        {
            var injectAttr = field.GetCustomAttribute<InjectAttribute>();
            if (injectAttr != null)
            {
                field.SetValue(scope, ProjectScope.Modules.Resolve(field.FieldType, parentScopeId));
            }
        }

        var props = type.GetProperties(MemberBindingFlags);
        foreach (var prop in props)
        {
            var injectAttr = prop.GetCustomAttribute<InjectAttribute>();
            if (injectAttr != null)
            {
                prop.SetValue(scope, ProjectScope.Modules.Resolve(prop.PropertyType, parentScopeId));
            }
        }
    }
}