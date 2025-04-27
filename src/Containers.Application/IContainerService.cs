using Containers.Models;

namespace Containers.Application;

public interface IContainerService
{
    IEnumerable<Container> GetAllContainers();

    bool AddContainer(Container container);
}