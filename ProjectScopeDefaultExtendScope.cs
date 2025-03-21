using NestedDIContainer.Godot.DefaultDependencies;
using TanitakaTech.NestedDIContainer;

namespace NestedDIContainer.Godot;

public class ProjectScopeDefaultExtendScope : IExtendScope
{
    private INodeScopeFactory NodeScopeFactory { get; }

    public ProjectScopeDefaultExtendScope(INodeScopeFactory nodeScopeFactory)
    {
        NodeScopeFactory = nodeScopeFactory;
    }

    void IExtendScope.Construct(DependencyBinder binder)
    {
        binder.Bind<INodeScopeFactory>(NodeScopeFactory);
    }
}