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
            _connectionString = configuration.GetConnectionString("Apostas") ?? "";
            _logger = logger;
        }

        /// <summary>
        /// Inserir um novo jogo na plataforma
        /// </summary>
        [HttpPost]
        public IActionResult InserirJogo([FromBody] JogoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Codigo) || !Regex.IsMatch(dto.Codigo, @"^FUT-\d{4}-\d{4}$"))
                return BadRequest(new { erro = "O código do jogo deve seguir o formato exato FUT-AAAA-JJNN (ex: FUT-2026-0101)." });

            if (dto.DataHoraInicio < DateTime.UtcNow)
                return BadRequest(new { erro = "A data e hora do jogo não pode ser no passado." });

            _logger.LogInformation($"Inserindo novo jogo: {dto.Codigo}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InserirJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;

                    cmd.Parameters.AddWithValue("@CodigoJogo", dto.Codigo);
                    cmd.Parameters.AddWithValue("@DataJogo", dto.DataHoraInicio.Date);
                    cmd.Parameters.AddWithValue("@HoraInicio", dto.DataHoraInicio.TimeOfDay);
                    cmd.Parameters.AddWithValue("@EquipaCasa", dto.EquipaCasa ?? string.Empty);
                    cmd.Parameters.AddWithValue("@EquipaFora", dto.EquipaFora ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Competicao", dto.TipoCompeticao ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Estado", 1); // 1 = Agendado

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
        /// Registar o resultado final de um jogo
        /// CORREÇÃO: o parâmetro na SP chama-se @CodigoJogo, não @Codigo
        /// </summary>
        [HttpPost("resultado")]
        public IActionResult RegistarResultado([FromBody] ResultadoDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Codigo))
                return BadRequest(new { erro = "Dados do resultado inválidos." });

            _logger.LogInformation($"Registo de Resultado Final: Jogo {dto.Codigo} -> {dto.GolosCasa}x{dto.GolosFora}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InserirResultado", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // CORREÇÃO: era @Codigo, mas a SP espera @CodigoJogo
                    cmd.Parameters.AddWithValue("@CodigoJogo", dto.Codigo);
                    cmd.Parameters.AddWithValue("@GolosCasa", dto.GolosCasa);
                    cmd.Parameters.AddWithValue("@GolosFora", dto.GolosFora);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        return Ok(new { mensagem = "Resultado registado e apostas processadas!" });
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError($"Erro ao gravar resultado do jogo {dto.Codigo}: {ex.Message}");

                        // A SP usa RAISERROR, por isso o erro vem sempre com Number 50000
                        // e a mensagem já é legível para o utilizador
                        if (ex.Number == 50000)
                            return BadRequest(new { erro = ex.Message });

                        return StatusCode(500, new { erro = $"Erro interno: {ex.Message}" });
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

                        return Ok(new { codigo = codigo, mensagem = "Jogo removido com sucesso." });
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
        /// CORREÇÃO: sp_ConsultarJogos não aceita parâmetro — devolve todos os jogos,
        /// por isso filtramos em memória pelo código.
        /// CORREÇÃO: colunas corretas são DataJogo + HoraInicio + Competicao
        /// </summary>
        [HttpGet("{codigo}")]
        public IActionResult ObterDetalhesJogo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return BadRequest(new { erro = "O código do jogo é obrigatório." });

            _logger.LogInformation($"Obtendo detalhes do jogo {codigo}");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ConsultarJogos", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    // Nota: a SP não aceita parâmetros — filtramos após leitura

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // CORREÇÃO: coluna é CodigoJogo, não Codigo
                                if (reader["CodigoJogo"].ToString() != codigo)
                                    continue;

                                // CORREÇÃO: combinamos DataJogo + HoraInicio num só DateTime
                                var dataJogo = Convert.ToDateTime(reader["DataJogo"]);
                                var horaInicio = (TimeSpan)reader["HoraInicio"];
                                var dataHora = dataJogo.Add(horaInicio);

                                return Ok(new
                                {
                                    codigo = reader["CodigoJogo"].ToString(),
                                    dataHoraInicio = dataHora,
                                    equipaCasa = reader["EquipaCasa"].ToString(),
                                    equipaFora = reader["EquipaFora"].ToString(),
                                    // CORREÇÃO: coluna é Competicao, não TipoCompeticao
                                    tipoCompeticao = reader["Competicao"].ToString(),
                                    estado = Convert.ToInt32(reader["Estado"])
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

        /// <summary>
        /// Obter a lista de todos os jogos
        /// CORREÇÃO: usa sp_ConsultarJogos em vez de SQL direto,
        /// com os nomes de coluna corretos da BD
        /// </summary>
        [HttpGet]
        public IActionResult GetTodosOsJogos()
        {
            var listaDeJogos = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ConsultarJogos", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var dataJogo = Convert.ToDateTime(reader["DataJogo"]);
                                var horaInicio = (TimeSpan)reader["HoraInicio"];

                                listaDeJogos.Add(new
                                {
                                    codigo = reader["CodigoJogo"].ToString(),
                                    dataHoraInicio = dataJogo.Add(horaInicio),
                                    equipaCasa = reader["EquipaCasa"].ToString(),
                                    equipaFora = reader["EquipaFora"].ToString(),
                                    tipoCompeticao = reader["Competicao"].ToString(),
                                    estado = Convert.ToInt32(reader["Estado"])
                                });
                            }
                        }

                        return Ok(listaDeJogos);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Erro ao listar jogos: {ex.Message}");
                        return StatusCode(500, new { erro = $"Erro ao listar jogos: {ex.Message}" });
                    }
                }
            }
        }
    }
}