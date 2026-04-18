using BetStrike.Apostas.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Apostas.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstatisticasController : ControllerBase
    {
        private readonly string _connectionString;

        public EstatisticasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/estatisticas/jogos/{codigo}
        [HttpGet("jogos/{codigo}")]
        public IActionResult ObterEstatisticasJogo(string codigo)
        {
            EstatisticasJogoDto stats = null;

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Apostas_EstatisticasJogo", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Codigo", codigo);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            stats = new EstatisticasJogoDto
                            {
                                TotalApostado = Convert.ToDecimal(reader["TotalApostado"]),
                                ApostasTipo1 = Convert.ToInt32(reader["ApostasTipo1"]),
                                ApostasTipoX = Convert.ToInt32(reader["ApostasTipoX"]),
                                ApostasTipo2 = Convert.ToInt32(reader["ApostasTipo2"]),
                                ApostasPendentes = Convert.ToInt32(reader["ApostasPendentes"]),
                                ApostasGanhas = Convert.ToInt32(reader["ApostasGanhas"]),
                                ApostasPerdidas = Convert.ToInt32(reader["ApostasPerdidas"]),
                                ApostasAnuladas = Convert.ToInt32(reader["ApostasAnuladas"]),
                                MargemPlataforma = Convert.ToDecimal(reader["MargemPlataforma"])
                            };
                        }
                    }
                }
            }

            if (stats == null) return NotFound("Jogo não encontrado ou sem dados estatísticos.");
            return Ok(stats);
        }

        // GET: api/estatisticas/competicoes/{tipoCompeticao}
        [HttpGet("competicoes/{tipoCompeticao}")]
        public IActionResult ObterEstatisticasCompeticao(string tipoCompeticao)
        {
            EstatisticasCompeticaoDto stats = null;

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_Apostas_EstatisticasCompeticao", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TipoCompeticao", tipoCompeticao);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            stats = new EstatisticasCompeticaoDto
                            {
                                MediaGolosPorJogo = Convert.ToDecimal(reader["MediaGolosPorJogo"]),
                                VolumeTotalApostado = Convert.ToDecimal(reader["VolumeTotalApostado"]),
                                TaxaVitoria1 = Convert.ToDecimal(reader["TaxaVitoria1"]),
                                TaxaVitoriaX = Convert.ToDecimal(reader["TaxaVitoriaX"]),
                                TaxaVitoria2 = Convert.ToDecimal(reader["TaxaVitoria2"])
                            };
                        }
                    }
                }
            }

            if (stats == null) return NotFound("Sem dados estatísticos para a competição indicada.");
            return Ok(stats);
        }
    }
}