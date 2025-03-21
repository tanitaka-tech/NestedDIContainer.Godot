using System.Threading;
using Fractural.Tasks;

namespace NestedDIContainer.Godot;

public interface IAsyncInitializer
{
    GDTask InitializeAsync(CancellationToken cancellationToken);
}