using BetStrike.Apostas.Api.Controllers;
using Microsoft.Data.SqlClient;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

// 1. Health check endpoint para testar ligação à base de dados
app.MapGet("/testar-ligacao", (IConfiguration config) => {
    string connectionString = config.GetConnectionString("DefaultConnection") ?? "";

    try
    {
        using (SqlConnection conexao = new SqlConnection(connectionString))
        {
            conexao.Open();
            return Results.Ok("Sucesso! A API está ligada à base de dados Apostas.");
        }
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro: {ex.Message}");
    }
});

app.Run();
