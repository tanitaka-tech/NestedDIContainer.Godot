using System.Collections.Generic;
using Fractural.Tasks.Triggers;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Godot;

public abstract partial class ProjectScope : NodeScope
{
    internal static NodeScopeBase ParentNodeScope =>
        _temporaryParentScopeId.HasValue
            ? GlobalProjectScope.Scopes[_temporaryParentScopeId.Value] as NodeScopeBase
            : Scope;
    internal static ProjectScope Scope => _scope;
    protected static ProjectScope _scope;

    internal static List<IAsyncInitializer> Initializers { get; } = new ();

    public static ScopeId? TemporaryParentScopeId => _temporaryParentScopeId;
    private static ScopeId? _temporaryParentScopeId = null;
    public static void SetTemporaryParentScopeId(ScopeId? parentScopeId)
    {
        _temporaryParentScopeId = parentScopeId;
    }

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

    private static object _tempConfig = null;

    public override void _EnterTree()
    {
        _scope = this;
        ScopeId = ScopeId.Create();
        ConstructScope(ScopeId, ScopeId.Create());
    }

    public override void _ExitTree()
    {
        GlobalProjectScope.Dispose();
        _scope = null;
        _tempConfig = null;
    }
}