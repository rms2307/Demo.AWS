using Common;
using Common.Model;
using Microsoft.AspNetCore.Mvc;

namespace Demo.AWS.Cadastrador.Controllers
{
    [Route("api/[controller]")]
    public class PedidoController : ControllerBase
    {
        [HttpPost]
        public async Task PostAsync([FromBody] Pedido pedido)
        {
            pedido.Id = Guid.NewGuid().ToString();
            pedido.DataDeCriacao = DateTime.UtcNow;

            await pedido.SalvarAsync();

            Console.WriteLine($"Pedido salvo com sucesso: id {pedido.Id}");
        }
    }
}
