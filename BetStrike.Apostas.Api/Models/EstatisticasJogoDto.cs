namespace BetStrike.Apostas.Api.Models
{
    public class EstatisticasJogoDto
    {
        public decimal TotalApostado { get; set; }
        public int ApostasTipo1 { get; set; }
        public int ApostasTipoX { get; set; }
        public int ApostasTipo2 { get; set; }
        public int ApostasPendentes { get; set; }
        public int ApostasGanhas { get; set; }
        public int ApostasPerdidas { get; set; }
        public int ApostasAnuladas { get; set; }
        public decimal MargemPlataforma { get; set; } 
    }
}