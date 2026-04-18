namespace BetStrike.Apostas.Api.Models
{
    public class ApostaResponseDto
    {
        public int IdAposta { get; set; }
        public int IdUtilizador { get; set; }
        public string CodigoJogo { get; set; }
        public string TipoAposta { get; set; }
        public decimal Montante { get; set; }
        public decimal Odd { get; set; }
        public decimal GanhosPotenciais { get; set; }
        public string Estado { get; set; }
        public DateTime DataRegisto { get; set; }
    }

    public class ListarApostasResponseDto
    {
        public List<ApostaResponseDto> Apostas { get; set; } = new();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int Tamanho { get; set; }
    }
}
