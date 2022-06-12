using Microsoft.AspNetCore.Mvc;
namespace RepositoryDemo.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class InMemoryCustomersController : ControllerBase
    {
        MemoryRepository<Customer> customersManager;

        public InMemoryCustomersController(MemoryRepository<Customer> _customersManager)
        {
            customersManager = _customersManager;
        }

        [HttpGet]
        public async Task<ActionResult<APIListOfEntityResponse<Customer>>> Get()
        {
            try
            {
                var result = await customersManager.GetAllAsync();
                return Ok(new APIListOfEntityResponse<Customer>()
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPost("getwithfilter")]
        public async Task<ActionResult<APIListOfEntityResponse<Customer>>>
            GetWithFilter([FromBody] QueryFilter<Customer> Filter)
        {
            try
            {
                var result = await customersManager.GetAsync(Filter);
                return Ok(new APIListOfEntityResponse<Customer>()
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                // log exception here
                var msg = ex.Message;
                return StatusCode(500);
            }
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<APIEntityResponse<Customer>>> GetById(int Id)
        {
            try
            {
                var result = await customersManager.GetByIdAsync(Id);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Customer Not Found" },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<ActionResult<APIEntityResponse<Customer>>>
         Insert([FromBody] Customer Customer)
        {
            try
            {
                var result = await customersManager.InsertAsync(Customer);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>()
               { "Could not find customer after adding it." },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }


        [HttpPut]
        public async Task<ActionResult<APIEntityResponse<Customer>>>
            Update([FromBody] Customer Customer)
        {
            try
            {
                var result = await customersManager.UpdateAsync(Customer);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<Customer>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>()
               { "Could not find customer after updating it." },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpDelete("{Id}")]
        public async Task<ActionResult<bool>> Delete(int Id)
        {
            try
            {
                return await customersManager.DeleteByIdAsync(Id);
            }
            catch (Exception ex)
            {
                // log exception here
                var msg = ex.Message;
                return StatusCode(500);
            }
        }


        [HttpGet("deleteall")]
        public async Task<ActionResult> DeleteAll()
        {
            try
            {
                await customersManager.DeleteAllAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }
    }
}