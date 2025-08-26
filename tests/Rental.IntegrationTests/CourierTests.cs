using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Rental.IntegrationTests;

public class CourierTests : IAsyncLifetime
{
    public Task InitializeAsync() => DbTestUtils.TruncateAllAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_Couriers_Should_Create()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var resp = await client.PostAsJsonAsync("/couriers", new {
            identifier = "CR-001",
            name = "John Doe",
            cnpj = "11222333000199",
            birthDate = new DateTime(1998,5,10),
            cnhNumber = "CNH123456",
            cnhType = "A+B",
            cnhImagePath = (string?)null
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!["cnpj"].ToString().Should().Be("11222333000199");
    }

    [Fact]
    public async Task Post_Couriers_Duplicate_Cnpj_Should_Conflict()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var payload = new {
            identifier = "CR-002",
            name = "Jane",
            cnpj = "99888777000155",
            birthDate = new DateTime(1990,1,1),
            cnhNumber = "CNH555111",
            cnhType = "A",
            cnhImagePath = (string?)null
        };

        (await client.PostAsJsonAsync("/couriers", payload)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/couriers", payload)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_Couriers_Duplicate_CnhNumber_Should_Conflict()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var p1 = new { identifier="CR-003", name="A", cnpj="55667788000144", birthDate=new DateTime(1995,7,7), cnhNumber="CNHX1111", cnhType="B",   cnhImagePath=(string?)null };
        var p2 = new { identifier="CR-004", name="B", cnpj="11220099000133", birthDate=new DateTime(2000,3,2), cnhNumber="CNHX1111", cnhType="A+B", cnhImagePath=(string?)null };

        (await client.PostAsJsonAsync("/couriers", p1)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/couriers", p2)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_Couriers_Invalid_CnhType_Should_BadRequest()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var resp = await client.PostAsJsonAsync("/couriers", new {
            identifier = "CR-005",
            name = "Erro Tipo",
            cnpj = "11111111000199",
            birthDate = new DateTime(2001,1,1),
            cnhNumber = "CNH000111",
            cnhType = "C",
            cnhImagePath = (string?)null
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_Courier_By_Id_Should_Work_And_NotFound_When_Missing()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var create = await client.PostAsJsonAsync("/couriers", new {
            identifier="CR-006", name="Get Guy", cnpj="22334455000188",
            birthDate=new DateTime(1999,9,9), cnhNumber="CNHGET123", cnhType="A", cnhImagePath=(string?)null
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await create.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = body!["id"].ToString();

        (await client.GetAsync($"/couriers/{id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/couriers/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
