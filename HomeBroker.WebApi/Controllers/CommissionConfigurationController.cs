using HomeBroker.Application;
using HomeBroker.Application.IServiceInterfaces.ICommissionService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeBroker.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminPolicy")]
    public class CommissionConfigurationController : ControllerBase
    {

        private readonly ICommissionService _commissionService;
        public CommissionConfigurationController(ICommissionService commissionService)
        {
            _commissionService = commissionService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] CommissionConfigurationDto config)
        {
            var result = await _commissionService.CreateOrUpdateConfigurationAsync(config);
            return Ok(new APIResponse(result));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAsync(long id, [FromBody] CommissionConfigurationDto config)
        {
            var result = await _commissionService.CreateOrUpdateConfigurationAsync(config);
            return Ok(new APIResponse(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var commission = await _commissionService.GetByIdAsync(id);
            return Ok(new APIResponse(commission));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var commissions = await _commissionService.GetCommissionConfigurationsAsync();
            return Ok(new APIResponse(commissions));
        }
    }
}
