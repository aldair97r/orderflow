using NetArchTest.Rules;

namespace OrderFlow.ArchitectureTests;

public class LayerTests
{
    [Fact]
    public void OrderingDomain_ShouldNotDependOn_FrameworksOrOtherModules()
    {
        var result = Types.InAssembly(typeof(Ordering.Domain.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "OrderFlow.Ordering.Application",
                "OrderFlow.Ordering.Infrastructure",
                "OrderFlow.Inventory")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void InventoryDomain_ShouldNotDependOn_FrameworksOrOtherModules()
    {
        var result = Types.InAssembly(typeof(Inventory.Domain.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "OrderFlow.Inventory.Application",
                "OrderFlow.Inventory.Infrastructure",
                "OrderFlow.Ordering")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}