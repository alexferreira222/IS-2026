using System.ComponentModel.DataAnnotations;

namespace BetStrike.Apostas.Api.Models
{
    public class RegistarApostaDto
    {
        [Required(ErrorMessage = "O ID do utilizador é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "O ID do utilizador deve ser válido.")]
        public int IdUtilizador { get; set; }

        [Required(ErrorMessage = "O código do jogo é obrigatório.")]
        [RegularExpression(@"^FUT-\d{4}-\d{4}$", ErrorMessage = "O código do jogo deve seguir o formato FUT-AAAA-JJNN.")]
        public string CodigoJogo { get; set; }

        [Required(ErrorMessage = "O tipo de aposta é obrigatório.")]
        [RegularExpression(@"^[1X2]$", ErrorMessage = "O tipo de aposta deve ser '1', 'X' ou '2'.")]
        public string TipoAposta { get; set; }

        [Required(ErrorMessage = "O montante é obrigatório.")]
        [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "O montante deve estar entre €0.01 e €999,999.99.")]
        public decimal Montante { get; set; }

        [Required(ErrorMessage = "A odd é obrigatória.")]
        [Range(typeof(decimal), "1.01", "999999.99", ErrorMessage = "A odd deve estar entre 1.01 e 999,999.99.")]
        public decimal Odd { get; set; }
    }
}