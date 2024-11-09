using Godot;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Godot;

public abstract partial class NodeScope : NodeScopeWithConfig<NodeScope.EmptyConfig>
{
    public abstract class EmptyConfig { }
    protected override void Construct(DependencyBinder binder, EmptyConfig config) => Construct(binder);
    protected abstract void Construct(DependencyBinder binder);
}

public abstract partial class NodeScopeWithConfig<TConfig> : NodeScopeBase
{
    [Export] private NodeScopeBase _parentNodeScope;

    protected override void Construct(DependencyBinder binder, object config) => Construct(binder, config is TConfig c ? c : default);
    protected abstract void Construct(DependencyBinder binder, TConfig config);

    public override void _EnterTree()
    {
        // Init ScopeId
        ScopeId = ScopeId.Create();
        _parentNodeScope ??= ProjectScope.ParentNodeScope;
        ParentScopeId = ScopeId.Equals(_parentNodeScope.ScopeId) ? ScopeId.Create() : _parentNodeScope.ScopeId;

        InitializeScope(ScopeId, ParentScopeId.Value, ProjectScope.PopConfig());
    }
}
