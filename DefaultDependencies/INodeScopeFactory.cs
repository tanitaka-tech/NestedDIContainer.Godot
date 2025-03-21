using Godot;

namespace NestedDIContainer.Godot.DefaultDependencies;

public interface INodeScopeFactory
{
    T Instantiate<T>(PackedScene packedScene, Node parent, object config = null) where T : Node;

    /// <summary>
    /// Create an instance of the specified type from the given instance placeholder.
    /// NOTE: Call at the timing of _Ready as an error will occur if called at the timing of _EnterTree
    /// </summary>
    T Instantiate<T>(InstancePlaceholder instancePlaceholder, object config = null) where T : Node;

    NodeScopeWithConfig<TConfig> InstantiateWithConfig<TConfig>(PackedScene packedScene, TConfig config, Node parent) where TConfig : class;
}

public interface IChildSceneScopeFactory : INodeScopeFactory {}