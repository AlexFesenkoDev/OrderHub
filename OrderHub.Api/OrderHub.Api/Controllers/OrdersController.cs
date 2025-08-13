using Microsoft.AspNetCore.Mvc;
using OrderHub.Api.Models;
using OrderHub.Api.Services;

namespace OrderHub.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] CreateOrderDto dto)
        {
            var result = await _orderService.PlaceOrderAsync(dto);
            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_orderService.GetAllOrders());
        }
    }
}
