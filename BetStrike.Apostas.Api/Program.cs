using BetStrike.Apostas.Api.Controllers;
using Microsoft.Data.SqlClient;
using System.Data;

var builder = WebApplication.CreateBuilder(args);
// 👇 1. ADICIONA ESTA LINHA DO CORS AQUI (Antes do builder.Build!)
builder.Services.AddCors(options => {
    options.AddPolicy("PermitirTudo", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 👇 2. ADICIONA ESTA LINHA AQUI (Logo a seguir ao ambiente de desenvolvimento)
app.UseCors("PermitirTudo");

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
