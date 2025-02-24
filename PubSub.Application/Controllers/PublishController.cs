using Microsoft.AspNetCore.Mvc;

namespace PubSub.Application.Controllers
{
    [ApiController]
    [Route("api/publish")]
    public class PublishController: ControllerBase
    {
        private readonly IPublisher _publisher;

        public PublishController(IPublisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost]
        public async Task<IActionResult> Publish(Publication publication) {
            if (!ModelState.IsValid) return BadRequest("Invalid request object provided.");

            await _publisher.Publish(publication);

            return Ok("Hit publish endpoint");
        }
    }
}
