using BetStrike.Apostas.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Apostas.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PagamentosController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<PagamentosController> _logger;

        public PagamentosController(IConfiguration configuration, ILogger<PagamentosController> logger)
        {
            _connectionString = configuration.GetConnectionString("Pagamentos") ?? ""; _logger = logger;
            _logger = logger;
        }

        /// <summary>
        /// Realizar um depósito fictício na conta do utilizador
        /// </summary>
        [HttpPost("deposito")]
        public IActionResult FazerDeposito([FromBody] DepositoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation($"Depósito de {dto.Montante}€ para utilizador {dto.UtilizadorId
                
                }");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Pagamentos_DepositoFicticio", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@UtilizadorId", dto.UtilizadorId);
                    cmd.Parameters.AddWithValue("@Montante", dto.Montante);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        _logger.LogInformation($"Depósito realizado com sucesso para utilizador {dto.UtilizadorId}");

                        return Ok(new
                        {
                            mensagem = $"Depósito de {dto.Montante}€ realizado com sucesso.",
                            montante = dto.Montante,
                            dataOperacao = DateTime.UtcNow
                        });
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao processar depósito: {ex.Message}");
                        return BadRequest(new { erro = $"Erro ao processar depósito: {ex.Message}" });
                    }
                }
            }
        }

        /// <summary>
        /// Realizar um levantamento da conta do utilizador
        /// </summary>
        [HttpPost("levantamento")]
        public IActionResult FazerLevantamento([FromBody] LevantamentoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation($"Levantamento de {dto.Montante}€ para utilizador {dto.UtilizadorId}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Pagamentos_Levantamento", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@UtilizadorId", dto.UtilizadorId);
                    cmd.Parameters.AddWithValue("@Montante", dto.Montante);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        _logger.LogInformation($"Levantamento realizado com sucesso para utilizador {dto.UtilizadorId}");

                        return Ok(new
                        {
                            mensagem = $"Levantamento de {dto.Montante}€ realizado com sucesso.",
                            montante = dto.Montante,
                            dataOperacao = DateTime.UtcNow
                        });
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao processar levantamento: {ex.Message}");

                        if (ex.Number == 50000)
                            return BadRequest(new { erro = ex.Message });

                        return BadRequest(new { erro = $"Erro ao processar levantamento: {ex.Message}" });
                    }
                }
            }
        }

        /// <summary>
        /// Obter saldo da conta do utilizador
        /// </summary>
        [HttpGet("saldo/{UtilizadorId}")]
        public IActionResult ObterSaldo(int UtilizadorId)
        {
            if (UtilizadorId <= 0) return BadRequest(new { erro = "O ID do utilizador deve ser válido." });

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_Pagamentos_ObterSaldo", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@UtilizadorId", UtilizadorId);

                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Vamos ver o que é que a base de dados enviou na primeira coluna
                                var valorVindoDaBD = reader[0];

                                return Ok(new
                                {
                                    UtilizadorId = UtilizadorId,
                                    saldo = Convert.ToDecimal(valorVindoDaBD),
                                    dataConsulta = DateTime.UtcNow
                                });
                            }
                            return NotFound(new { erro = "Utilizador não encontrado." });
                        }
                        }
                }
            }
            catch (Exception ex) // <--- APANHA TUDO AGORA!
            {
                // Agora o C# é OBRIGADO a dizer o que falhou!
                return StatusCode(500, new { erro = $"A VERDADE DO SALDO: {ex.Message}" });
            }
        }
    }
}
