using Microsoft.Data.SqlClient;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 1. Endpoint de teste de ligação
app.MapGet("/testar-ligacao", (IConfiguration config) => {
    // O '?? ""' resolve o aviso amarelo CS8600
    string connectionString = config.GetConnectionString("ApostasDB") ?? "";

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

// 2. Endpoint para inserir jogo via Stored Procedure
app.MapPost("/jogos", async (JogoDto jogo, IConfiguration config) => {
    string connectionString = config.GetConnectionString("ApostasDB") ?? "";

    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        using (SqlCommand cmd = new SqlCommand("sp_InserirJogo", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@CodigoJogo", jogo.CodigoJogo);
            cmd.Parameters.AddWithValue("@DataJogo", jogo.DataJogo);
            cmd.Parameters.AddWithValue("@HoraInicio", jogo.HoraInicio);
            cmd.Parameters.AddWithValue("@EquipaCasa", jogo.EquipaCasa);
            cmd.Parameters.AddWithValue("@EquipaFora", jogo.EquipaFora);
            cmd.Parameters.AddWithValue("@Competicao", jogo.Competicao);
            cmd.Parameters.AddWithValue("@Estado", jogo.Estado);

            try
            {
                await conn.OpenAsync();
                var idGerado = await cmd.ExecuteScalarAsync();
                return Results.Ok(new { Mensagem = "Jogo inserido com sucesso!", IdJogo = idGerado });
            }
            catch (SqlException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
        }
    }
});

app.Run();

// Modelo para receber os dados
// O '= string.Empty;' resolve os avisos amarelos CS8618
public class JogoDto
{
    public string CodigoJogo { get; set; } = string.Empty;
    public DateTime DataJogo { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public string EquipaCasa { get; set; } = string.Empty;
    public string EquipaFora { get; set; } = string.Empty;
    public string Competicao { get; set; } = string.Empty;

    public int Estado { get; set; }
}