using System;

namespace BetStrike.Apostas.Api.Models
{
    public class JogoResultadoDto
    {
        // Adicionamos o '= string.Empty;' às propriedades de texto
        public string Codigo_Jogo { get; set; } = string.Empty;
        public DateTime Data { get; set; }
        public string EquipaCasa { get; set; } = string.Empty;
        public string EquipaFora { get; set; } = string.Empty;
        public int GolosCasa { get; set; }
        public int GolosFora { get; set; }
        public int Estado { get; set; }
    }
}