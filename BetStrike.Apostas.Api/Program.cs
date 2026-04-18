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
// 3. NOVO: Endpoint para atualizar o estado e marcador de um jogo
app.MapPut("/jogos/{codigoJogo}", async (string codigoJogo, AtualizarJogoDto atualizacao, IConfiguration config) => {
    string connectionString = config.GetConnectionString("ApostasDB") ?? "";

    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        using (SqlCommand cmd = new SqlCommand("sp_AtualizarEstadoJogo", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@CodigoJogo", codigoJogo);
            cmd.Parameters.AddWithValue("@Estado", atualizacao.Estado);
            cmd.Parameters.AddWithValue("@GolosCasa", atualizacao.GolosCasa);
            cmd.Parameters.AddWithValue("@GolosFora", atualizacao.GolosFora);

            try
            {
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                return Results.Ok(new { Mensagem = "Jogo atualizado com sucesso!" });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Erro ao atualizar jogo: {ex.Message}");
            }
        }
    }
});
// 4. NOVO: Endpoint para inserir o resultado final de um jogo
app.MapPost("/resultados", async (ResultadoDto resultado, IConfiguration config) => {
    string connectionString = config.GetConnectionString("ApostasDB") ?? "";

    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        using (SqlCommand cmd = new SqlCommand("sp_InserirResultado", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@CodigoJogo", resultado.CodigoJogo);
            cmd.Parameters.AddWithValue("@GolosCasa", resultado.GolosCasa);
            cmd.Parameters.AddWithValue("@GolosFora", resultado.GolosFora);

            try
            {
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                return Results.Ok(new { Mensagem = "Resultado final guardado na tabela Resultados!" });
            }
            catch (SqlException ex)
            {
                // Captura os nossos erros da Stored Procedure (ex: "jogo já tem resultado")
                return Results.BadRequest(new { Erro = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Erro de sistema: {ex.Message}");
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
public class AtualizarJogoDto
{
    public int Estado { get; set; }
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
}
public class ResultadoDto
{
    public string CodigoJogo { get; set; } = string.Empty;
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
}