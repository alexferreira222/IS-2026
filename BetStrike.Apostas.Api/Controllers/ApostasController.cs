using BetStrike.Apostas.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Apostas.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApostasController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<ApostasController> _logger;

        public ApostasController(IConfiguration configuration, ILogger<ApostasController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        /// <summary>
        /// Registar uma nova aposta
        /// </summary>
        [HttpPost]
        public IActionResult RegistarAposta([FromBody] RegistarApostaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation($"Registando aposta para utilizador {dto.IdUtilizador} no jogo {dto.CodigoJogo}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Apostas_Registar", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@IdUtilizador", dto.IdUtilizador);
                    cmd.Parameters.AddWithValue("@CodigoJogo", dto.CodigoJogo);
                    cmd.Parameters.AddWithValue("@TipoAposta", dto.TipoAposta);
                    cmd.Parameters.AddWithValue("@Montante", dto.Montante);
                    cmd.Parameters.AddWithValue("@Odd", dto.Odd);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        _logger.LogInformation($"Aposta registada com sucesso para utilizador {dto.IdUtilizador}");

                        return Ok(new
                        {
                            mensagem = "Aposta registada com sucesso. Saldo debitado.",
                            ganhosPotenciais = dto.Montante * dto.Odd
                        });
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao registar aposta: {ex.Message}");

                        if (ex.Number == 50000) 
                            return BadRequest(new { erro = ex.Message });

                        return StatusCode(500, new { erro = "Erro ao processar aposta. Tente novamente." });
                    }
                }
            }
        }

        /// <summary>
        /// Listar apostas do utilizador com paginação
        /// </summary>
        [HttpGet("utilizador/{idUtilizador}")]
        public IActionResult ListarApostasUtilizador(int idUtilizador, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 20)
        {
            if (idUtilizador <= 0)
                return BadRequest("O ID do utilizador deve ser válido.");

            if (tamanho <= 0 || tamanho > 100)
                tamanho = 20;

            _logger.LogInformation($"Listando apostas do utilizador {idUtilizador}, página {pagina}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Apostas_ListarPorUtilizador", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@IdUtilizador", idUtilizador);
                    cmd.Parameters.AddWithValue("@Pagina", pagina);
                    cmd.Parameters.AddWithValue("@Tamanho", tamanho);

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            var apostas = new List<ApostaResponseDto>();
                            int total = 0;

                            while (reader.Read())
                            {
                                apostas.Add(new ApostaResponseDto
                                {
                                    IdAposta = Convert.ToInt32(reader["IdAposta"]),
                                    IdUtilizador = Convert.ToInt32(reader["IdUtilizador"]),
                                    CodigoJogo = reader["CodigoJogo"].ToString(),
                                    TipoAposta = reader["TipoAposta"].ToString(),
                                    Montante = Convert.ToDecimal(reader["Montante"]),
                                    Odd = Convert.ToDecimal(reader["Odd"]),
                                    GanhosPotenciais = Convert.ToDecimal(reader["Montante"]) * Convert.ToDecimal(reader["Odd"]),
                                    Estado = reader["Estado"].ToString(),
                                    DataRegisto = Convert.ToDateTime(reader["DataRegisto"])
                                });
                            }

                            if (reader.NextResult())
                            {
                                if (reader.Read())
                                    total = Convert.ToInt32(reader["Total"]);
                            }

                            return Ok(new ListarApostasResponseDto
                            {
                                Apostas = apostas,
                                Total = total,
                                Pagina = pagina,
                                Tamanho = tamanho
                            });
                        }
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao listar apostas: {ex.Message}");
                        return StatusCode(500, new { erro = "Erro ao recuperar apostas." });
                    }
                }
            }
        }

        /// <summary>
        /// Obter detalhes de uma aposta específica
        /// </summary>
        [HttpGet("{idAposta}")]
        public IActionResult ObterApostaDetalhe(int idAposta)
        {
            if (idAposta <= 0)
                return BadRequest("O ID da aposta deve ser válido.");

            _logger.LogInformation($"Obtendo detalhes da aposta {idAposta}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Apostas_ObterDetalhe", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@IdAposta", idAposta);

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return Ok(new ApostaResponseDto
                                {
                                    IdAposta = Convert.ToInt32(reader["IdAposta"]),
                                    IdUtilizador = Convert.ToInt32(reader["IdUtilizador"]),
                                    CodigoJogo = reader["CodigoJogo"].ToString(),
                                    TipoAposta = reader["TipoAposta"].ToString(),
                                    Montante = Convert.ToDecimal(reader["Montante"]),
                                    Odd = Convert.ToDecimal(reader["Odd"]),
                                    GanhosPotenciais = Convert.ToDecimal(reader["Montante"]) * Convert.ToDecimal(reader["Odd"]),
                                    Estado = reader["Estado"].ToString(),
                                    DataRegisto = Convert.ToDateTime(reader["DataRegisto"])
                                });
                            }

                            return NotFound(new { erro = "Aposta não encontrada." });
                        }
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao obter aposta: {ex.Message}");
                        return StatusCode(500, new { erro = "Erro ao recuperar aposta." });
                    }
                }
            }
        }

        /// <summary>
        /// Listar apostas de um jogo específico
        /// </summary>
        [HttpGet("jogo/{codigoJogo}")]
        public IActionResult ListarApostasJogo(string codigoJogo, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 20)
        {
            if (string.IsNullOrWhiteSpace(codigoJogo))
                return BadRequest("O código do jogo é obrigatório.");

            if (tamanho <= 0 || tamanho > 100)
                tamanho = 20;

            _logger.LogInformation($"Listando apostas do jogo {codigoJogo}, página {pagina}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Apostas_ListarPorJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@CodigoJogo", codigoJogo);
                    cmd.Parameters.AddWithValue("@Pagina", pagina);
                    cmd.Parameters.AddWithValue("@Tamanho", tamanho);

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            var apostas = new List<ApostaResponseDto>();
                            int total = 0;

                            while (reader.Read())
                            {
                                apostas.Add(new ApostaResponseDto
                                {
                                    IdAposta = Convert.ToInt32(reader["IdAposta"]),
                                    IdUtilizador = Convert.ToInt32(reader["IdUtilizador"]),
                                    CodigoJogo = reader["CodigoJogo"].ToString(),
                                    TipoAposta = reader["TipoAposta"].ToString(),
                                    Montante = Convert.ToDecimal(reader["Montante"]),
                                    Odd = Convert.ToDecimal(reader["Odd"]),
                                    GanhosPotenciais = Convert.ToDecimal(reader["Montante"]) * Convert.ToDecimal(reader["Odd"]),
                                    Estado = reader["Estado"].ToString(),
                                    DataRegisto = Convert.ToDateTime(reader["DataRegisto"])
                                });
                            }

                            if (reader.NextResult())
                            {
                                if (reader.Read())
                                    total = Convert.ToInt32(reader["Total"]);
                            }

                            return Ok(new ListarApostasResponseDto
                            {
                                Apostas = apostas,
                                Total = total,
                                Pagina = pagina,
                                Tamanho = tamanho
                            });
                        }
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao listar apostas do jogo: {ex.Message}");
                        return StatusCode(500, new { erro = "Erro ao recuperar apostas do jogo." });
                    }
                }
            }
        }
    }
}