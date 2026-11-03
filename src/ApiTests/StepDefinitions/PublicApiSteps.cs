using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ApiTests.Clients;
using NUnit.Framework;
using Reqnroll;

namespace ApiTests.StepDefinitions
{
    [Binding]
    public class PublicApiSteps
    {
        private ApiClient _apiClient;
        private HttpResponseMessage _response;

        [Given("DummyJSON is available")]
        public void GivenDummyJsonIsAvailable()
        {
            _apiClient = new ApiClient(TestSettings.BaseUrl);
        }

        [When("I send a {word} request to {string}")]
        public async Task WhenISendARequestTo(string method, string endpoint)
        {
            _response = await _apiClient.SendAsync(new HttpMethod(method.ToUpperInvariant()), endpoint);
        }

        [When("I send a {word} request to {string} with JSON body:")]
        public async Task WhenISendARequestWithJsonBody(string method, string endpoint, string jsonBody)
        {
            _response = await _apiClient.SendAsync(
                new HttpMethod(method.ToUpperInvariant()),
                endpoint,
                jsonBody);
        }

        [Then("the response status code should be {int}")]
        public void ThenTheResponseStatusCodeShouldBe(int expectedStatusCode)
        {
            Assert.That(_response.StatusCode, Is.EqualTo((HttpStatusCode)expectedStatusCode));
        }

        [Then("response JSON path {string} should equal {string}")]
        public async Task ThenResponseJsonPathShouldEqual(string jsonPath, string expectedValue)
        {
            var actualValue = await GetJsonValueAsync(jsonPath);
            Assert.That(GetScalarValue(actualValue), Is.EqualTo(expectedValue));
        }

        [Then("response JSON path {string} should contain {string}")]
        public async Task ThenResponseJsonPathShouldContain(string jsonPath, string expectedText)
        {
            var actualValue = await GetJsonValueAsync(jsonPath);
            Assert.That(actualValue.ToString().ToLowerInvariant(), Does.Contain(expectedText.ToLowerInvariant()));
        }

        [Then("response JSON path {string} should be greater than {int}")]
        public async Task ThenResponseJsonPathShouldBeGreaterThan(string jsonPath, int expectedMinimum)
        {
            var actualValue = await GetJsonValueAsync(jsonPath);
            Assert.That(actualValue.GetInt32(), Is.GreaterThan(expectedMinimum));
        }

        [Then("response JSON path {string} should not exist")]
        public async Task ThenResponseJsonPathShouldNotExist(string jsonPath)
        {
            Assert.That(await JsonPathExistsAsync(jsonPath), Is.False);
        }

        [Then("response JSON path {string} should exist")]
        public async Task ThenResponseJsonPathShouldExist(string jsonPath)
        {
            Assert.That(await JsonPathExistsAsync(jsonPath), Is.True);
        }

        [AfterScenario]
        public void Cleanup()
        {
            _response?.Dispose();
            _apiClient?.Dispose();
        }

        private async Task<JsonElement> GetJsonValueAsync(string jsonPath)
        {
            using var document = await GetResponseJsonAsync();
            var value = document.RootElement;

            foreach (var segment in GetPathSegments(jsonPath))
            {
                value = value.ValueKind == JsonValueKind.Array
                    ? value[int.Parse(segment)]
                    : value.GetProperty(segment);
            }

            return value.Clone();
        }

        private async Task<bool> JsonPathExistsAsync(string jsonPath)
        {
            using var document = await GetResponseJsonAsync();
            var value = document.RootElement;

            foreach (var segment in GetPathSegments(jsonPath))
            {
                if (value.ValueKind == JsonValueKind.Array)
                {
                    if (!int.TryParse(segment, out var index) || index < 0 || index >= value.GetArrayLength())
                    {
                        return false;
                    }

                    value = value[index];
                }
                else if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty(segment, out var property))
                {
                    value = property;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<JsonDocument> GetResponseJsonAsync()
        {
            var responseBody = await _response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(responseBody);
        }

        private static string[] GetPathSegments(string jsonPath)
        {
            return jsonPath.Replace("[", ".").Replace("]", string.Empty)
                .Split('.', System.StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetScalarValue(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.String => value.GetString(),
                _ => value.GetRawText()
            };
        }
    }
}