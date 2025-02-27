using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PubSub.Core.Models;
using PubSub.Core.Services.Publish;

namespace PubSub.Application.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/publish")]
    public class PublishController: ControllerBase
    {
        private readonly IPublisherService _publisherService;

        public PublishController(IPublisherService publisherService)
        {
            _publisherService = publisherService;
        }

        [HttpPost]
        public async Task<IActionResult> Publish(Publication publication) {
            if (!ModelState.IsValid) return BadRequest("Invalid request object provided.");

            await _publisherService.Publish(publication.Queue, publication.Message);

            return Ok("Hit publish endpoint");
        }
    }
}
