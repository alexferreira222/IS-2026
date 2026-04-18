using BetStrike.Apostas.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Apostas.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilizadoresController : ControllerBase
    {
        private readonly string _connectionString;

        public UtilizadoresController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost]
        public IActionResult CriarUtilizador([FromBody] UtilizadorDto dto)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_CriarUtilizador", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Nome", dto.Nome);

                    try
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        return Ok("Utilizador criado com sucesso com saldo inicial de 50.00€.");
                    }
                    catch (SqlException ex)
                    {
                        return StatusCode(500, $"Erro ao criar utilizador: {ex.Message}");
                    }
                }
            }
        }
    }
}