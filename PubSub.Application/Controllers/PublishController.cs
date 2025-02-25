using Microsoft.AspNetCore.Mvc;
using PubSub.Application.Publish;
using PubSub.Core.Services.Publish;

namespace PubSub.Application.Controllers
{
    [ApiController]
    [Route("api/publish")]
    public class PublishController: ControllerBase
    {
        private readonly IPublisher _publisher;
        private readonly IPublisherService _publisherService;

        public PublishController(IPublisher publisher, IPublisherService publisherService)
        {
            _publisher = publisher;
            _publisherService = publisherService;
        }

        [HttpPost]
        public async Task<IActionResult> Publish(Publication publication) {
            if (!ModelState.IsValid) return BadRequest("Invalid request object provided.");

            //await _publisher.Publish(publication);
            await _publisherService.Publish(publication.Queue, publication.Message);

            return Ok("Hit publish endpoint");
        }
    }
}
