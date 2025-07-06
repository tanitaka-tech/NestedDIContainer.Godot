using Godot;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Godot;

public partial class ProjectContext : ProjectScope
{
    [Export] private NodeScope[] _nodeInstallers;

    public override void _EnterTree()
    {
        ConstructScope(ScopeId.Create(), parentScopeContainer: null, optionExtendScope: new ProjectScopeDefaultExtendScope(this));
    }

    protected override void Construct(DependencyBinder binder)
    {
    }
}
