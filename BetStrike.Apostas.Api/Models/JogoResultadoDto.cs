using System;

namespace BetStrike.Apostas.Api.Models
{
    public class JogoResultadoDto
    {
        public string Codigo_Jogo { get; set; }
        public DateTime Data { get; set; }
        public string EquipaCasa { get; set; }
        public string EquipaFora { get; set; }
        public int GolosCasa { get; set; }
        public int GolosFora { get; set; }
        public int Estado { get; set; }
    }
}