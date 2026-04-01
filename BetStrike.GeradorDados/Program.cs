using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BetStrike.GeradorDados
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        // Garante que este porto (7083) é o mesmo que aparece na tua janela da API
        private static string apiUrl = "https://localhost:7083/jogos";

        static async Task Main(string[] args)
        {
            Console.WriteLine("A iniciar o Gerador - BetStrike...");

            List<JogoSimulado> jogos = GerarJogosJornada(2025, 1);

            // 1. Tentar inserir os 9 jogos na Base de Dados via API
            foreach (var jogo in jogos)
            {
                await InserirJogoNaPlataforma(jogo);
            }

            Console.WriteLine("\n--- A INICIAR SIMULAÇÃO DOS JOGOS ---\n");

            // 2. Simular os 9 jogos ao mesmo tempo
            List<Task> tarefas = new List<Task>();
            foreach (var jogo in jogos)
            {
                tarefas.Add(SimularJogo(jogo));
            }
            await Task.WhenAll(tarefas);

            Console.WriteLine("\nJornada finalizada! Prime ENTER para sair.");
            Console.ReadLine(); // Impede a consola de fechar sozinha!
        }

        static async Task InserirJogoNaPlataforma(JogoSimulado jogo)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(apiUrl, jogo);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[API] Jogo {jogo.CodigoJogo} registado com sucesso na Base de Dados.");
                }
                else
                {
                    // Agora lê a mensagem de erro que vem da API/SQL Server
                    var erroMsg = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API] Erro ao registar {jogo.CodigoJogo}: {erroMsg}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO DE LIGAÇÃO] Falha na comunicação com a API: {ex.Message}");
            }
        }

        static async Task AtualizarJogoNaPlataforma(JogoSimulado jogo)
        {
            try
            {
                var atualizacao = new
                {
                    Estado = jogo.Estado,
                    GolosCasa = jogo.GolosCasa,
                    GolosFora = jogo.GolosFora
                };

                var response = await httpClient.PutAsJsonAsync($"{apiUrl}/{jogo.CodigoJogo}", atualizacao);

                if (!response.IsSuccessStatusCode)
                {
                    // Agora ele avisa se a API recusar a atualização!
                    var erroMsg = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API ERRO] Falha ao atualizar {jogo.CodigoJogo}: {erroMsg}");
                }
            }
            catch (Exception ex)
            {
                // Agora ele avisa se não conseguir ligar à API!
                Console.WriteLine($"[ERRO DE LIGAÇÃO] Falha ao atualizar: {ex.Message}");
            }
        }
        static List<JogoSimulado> GerarJogosJornada(int ano, int jornada)
        {
            var jogos = new List<JogoSimulado>();
            for (int i = 1; i <= 9; i++)
            {
                jogos.Add(new JogoSimulado
                {
                    CodigoJogo = $"FUT-{ano}-{jornada:D2}{i:D2}",
                    EquipaCasa = $"Equipa Casa {i}",
                    EquipaFora = $"Equipa Fora {i}",
                    DataJogo = DateTime.Now.Date,
                    HoraInicio = DateTime.Now.TimeOfDay,
                    Competicao = "Primeira Liga",
                    Estado = 1 // Estado inicial: Agendado
                });
            }
            return jogos;
        }

        static async Task SimularJogo(JogoSimulado jogo)
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            // 1. Muda o estado para Em Curso e ATUALIZA A API (sem barras //)
            jogo.Estado = 2;
            await AtualizarJogoNaPlataforma(jogo);

            Console.WriteLine($"[{jogo.CodigoJogo}] Apito inicial: {jogo.EquipaCasa} vs {jogo.EquipaFora}");

            for (int minuto = 0; minuto <= 90; minuto += 10)
            {
                await Task.Delay(10000); // Espera 10 segundos reais

                if (rnd.Next(1, 100) <= 15) jogo.GolosCasa++;
                if (rnd.Next(1, 100) <= 15) jogo.GolosFora++;

                Console.WriteLine($"[{jogo.CodigoJogo}] Minuto {minuto}': {jogo.EquipaCasa} {jogo.GolosCasa} - {jogo.GolosFora} {jogo.EquipaFora}");

                // 2. ATUALIZA A API com os novos golos (sem barras //)
                await AtualizarJogoNaPlataforma(jogo);
            }


            // 3. Muda o estado para Finalizado e ATUALIZA A API
            jogo.Estado = 3;
            await AtualizarJogoNaPlataforma(jogo);

            Console.WriteLine($"[{jogo.CodigoJogo}] FINAL DO JOGO!");

            // 4. INSERE O RESULTADO FINAL (ADICIONAR ESTA LINHA)
            await InserirResultadoFinal(jogo);
        }


        static async Task InserirResultadoFinal(JogoSimulado jogo)
        {
            try
            {
                var resultadoFinal = new
                {
                    CodigoJogo = jogo.CodigoJogo,
                    GolosCasa = jogo.GolosCasa,
                    GolosFora = jogo.GolosFora
                };

                // Faz o POST para o nosso novo endpoint
                var response = await httpClient.PostAsJsonAsync("https://localhost:7083/resultados", resultadoFinal);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{jogo.CodigoJogo}] --- RESULTADO FINAL GRAVADO NA BD ---");
                }
                else
                {
                    var erroMsg = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[{jogo.CodigoJogo}] ERRO NO RESULTADO: {erroMsg}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro de ligação ao enviar resultado: {ex.Message}");
            }
        }
        public class JogoSimulado
        {
            public string CodigoJogo { get; set; } = string.Empty;
            public string EquipaCasa { get; set; } = string.Empty;
            public string EquipaFora { get; set; } = string.Empty;
            public DateTime DataJogo { get; set; }
            public TimeSpan HoraInicio { get; set; }
            public string Competicao { get; set; } = string.Empty;
            public int Estado { get; set; }
            public int GolosCasa { get; set; } = 0;
            public int GolosFora { get; set; } = 0;
        }
    }
}