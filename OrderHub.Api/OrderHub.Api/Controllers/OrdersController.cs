using Microsoft.AspNetCore.Mvc;
using OrderHub.Api.Models;
using OrderHub.Api.Services;

namespace OrderHub.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] CreateOrderDto dto)
        {
            var svc = new OrderService();
            var result = await svc.PlaceOrderAsync(dto);
            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var svc = new OrderService();
            return Ok(svc.GetAllOrders());
        }
    }
}
