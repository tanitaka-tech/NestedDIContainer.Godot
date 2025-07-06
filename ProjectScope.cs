using System.Collections.Generic;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Godot;

public abstract partial class ProjectScope : NodeScope
{
    internal static ProjectScope Scope => _projectScope;
    private static ProjectScope _projectScope;
    private static object _tempConfig = null;
    private static IScope _tempParent = null;

    internal static readonly List<IAsyncInitializer> Initializers = new ();

    internal static object PopConfig()
    {
        var temp = _tempConfig;
        _tempConfig = null;
        return temp;
    }

    internal static void PushConfig(object config)
    {
        _tempConfig = config;
    }

    internal static IScope PopParentScope()
    {
        var temp = _tempParent;
        _tempParent = null;
        return temp;
    }
    internal static void PushParentScope(IScope parentScope)
    {
        _tempParent = parentScope;
    }

    public override void _EnterTree()
    {
        _projectScope = this;
        ConstructScope(ScopeId.Create(), null);
    }

    public override void _ExitTree()
    {
        _tempConfig = null;
        _tempParent = null;
        _projectScope = null;
    }
}