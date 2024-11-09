using Godot;
using NestedDIContainer.Godot.DefaultDependencies;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Godot;

public partial class ProjectContext : ProjectScope
{
    [Export] private NodeScope[] _nodeInstallers;

    public override void _EnterTree()
    {
        _scope = this;

        ScopeId = ScopeId.Create();
        _scope.InitializeScope(ScopeId, ScopeId.Create());
    }

    protected override void Construct(DependencyBinder binder)
    {
        binder.Bind<INodeFactory>(new NodeFactory());
    }
}
