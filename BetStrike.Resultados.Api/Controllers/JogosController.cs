using BetStrike.Resultados.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace BetStrike.Resultados.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JogosController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public JogosController(IConfiguration configuration)
        {
            _configuration = configuration;
            // A string de ligação deve ser configurada no appsettings.json
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // POST: api/jogos
        [HttpPost]
        public IActionResult InserirJogo([FromBody] Jogo jogo)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Resultados_InserirJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Codigo", jogo.Codigo_Jogo);
                    cmd.Parameters.AddWithValue("@Data", jogo.Data);
                    cmd.Parameters.AddWithValue("@EquipaCasa", jogo.EquipaCasa);
                    cmd.Parameters.AddWithValue("@EquipaFora", jogo.EquipaFora);

                    try
                    {
                        con.Open();
                        // O ExecuteScalar devolve o ID gerado pela Stored Procedure
                        int idInterno = Convert.ToInt32(cmd.ExecuteScalar());
                        return StatusCode(201, new { Id = idInterno, Codigo = jogo.Codigo_Jogo });
                    }
                    catch (SqlException ex)
                    {
                        // Se o código já existir, a SP vai lançar um erro (Número 50000)
                        if (ex.Number == 50000) return Conflict(ex.Message);
                        return StatusCode(500, "Erro interno do servidor.");
                    }
                }
            }
        }

        // PUT: api/jogos/{codigo}
        [HttpPut("{codigo}")]
        public IActionResult AtualizarJogo(string codigo, [FromBody] AtualizarJogoDto dto)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Resultados_AtualizarJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@NovoEstado", dto.Estado);
                    cmd.Parameters.AddWithValue("@GolosCasa", dto.GolosCasa);
                    cmd.Parameters.AddWithValue("@GolosFora", dto.GolosFora);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        return Ok("Estado e/ou marcador atualizados com sucesso.");
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 50000) return BadRequest(ex.Message);
                        return StatusCode(500, "Erro interno.");
                    }
                }
            }
        }

        // GET: api/jogos
        [HttpGet]
        public IActionResult ListarJogos([FromQuery] DateTime? data, [FromQuery] int? estado)
        {
            var jogos = new List<Jogo>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Resultados_ListarJogos", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Data", data ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Estado", estado ?? (object)DBNull.Value);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            jogos.Add(new Jogo
                            {
                                Codigo_Jogo = reader["Codigo_Jogo"].ToString(),
                                Data = Convert.ToDateTime(reader["DataHora"]),
                                EquipaCasa = reader["EquipaCasa"].ToString(),
                                EquipaFora = reader["EquipaFora"].ToString(),
                                GolosCasa = Convert.ToInt32(reader["GolosCasa"]),
                                GolosFora = Convert.ToInt32(reader["GolosFora"]),
                                Estado = Convert.ToInt32(reader["Estado"])
                            });
                        }
                    }
                }
            }
            return Ok(jogos);
        }

        // GET: api/jogos/{codigo}
        [HttpGet("{codigo}")]
        public IActionResult ObterJogo(string codigo)
        {
            Jogo jogo = null;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Resultados_ObterJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Codigo", codigo);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            jogo = new Jogo
                            {
                                Codigo_Jogo = reader["Codigo_Jogo"].ToString(),
                                Data = Convert.ToDateTime(reader["DataHora"]),
                                EquipaCasa = reader["EquipaCasa"].ToString(),
                                EquipaFora = reader["EquipaFora"].ToString(),
                                GolosCasa = Convert.ToInt32(reader["GolosCasa"]),
                                GolosFora = Convert.ToInt32(reader["GolosFora"]),
                                Estado = Convert.ToInt32(reader["Estado"])
                            };
                        }
                    }
                }
            }
            return jogo != null ? Ok(jogo) : NotFound("Jogo não encontrado.");
        }

        // DELETE: api/jogos/{codigo}
        [HttpDelete("{codigo}")]
        public IActionResult RemoverJogo(string codigo)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Resultados_RemoverJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Codigo", codigo);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        return Ok("Jogo removido com sucesso.");
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 50000) return BadRequest(ex.Message);
                        return StatusCode(500, "Erro ao remover jogo.");
                    }
                }
            }
        }
    }
}