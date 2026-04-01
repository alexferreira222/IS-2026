namespace BetStrike.Resultados.Api.Models
{
    public class Jogo
    {
        public int Id { get; set; }
        public string Codigo_Jogo { get; set; }
        public DateTime Data { get; set; }
        public string EquipaCasa { get; set; }
        public string EquipaFora { get; set; }
        public int GolosCasa { get; set; }
        public int GolosFora { get; set; }
        public int Estado { get; set; }
    }

    public class AtualizarJogoDto
    {
        public int Estado { get; set; }
        public int GolosCasa { get; set; }
        public int GolosFora { get; set; }
    }
}