using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace BetStrike.GeradorDados
{
    class Program
    {
        // Cliente HTTP para invocar a API REST da Plataforma de Resultados
        private static readonly HttpClient httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            Console.WriteLine("A iniciar a Aplicação Geradora de Dados - BetStrike...");

            // 1. Gerar os 9 jogos da jornada
            List<JogoSimulado> jogosDaJornada = GerarJogosJornada(2025, 1);

            // 2. Iniciar simulação em paralelo para os 9 jogos
            List<Task> tarefasSimulacao = new List<Task>();
            foreach (var jogo in jogosDaJornada)
            {
                // Regista o jogo via API (Agendado)
                // await InserirJogoNaPlataforma(jogo);

                tarefasSimulacao.Add(SimularJogo(jogo));
            }

            // Aguarda que todos os 9 jogos terminem
            await Task.WhenAll(tarefasSimulacao);

            Console.WriteLine("Jornada finalizada!");
        }

        static List<JogoSimulado> GerarJogosJornada(int ano, int jornada)
        {
            var jogos = new List<JogoSimulado>();
            // Exemplo de geração de 1 jogo. Terás de implementar a lógica de emparelhamento aleatório das 18 equipas.
            for (int i = 1; i <= 9; i++)
            {
                jogos.Add(new JogoSimulado
                {
                    // Formato: FUT-AAAA-JJNN
                    CodigoJogo = $"FUT-{ano}-{jornada:D2}{i:D2}",
                    EquipaCasa = $"Equipa Casa {i}",
                    EquipaFora = $"Equipa Fora {i}",
                    Estado = 1 // Agendado
                });
            }
            return jogos;
        }

        static async Task SimularJogo(JogoSimulado jogo)
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            // Transita para "Em Curso"
            jogo.Estado = 2;
            // await AtualizarJogoNaPlataforma(jogo);
            Console.WriteLine($"[{jogo.CodigoJogo}] Apito inicial: {jogo.EquipaCasa} vs {jogo.EquipaFora}");

            // Simula os 90 minutos (atualizando a cada 10 segundos)
            for (int minuto = 0; minuto <= 90; minuto += 10)
            {
                await Task.Delay(10000); // Espera 10 segundos reais

                // Lógica simples para golos (Probabilidade média de 2 a 3 golos por jogo)
                if (rnd.Next(1, 100) <= 15) jogo.GolosCasa++;
                if (rnd.Next(1, 100) <= 15) jogo.GolosFora++;

                Console.WriteLine($"[{jogo.CodigoJogo}] Minuto {minuto}': {jogo.EquipaCasa} {jogo.GolosCasa} - {jogo.GolosFora} {jogo.EquipaFora}");

                // await AtualizarJogoNaPlataforma(jogo);
            }

            // Transita para "Finalizado"
            jogo.Estado = 3;
            // await AtualizarJogoNaPlataforma(jogo);
            Console.WriteLine($"[{jogo.CodigoJogo}] FINAL DO JOGO!");
        }
    }

    public class JogoSimulado
    {
        public string CodigoJogo { get; set; }
        public string EquipaCasa { get; set; }
        public string EquipaFora { get; set; }
        public int GolosCasa { get; set; } = 0;
        public int GolosFora { get; set; } = 0;
        public int Estado { get; set; }
    }
}

