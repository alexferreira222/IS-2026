using BetStrike.Apostas.Api.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Apostas.Api.Services
{
    // A classe BackgroundService permite correr código em loop enquanto a API estiver ligada
    public class SincronizadorWorker : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _connectionString;

        public SincronizadorWorker(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Fica a correr em loop até desligares o programa
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ATENÇÃO: Troca a porta (ex: 7123) pela porta real da tua API de Resultados!
                    string urlApiResultados = "https://localhost:7123/api/jogos";

                    // Vai buscar a lista de jogos à Plataforma de Resultados
                    var jogos = await _httpClient.GetFromJsonAsync<List<JogoResultadoDto>>(urlApiResultados, stoppingToken);

                    if (jogos != null)
                    {
                        foreach (var jogo in jogos)
                        {
                            SincronizarJogoNaBaseDeDados(jogo);
                        }
                    }
                }
                catch (Exception ex)
                {
                   
                    Console.WriteLine($"Erro ao sincronizar dados: {ex.Message}");
                }

               
                await Task.Delay(10000, stoppingToken);
            }
        }

        private void SincronizarJogoNaBaseDeDados(JogoResultadoDto jogo)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

               
                bool jogoExiste = false;
                using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(1) FROM Apostas.dbo.Jogo WHERE Codigo = @Codigo", con))
                {
                    checkCmd.Parameters.AddWithValue("@Codigo", jogo.Codigo_Jogo);
                    jogoExiste = (int)checkCmd.ExecuteScalar() > 0;
                }

                if (!jogoExiste)
                {
                    
                    using (SqlCommand insertCmd = new SqlCommand("sp_InserirJogo", con))
                    {
                        insertCmd.CommandType = CommandType.StoredProcedure;
                        insertCmd.Parameters.AddWithValue("@Codigo", jogo.Codigo_Jogo);
                        insertCmd.Parameters.AddWithValue("@DataHoraInicio", jogo.Data);
                        insertCmd.Parameters.AddWithValue("@EquipaCasa", jogo.EquipaCasa);
                        insertCmd.Parameters.AddWithValue("@EquipaFora", jogo.EquipaFora);
                        insertCmd.Parameters.AddWithValue("@TipoCompeticao", "Primeira Liga"); // Valor por defeito

                        try { insertCmd.ExecuteNonQuery(); } catch { /* Ignora se houver conflito */ }
                    }
                }
                else
                {
                    // 3. Se já existir, atualiza o estado e os golos
                    using (SqlCommand updateCmd = new SqlCommand("sp_AtualizarEstadoJogo", con))
                    {
                        updateCmd.CommandType = CommandType.StoredProcedure;
                        updateCmd.Parameters.AddWithValue("@Codigo", jogo.Codigo_Jogo);
                        updateCmd.Parameters.AddWithValue("@NovoEstado", jogo.Estado);
                        updateCmd.Parameters.AddWithValue("@GolosCasa", jogo.GolosCasa);
                        updateCmd.Parameters.AddWithValue("@GolosFora", jogo.GolosFora);

                        try { updateCmd.ExecuteNonQuery(); } catch { /* Ignora transições inválidas antigas */ }
                    }
                }
            }
        }
    }
}