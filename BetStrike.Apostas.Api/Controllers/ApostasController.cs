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
        private string ConverterEstadoAposta(int estadoId)
        {
            return estadoId switch
            {
                1 => "Pendente",
                2 => "Ganha",
                3 => "Perdida",
                4 => "Cancelada",
                _ => "Desconhecido"
            };
        }

        public ApostasController(IConfiguration configuration, ILogger<ApostasController> logger)
        {
            _connectionString = configuration.GetConnectionString("Apostas") ?? ""; _logger = logger;
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

            _logger.LogInformation($"Registando aposta para utilizador {dto.UtilizadorId} no jogo {dto.CodigoJogo}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InserirAposta", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@UtilizadorId", dto.UtilizadorId);
                    cmd.Parameters.AddWithValue("@CodigoJogo", dto.CodigoJogo);
                    cmd.Parameters.AddWithValue("@TipoAposta", dto.TipoAposta);
                    cmd.Parameters.AddWithValue("@Montante", dto.Montante);
                    cmd.Parameters.AddWithValue("@OddMomento", dto.OddMomento);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        _logger.LogInformation($"Aposta registada com sucesso para utilizador {dto.UtilizadorId}");

                        return Ok(new
                        {
                            mensagem = "Aposta registada com sucesso. Saldo debitado.",
                            ganhosPotenciais = dto.Montante * dto.OddMomento
                        });
                    }
                    // Substitui o teu catch antigo por este que apanha TUDO:
                    catch (Exception ex)
                    {
                        _logger.LogError($"ERRO FATAL NA APOSTA: {ex.Message}");
                        // Isto vai mandar o erro real do SQL direto para o teu site!
                        return StatusCode(500, new { erro = $"A VERDADE DA APOSTA: {ex.Message}" });
                    }
                }
            }
        }

        [HttpGet("utilizador/{UtilizadorId}")]
        public IActionResult ListarApostasUtilizador(int UtilizadorId)
        {
            if (UtilizadorId <= 0)
                return BadRequest(new { erro = "O ID do utilizador deve ser válido." });

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // CORREÇÃO: nome correto da SP
                using (SqlCommand cmd = new SqlCommand("sp_ConsultarApostasPorUtilizador", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@UtilizadorId", UtilizadorId);

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            var apostas = new List<object>();
                            while (reader.Read())
                            {
                                apostas.Add(new
                                {
                                    idAposta = Convert.ToInt32(reader["Id"]),
                                    utilizadorId = Convert.ToInt32(reader["UtilizadorId"]),

                                    // 1. Agora o C# lê a coluna CodigoJogo que a SP devolve
                                    codigoJogo = reader["CodigoJogo"].ToString(),

                                    tipoAposta = reader["TipoAposta"].ToString(),
                                    montante = Convert.ToDecimal(reader["ValorApostado"]),
                                    oddMomento = Convert.ToDecimal(reader["OddMomento"]),

                                    // 2. O Frontend precisa da palavra "Pendente" ou "Ganha", e não do número 1 ou 2.
                                    estado = ConverterEstadoAposta(Convert.ToInt32(reader["EstadoAposta"])),

                                    dataRegisto = Convert.ToDateTime(reader["DataHoraAposta"])
                                });
                            }
                            return Ok(apostas);
                        }
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao listar apostas: {ex.Message}");
                        return StatusCode(500, new { erro = $"Erro ao recuperar apostas: {ex.Message}" });
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
                                    UtilizadorId = Convert.ToInt32(reader["UtilizadorId"]),
                                    CodigoJogo = reader["CodigoJogo"].ToString(),
                                    TipoAposta = reader["TipoAposta"].ToString(),
                                    Montante = Convert.ToDecimal(reader["Montante"]),
                                    OddMomento = Convert.ToDecimal(reader["OddMomento"]),
                                    GanhosPotenciais = Convert.ToDecimal(reader["Montante"]) * Convert.ToDecimal(reader["OddMomento"]),
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
        /// Cancelar uma aposta pendente
        /// </summary>
        [HttpPost("{idAposta}/cancelar")]
        public IActionResult CancelarAposta(int idAposta)
        {
            if (idAposta <= 0)
                return BadRequest(new { erro = "O ID da aposta deve ser válido." });

            _logger.LogInformation($"Cancelando aposta {idAposta}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_CancelarAposta", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.AddWithValue("@ApostaId", idAposta);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        _logger.LogInformation($"Aposta {idAposta} cancelada com sucesso");
                        return Ok(new { mensagem = "Aposta cancelada com sucesso." });
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao cancelar aposta {idAposta}: {ex.Message}");
                        if (ex.Number == 50000)
                            return BadRequest(new { erro = ex.Message });
                        return StatusCode(500, new { erro = "Erro ao cancelar aposta." });
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
                                    UtilizadorId = Convert.ToInt32(reader["UtilizadorId"]),
                                    CodigoJogo = reader["CodigoJogo"].ToString(),
                                    TipoAposta = reader["TipoAposta"].ToString(),
                                    Montante = Convert.ToDecimal(reader["Montante"]),
                                    OddMomento = Convert.ToDecimal(reader["OddMomento"]),
                                    GanhosPotenciais = Convert.ToDecimal(reader["Montante"]) * Convert.ToDecimal(reader["OddMomento"]),
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

