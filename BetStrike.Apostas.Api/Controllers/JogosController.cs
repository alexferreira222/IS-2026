using BetStrike.Apostas.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace BetStrike.Apostas.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JogosController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<JogosController> _logger;

        public JogosController(IConfiguration configuration, ILogger<JogosController> logger)
        {
            _connectionString = configuration.GetConnectionString("Apostas") ?? ""; _logger = logger;
        }

        /// <summary>
        /// Inserir um novo jogo na plataforma
        /// </summary>
        [HttpPost]
        public IActionResult InserirJogo([FromBody] JogoDto dto)
        {
            // Valida o formato exato do código (FUT-AAAA-JJNN) antes de persistir, usando Regex
            if (string.IsNullOrWhiteSpace(dto.Codigo) || !Regex.IsMatch(dto.Codigo, @"^FUT-\d{4}-\d{4}$"))
            {
                return BadRequest(new 
                { 
                    erro = "O código do jogo deve seguir o formato exato FUT-AAAA-JJNN (ex: FUT-2026-0101)."
                });
            }

            if (dto.DataHoraInicio < DateTime.UtcNow)
            {
                return BadRequest(new 
                { 
                    erro = "A data e hora do jogo não pode ser no passado."
                });
            }

            _logger.LogInformation($"Inserindo novo jogo: {dto.Codigo}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Apostas_InserirJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@Codigo", dto.Codigo);
                    cmd.Parameters.AddWithValue("@DataHoraInicio", dto.DataHoraInicio);
                    cmd.Parameters.AddWithValue("@EquipaCasa", dto.EquipaCasa ?? string.Empty);
                    cmd.Parameters.AddWithValue("@EquipaFora", dto.EquipaFora ?? string.Empty);
                    cmd.Parameters.AddWithValue("@TipoCompeticao", dto.TipoCompeticao ?? string.Empty);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        _logger.LogInformation($"Jogo {dto.Codigo} inserido com sucesso");

                        return CreatedAtAction(nameof(ObterDetalhesJogo), new { codigo = dto.Codigo }, new
                        {
                            codigo = dto.Codigo,
                            mensagem = "Jogo inserido com sucesso na plataforma de apostas."
                        });
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao inserir jogo {dto.Codigo}: {ex.Message}");

                        if (ex.Number == 50000) 
                            return Conflict(new { erro = ex.Message });

                        return StatusCode(500, new { erro = "Erro interno ao inserir jogo." });
                    }
                }
            }
        }

        /// <summary>
        /// Atualizar o estado e marcador de um jogo
        /// </summary>
        [HttpPut("{codigo}")]
        public IActionResult AtualizarJogo(string codigo, [FromBody] AtualizarJogoDto dto)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return BadRequest(new { erro = "O código do jogo é obrigatório." });

            if (dto.Estado < 0 || dto.Estado > 3)
                return BadRequest(new { erro = "O estado do jogo deve estar entre 0 e 3." });

            if (dto.GolosCasa < 0 || dto.GolosFora < 0)
                return BadRequest(new { erro = "Os golos não podem ser negativos." });

            _logger.LogInformation($"Atualizando jogo {codigo} - Estado: {dto.Estado}, Golos: {dto.GolosCasa}x{dto.GolosFora}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Apostas_AtualizarJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@NovoEstado", dto.Estado);
                    cmd.Parameters.AddWithValue("@GolosCasa", dto.GolosCasa);
                    cmd.Parameters.AddWithValue("@GolosFora", dto.GolosFora);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        _logger.LogInformation($"Jogo {codigo} atualizado com sucesso");

                        return Ok(new
                        {
                            codigo = codigo,
                            mensagem = "Estado do jogo atualizado.",
                            estado = dto.Estado,
                            marcador = $"{dto.GolosCasa}x{dto.GolosFora}"
                        });
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao atualizar jogo {codigo}: {ex.Message}");
                        return BadRequest(new { erro = ex.Message });
                    }
                }
            }
        }

        /// <summary>
        /// Remover um jogo da plataforma
        /// </summary>
        [HttpDelete("{codigo}")]
        public IActionResult RemoverJogo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return BadRequest(new { erro = "O código do jogo é obrigatório." });

            _logger.LogInformation($"Removendo jogo {codigo}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Apostas_RemoverJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@Codigo", codigo);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        _logger.LogInformation($"Jogo {codigo} removido com sucesso");

                        return Ok(new
                        {
                            codigo = codigo,
                            mensagem = "Jogo removido com sucesso."
                        });
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao remover jogo {codigo}: {ex.Message}");
                        return BadRequest(new { erro = ex.Message });
                    }
                }
            }
        }

        /// <summary>
        /// Obter detalhes de um jogo específico
        /// </summary>
        [HttpGet("{codigo}")]
        public IActionResult ObterDetalhesJogo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return BadRequest(new { erro = "O código do jogo é obrigatório." });

            _logger.LogInformation($"Obtendo detalhes do jogo {codigo}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Apostas_ObterJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@Codigo", codigo);

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return Ok(new
                                {
                                    codigo = reader["Codigo"].ToString(),
                                    dataHoraInicio = Convert.ToDateTime(reader["DataHoraInicio"]),
                                    equipaCasa = reader["EquipaCasa"].ToString(),
                                    equipaFora = reader["EquipaFora"].ToString(),
                                    tipoCompeticao = reader["TipoCompeticao"].ToString(),
                                    estado = reader["Estado"].ToString(),
                                    golosCasa = Convert.ToInt32(reader["GolosCasa"]),
                                    golosFora = Convert.ToInt32(reader["GolosFora"])
                                });
                            }

                            return NotFound(new { erro = "Jogo não encontrado." });
                        }
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao obter jogo {codigo}: {ex.Message}");
                        return StatusCode(500, new { erro = "Erro ao recuperar jogo." });
                    }
                }
            }
        }
    }
}