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
            _connectionString = configuration.GetConnectionString("DefaultConnection");
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

            _logger.LogInformation($"Depósito de {dto.Montante}€ para utilizador {dto.IdUtilizador}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Pagamentos_DepositoFicticio", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@IdUtilizador", dto.IdUtilizador);
                    cmd.Parameters.AddWithValue("@Montante", dto.Montante);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        _logger.LogInformation($"Depósito realizado com sucesso para utilizador {dto.IdUtilizador}");

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

            _logger.LogInformation($"Levantamento de {dto.Montante}€ para utilizador {dto.IdUtilizador}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Pagamentos_Levantamento", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@IdUtilizador", dto.IdUtilizador);
                    cmd.Parameters.AddWithValue("@Montante", dto.Montante);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        _logger.LogInformation($"Levantamento realizado com sucesso para utilizador {dto.IdUtilizador}");

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
        [HttpGet("saldo/{idUtilizador}")]
        public IActionResult ObterSaldo(int idUtilizador)
        {
            if (idUtilizador <= 0)
                return BadRequest(new { erro = "O ID do utilizador deve ser válido." });

            _logger.LogInformation($"Obtendo saldo do utilizador {idUtilizador}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Pagamentos_ObterSaldo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@IdUtilizador", idUtilizador);

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return Ok(new
                                {
                                    idUtilizador = Convert.ToInt32(reader["IdUtilizador"]),
                                    saldo = Convert.ToDecimal(reader["Saldo"]),
                                    dataConsulta = DateTime.UtcNow
                                });
                            }

                            return NotFound(new { erro = "Utilizador não encontrado." });
                        }
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao obter saldo: {ex.Message}");
                        return StatusCode(500, new { erro = "Erro ao recuperar saldo." });
                    }
                }
            }
        }
    }
}