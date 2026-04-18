namespace BetStrike.Apostas.Api.Models
{
    public class JogoDto
    {
        public string Codigo { get; set; }
        public DateTime DataHoraInicio { get; set; }
        public string EquipaCasa { get; set; }
        public string EquipaFora { get; set; }
        public string TipoCompeticao { get; set; }
    }
}