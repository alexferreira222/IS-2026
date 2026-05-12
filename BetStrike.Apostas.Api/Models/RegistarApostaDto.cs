using System.ComponentModel.DataAnnotations;

namespace BetStrike.Apostas.Api.Models
{
    public class RegistarApostaDto
    {
        [Required(ErrorMessage = "O ID do utilizador é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "O ID do utilizador deve ser válido.")]
        public int UtilizadorId
        { get; set; }

        [Required(ErrorMessage = "O código do jogo é obrigatório.")]
        [RegularExpression(@"^FUT-\d{4}-\d{4}$", ErrorMessage = "O código do jogo deve seguir o formato FUT-AAAA-JJNN.")]
        public string CodigoJogo { get; set; }

        [Required(ErrorMessage = "O tipo de aposta é obrigatório.")]
        [RegularExpression(@"^[1X2]$", ErrorMessage = "O tipo de aposta deve ser '1', 'X' ou '2'.")]
        public string TipoAposta { get; set; }

        [Required(ErrorMessage = "O montante é obrigatório.")]
        [Range(0.01, 10000.00)]
        public decimal Montante { get; set; }

        [Required(ErrorMessage = "A OddMomento é obrigatória.")]
        [Range(0.01, 10000.00)]
        public decimal OddMomento { get; set; }
    
    }
}