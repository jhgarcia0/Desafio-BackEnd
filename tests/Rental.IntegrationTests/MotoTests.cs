using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Rental.IntegrationTests;
[Collection("NoParallel")]
public class MotoTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await DbTestUtils.TruncateAllAsync();
    }
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_Motos_Should_Create_New_Moto()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/motos", new
        {
            identifier = "MOT-TEST-001",
            year = 2025,
            model = "Yamaha",
            plate = "XYZ9T99"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!["plate"].ToString().Should().Be("XYZ9T99");
    }
    [Fact]
    public async Task Post_Motos_With_Duplicate_Plate_Should_Return_Conflict()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var payload = new { identifier = "MOT-A", year = 2024, model = "CG 160", plate = "AAA1B11" };

        var r1 = await client.PostAsJsonAsync("/motos", payload);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        var r2 = await client.PostAsJsonAsync("/motos", payload);
        r2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task Delete_Moto_By_Id_Should_Return_NoContent()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var create = await client.PostAsJsonAsync("/motos", new
        {
            identifier = "DEL1",
            year = 2024,
            model = "CG",
            plate = "DEL1A11"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var moto = await create.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = moto!["id"].ToString();

        var del = await client.DeleteAsync($"/motos/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await client.GetAsync($"/motos/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Moto_Should_Return_NotFound_When_Id_Does_Not_Exist()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var randomId = Guid.NewGuid();
        var del = await client.DeleteAsync($"/motos/{randomId}");
        del.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task Get_Moto_By_Id_Should_Return_Ok_With_Body()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var create = await client.PostAsJsonAsync("/motos", new {
            identifier = "GET1", year = 2024, model = "CG", plate = "GET1A11"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = created!["id"].ToString();

        var resp = await client.GetAsync($"/motos/{id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!["id"].ToString().Should().Be(id);
        body["plate"].ToString().Should().Be("GET1A11");
    }

    [Fact]
    public async Task Get_Moto_By_Id_Should_Return_NotFound_When_Does_Not_Exist()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var randomId = Guid.NewGuid();
        var resp = await client.GetAsync($"/motos/{randomId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Fact]
    public async Task Put_Moto_Update_Plate_Should_Work()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var create = await client.PostAsJsonAsync("/motos", new { identifier="UP1", year=2024, model="CG", plate="UP1A11" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var moto = await create.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = moto!["id"].ToString();

        var put = await client.PutAsJsonAsync($"/motos/{id}/placa", new { plate = "UP9Z99" });
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await put.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        updated!["plate"].ToString().Should().Be("UP9Z99");
    }

    [Fact]
    public async Task Put_Moto_Update_Plate_Should_Return_Conflict_When_Duplicate()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var a = await client.PostAsJsonAsync("/motos", new { identifier="A", year=2024, model="CG", plate="DUP1A11" });
        var b = await client.PostAsJsonAsync("/motos", new { identifier="B", year=2024, model="CG", plate="DUP2B22" });
        a.StatusCode.Should().Be(HttpStatusCode.Created);
        b.StatusCode.Should().Be(HttpStatusCode.Created);

        var motoB = await b.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var idB = motoB!["id"].ToString();

        var put = await client.PutAsJsonAsync($"/motos/{idB}/placa", new { plate = "DUP1A11" });
        put.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Put_Moto_Update_Plate_Should_Return_BadRequest_When_Empty()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var create = await client.PostAsJsonAsync("/motos", new { identifier="BAD", year=2024, model="CG", plate="BAD1A11" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var moto = await create.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = moto!["id"].ToString();

        var put = await client.PutAsJsonAsync($"/motos/{id}/placa", new { plate = "" });
        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }



}
