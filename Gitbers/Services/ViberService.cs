using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Gitbers.Models;
using Microsoft.Extensions.Options;

namespace Gitbers.Services
{
    public class ViberService
    {
        private readonly HttpClient _httpClient;
        private readonly ViberSettings _settings;

        public ViberService(
            HttpClient httpClient,
            IOptions<ViberSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<bool> SendMessageAsync(
            string viberUserId,
            string message)
        {
            if (string.IsNullOrWhiteSpace(_settings.AuthToken))
                return false;

            if (string.IsNullOrWhiteSpace(viberUserId))
                return false;

            var request = new
            {
                receiver = viberUserId,
                min_api_version = 7,
                sender = new
                {
                    name = "Gitbers"
                },
                type = "text",
                text = message
            };

            var json = JsonSerializer.Serialize(request);

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "https://chatapi.viber.com/pa/send_message");

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _settings.AuthToken);

            httpRequest.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            using var response =
                await _httpClient.SendAsync(httpRequest);

            return response.IsSuccessStatusCode;
        }
    }
}