using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BetStrike.GeradorDados
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string apiUrl = "https://localhost:7083/api/jogos";

        static async Task Main(string[] args)
        {
            Console.WriteLine("A iniciar o Gerador - BetStrike...");

            List<JogoSimulado> jogosGerados = GerarJogosJornada();
            List<JogoSimulado> jogosParaSimular = new List<JogoSimulado>();

            Console.WriteLine("\n--- A INSERIR JOGOS NA BD ---");

            foreach (var jogo in jogosGerados)
            {
                bool inseridoComSucesso = await InserirJogoNaPlataforma(jogo);
                if (inseridoComSucesso)
                {
                    jogosParaSimular.Add(jogo);
                }
            }

            if (jogosParaSimular.Count == 0)
            {
                Console.WriteLine("\nNenhum jogo novo foi inserido. A simulação foi cancelada!");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("\n====================================================");
            Console.WriteLine("Fase 1 Concluída: Calendário Publicado com Sucesso!");
            Console.WriteLine("Os jogos estão agora no estado 'Agendado' (1).");
            Console.WriteLine("Podes ir ao site e realizar as tuas apostas agora.");
            Console.WriteLine("====================================================");
            Console.WriteLine("\nPrime [ENTER] quando quiseres iniciar a simulação (Fase 2)...");
            Console.ReadLine();

            Console.WriteLine("\n--- A INICIAR SIMULAÇÃO DOS JOGOS VÁLIDOS ---\n");

            List<Task> tarefas = new List<Task>();
            foreach (var jogo in jogosParaSimular)
            {
                tarefas.Add(SimularJogo(jogo));
            }
            await Task.WhenAll(tarefas);

            Console.WriteLine("\nJornada finalizada! Prime ENTER para sair.");
            Console.ReadLine();
        }

        static async Task<bool> InserirJogoNaPlataforma(JogoSimulado jogo)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(apiUrl, jogo);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[API] Jogo {jogo.Codigo} registado com sucesso.");
                    return true;
                }
                else
                {
                    var erroMsg = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API] Rejeitado {jogo.Codigo}: {erroMsg}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO DE LIGAÇÃO] Falha na comunicação: {ex.Message}");
                return false;
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

                var response = await httpClient.PutAsJsonAsync($"{apiUrl}/{jogo.Codigo}", atualizacao);

                if (!response.IsSuccessStatusCode)
                {
                    var erroMsg = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API ERRO] Falha ao atualizar {jogo.Codigo}: {erroMsg}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO DE LIGAÇÃO] Falha ao atualizar: {ex.Message}");
            }
        }

        static List<JogoSimulado> GerarJogosJornada()
        {
            var jogos = new List<JogoSimulado>();
            var rnd = new Random();
            string ano = DateTime.Now.ToString("yyyy");

            string[] equipas = { "Benfica", "Porto", "Sporting", "Braga", "Vitoria SC", "Moreirense",
                                  "Arouca", "Gil Vicente", "Famalicao", "Boavista", "Estoril", "Rio Ave",
                                  "Chaves", "Vizela", "Farense", "Estrela Amadora", "Portimonense", "Casa Pia" };

            var equipasBaralhadas = equipas.OrderBy(x => rnd.Next()).ToList();

            for (int i = 0; i < 9; i++)
            {
                string idAleatorio = rnd.Next(1000, 9999).ToString();

                jogos.Add(new JogoSimulado
                {
                    Codigo = $"FUT-{ano}-{idAleatorio}",
                    EquipaCasa = equipasBaralhadas[i],
                    EquipaFora = equipasBaralhadas[i + 9],
                    DataHoraInicio = DateTime.Now.AddHours(2),
                    TipoCompeticao = "Primeira Liga",
                    Estado = 1
                });
            }
            return jogos;
        }

        static async Task SimularJogo(JogoSimulado jogo)
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            jogo.Estado = 2;
            await AtualizarJogoNaPlataforma(jogo);

            Console.WriteLine($"[{jogo.Codigo}] Apito inicial: {jogo.EquipaCasa} vs {jogo.EquipaFora}");

            for (int minuto = 0; minuto <= 90; minuto += 10)
            {
                await Task.Delay(10000);

                if (rnd.Next(1, 100) <= 15) jogo.GolosCasa++;
                if (rnd.Next(1, 100) <= 15) jogo.GolosFora++;

                Console.WriteLine($"[{jogo.Codigo}] Minuto {minuto}': {jogo.EquipaCasa} {jogo.GolosCasa} - {jogo.GolosFora} {jogo.EquipaFora}");

                await AtualizarJogoNaPlataforma(jogo);
            }

            jogo.Estado = 3;
            await AtualizarJogoNaPlataforma(jogo);

            Console.WriteLine($"[{jogo.Codigo}] FINAL DO JOGO!");
        }

        public class JogoSimulado
        {
            public string Codigo { get; set; } = string.Empty;
            public string EquipaCasa { get; set; } = string.Empty;
            public string EquipaFora { get; set; } = string.Empty;
            public DateTime DataHoraInicio { get; set; }
            public string TipoCompeticao { get; set; } = string.Empty;
            public int Estado { get; set; }
            public int GolosCasa { get; set; } = 0;
            public int GolosFora { get; set; } = 0;
        }
    }
}