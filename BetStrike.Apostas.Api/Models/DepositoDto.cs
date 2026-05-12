using System.ComponentModel.DataAnnotations;

namespace BetStrike.Apostas.Api.Models
{
    public class DepositoDto
    {
        [Required(ErrorMessage = "O ID do utilizador é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "O ID do utilizador deve ser válido.")]
        public int UtilizadorId { get; set; }

        [Required(ErrorMessage = "O montante é obrigatório.")]
        [Range(0.01, 10000.00)]
        public decimal Montante { get; set; }
    }

    public class LevantamentoDto
    {
        [Required(ErrorMessage = "O ID do utilizador é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "O ID do utilizador deve ser válido.")]
        public int UtilizadorId { get; set; }

        [Required(ErrorMessage = "O montante é obrigatório.")]
        [Range(0.01, 10000.00)]
        public decimal Montante { get; set; }
    }
}