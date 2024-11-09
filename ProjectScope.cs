using System.Collections.Generic;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Godot;

public abstract partial class ProjectScope : NodeScope
{
    internal static NodeScopeBase ParentNodeScope =>
        _temporaryParentScopeId.HasValue
            ? NestedScopes.Get(_temporaryParentScopeId.Value) as NodeScopeBase
            : Scope;
    internal static ProjectScope Scope => _scope;
    protected static ProjectScope _scope;

    internal static NestedScopes NestedScopes
    {
        get
        {
            _nestedScopes ??= new NestedScopes(new Dictionary<ScopeId, IScope>());
            return _nestedScopes;
        }
    }

    private static NestedScopes _nestedScopes;

    internal static Modules Modules
    {
        get
        {
            _modules ??= new Modules(new Dictionary<ModuleRelation, object>(), NestedScopes);
            return _modules;
        }
    }

    private static Modules _modules;
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
        _scope.InitializeScope(ScopeId, ScopeId.Create());
    }

    protected void OnDestroy()
    {
        _nestedScopes = null;
        _modules = null;
        _scope = null;
        _tempConfig = null;
    }
}