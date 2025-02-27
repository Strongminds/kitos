using Microsoft.AspNetCore.Mvc;
using PubSub.Application.DTOs;
using PubSub.Core.Models;
using PubSub.Core.Services.Publish;

namespace PubSub.Application.Controllers
{
    [ApiController]
    [Route("api/publish")]
    public class PublishController: ControllerBase
    {
        private readonly IPublisherService _publisherService;

        public PublishController(IPublisherService publisherService)
        {
            _publisherService = publisherService;
        }

        [HttpPost]
        public async Task<IActionResult> Publish(PublishRequestDto request) {
            if (!ModelState.IsValid) return BadRequest("Invalid request object provided.");

            var publication = new Publication(request.Topic, request.Message);
            await _publisherService.Publish(publication);

            return Ok();
        }
    }
}
