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
    protected override void Construct(DependencyBinder binder, object config) => Construct(binder, config is TConfig c ? c : default);
    protected abstract void Construct(DependencyBinder binder, TConfig config);

    public override void _EnterTree()
    {
        var parentScope = ProjectScope.PopParentScope() ?? ProjectScope.Scope;
        ConstructScope(ScopeId.Create(), parentScope.ScopeContainer, ProjectScope.PopConfig(), optionExtendScope: null);
    }
}
