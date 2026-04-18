namespace BetStrike.Apostas.Api.Models
{
    public class EstatisticasCompeticaoDto
    {
        public decimal MediaGolosPorJogo { get; set; }
        public decimal VolumeTotalApostado { get; set; }
        public decimal TaxaVitoria1 { get; set; } // Percentagem de vitórias da equipa da casa
        public decimal TaxaVitoriaX { get; set; } // Percentagem de empates
        public decimal TaxaVitoria2 { get; set; } // Percentagem de vitórias da equipa visitante
    }
}