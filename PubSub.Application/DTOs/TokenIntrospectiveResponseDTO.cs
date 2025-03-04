﻿using System.Text.Json.Serialization;

namespace PubSub.Application.DTOs
{
    public class TokenIntrospectiveResponseDTO
    {
        [JsonPropertyName("active")]
        public bool Active { get; set; }
        [JsonPropertyName("expiration")]
        public DateTime Expiration { get; set; }
        [JsonPropertyName("claims")]
        public IEnumerable<ClaimResponseDTO> Claims { get; set; }
    }
}
