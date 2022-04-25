using System.Threading.Tasks;
using Ingredients.Protos;
using Xunit;

namespace Ingredients.Tests;

public class IngredientsServiceTests : IClassFixture<IngredientsApplicationFactory>
{
    private readonly IngredientsApplicationFactory _factory;

    public IngredientsServiceTests(IngredientsApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetsToppings()
    {
        var client = _factory.CreateGrpcClient();
        var response = await client.GetToppingsAsync(new GetToppingsRequest());

        Assert.Collection(response.Toppings,
            t => { Assert.Equal("cheese", t.Id); },
            t => { Assert.Equal("tomato", t.Id); }
        );
    }
}