using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;

namespace Rental.IntegrationTests;

[Collection("NoParallel")]
public class CourierTests : IAsyncLifetime
{
    public Task InitializeAsync() => DbTestUtils.TruncateAllAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static DateTime Utc(int y, int m, int d) => new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Post_Couriers_Should_Create()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var resp = await client.PostAsJsonAsync("/couriers", new
        {
            identifier = "CR-001",
            name = "John Doe",
            cnpj = "11222333000199",
            birthDate = Utc(1998, 5, 10),
            cnhNumber = "CNH123456",
            cnhType = "A+B",
            cnhImagePath = (string?)null
        });

        var bodyText = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.Created, $"body: {bodyText}");

        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!["cnpj"].ToString().Should().Be("11222333000199");
    }

    [Fact]
    public async Task Post_Couriers_Duplicate_Cnpj_Should_Conflict()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var payload = new
        {
            identifier = "CR-002",
            name = "Jane",
            cnpj = "99888777000155",
            birthDate = Utc(1990, 1, 1),
            cnhNumber = "CNH555111",
            cnhType = "A",
            cnhImagePath = (string?)null
        };

        var r1 = await client.PostAsJsonAsync("/couriers", payload);
        await r1.Content.ReadAsStringAsync();
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        var r2 = await client.PostAsJsonAsync("/couriers", payload);
        var bodyText = await r2.Content.ReadAsStringAsync();
        r2.StatusCode.Should().Be(HttpStatusCode.Conflict, $"body: {bodyText}");
    }

    [Fact]
    public async Task Post_Couriers_Duplicate_CnhNumber_Should_Conflict()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var p1 = new { identifier = "CR-003", name = "A", cnpj = "55667788000144", birthDate = Utc(1995, 7, 7), cnhNumber = "CNHX1111", cnhType = "B", cnhImagePath = (string?)null };
        var p2 = new { identifier = "CR-004", name = "B", cnpj = "11220099000133", birthDate = Utc(2000, 3, 2), cnhNumber = "CNHX1111", cnhType = "A+B", cnhImagePath = (string?)null };

        var r1 = await client.PostAsJsonAsync("/couriers", p1);
        await r1.Content.ReadAsStringAsync();
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        var r2 = await client.PostAsJsonAsync("/couriers", p2);
        var bodyText = await r2.Content.ReadAsStringAsync();
        r2.StatusCode.Should().Be(HttpStatusCode.Conflict, $"body: {bodyText}");
    }

    [Fact]
    public async Task Post_Couriers_Invalid_CnhType_Should_BadRequest()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var resp = await client.PostAsJsonAsync("/couriers", new
        {
            identifier = "CR-005",
            name = "Erro Tipo",
            cnpj = "11111111000199",
            birthDate = Utc(2001, 1, 1),
            cnhNumber = "CNH000111",
            cnhType = "C",
            cnhImagePath = (string?)null
        });

        var bodyText = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"body: {bodyText}");
    }

    [Fact]
    public async Task Get_Courier_By_Id_Should_Work_And_NotFound_When_Missing()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var create = await client.PostAsJsonAsync("/couriers", new
        {
            identifier = "CR-006",
            name = "Get Guy",
            cnpj = "22334455000188",
            birthDate = Utc(1999, 9, 9),
            cnhNumber = "CNHGET123",
            cnhType = "A",
            cnhImagePath = (string?)null
        });

        var bodyCreateText = await create.Content.ReadAsStringAsync();
        create.StatusCode.Should().Be(HttpStatusCode.Created, $"body: {bodyCreateText}");

        var body = await create.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = body!["id"].ToString();

        (await client.GetAsync($"/couriers/{id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/couriers/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Fact]
    public async Task Upload_Cnh_Should_Save_File_And_Update_Path()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();

        var create = await client.PostAsJsonAsync("/couriers", new {
            identifier="CR-UP", name="Upload", cnpj="99887766000155",
            birthDate=new DateTime(1995,5,5,0,0,0,DateTimeKind.Utc),
            cnhNumber="CNH-UP-001", cnhType="A", cnhImagePath=(string?)null
        });
        create.EnsureSuccessStatusCode();

        var body = await create.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        var id = body!["id"].GetString()!;

        using var content = new MultipartFormDataContent();
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "test.png");

        var resp = await client.PostAsync($"/couriers/{id}/cnh", content);
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var up = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        up!["path"].GetString().Should().Contain(".png");
    }


}
