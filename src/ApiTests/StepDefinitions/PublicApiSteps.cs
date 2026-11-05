using System.Net;
using System.Net.Http;
using System.Globalization;
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
        private readonly ScenarioContext _scenarioContext;
        private ApiClient _apiClient;
        private HttpResponseMessage _response;

        public PublicApiSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [Given("DummyJSON is available")]
        public void GivenDummyJsonIsAvailable()
        {
            _apiClient = new ApiClient(TestSettings.BaseUrl);
        }

        [When("I send a {word} request to {string}")]
        public async Task WhenISendARequestTo(string method, string endpoint)
        {
            SetResponse(await _apiClient.SendAsync(new HttpMethod(method.ToUpperInvariant()), endpoint));
        }

        [When("I send a {word} request to {string} with JSON body:")]
        public async Task WhenISendARequestWithJsonBody(string method, string endpoint, string jsonBody)
        {
            SetResponse(await _apiClient.SendAsync(
                new HttpMethod(method.ToUpperInvariant()),
                endpoint,
                jsonBody));
        }

        [When("I authenticate with username {string} and password {string}")]
        public async Task WhenIAuthenticateWithCredentials(string username, string password)
        {
            var requestBody = JsonSerializer.Serialize(new { username, password });
            SetResponse(await _apiClient.PostAsync("/auth/login", requestBody));

            using var loginResponse = await JsonDocument.ParseAsync(await _response.Content.ReadAsStreamAsync());
            var accessToken = loginResponse.RootElement.GetProperty("accessToken").GetString();
            _scenarioContext["accessToken"] = accessToken;

            _apiClient.Dispose();
            _apiClient = new ApiClient(TestSettings.BaseUrl, accessToken);
        }

        [When("I request the current authenticated user")]
        public async Task WhenIRequestTheCurrentAuthenticatedUser()
        {
            SetResponse(await _apiClient.GetAsync("/auth/me"));
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

        [Then("the access token should be stored for this scenario")]
        public void ThenTheAccessTokenShouldBeStoredForThisScenario()
        {
            Assert.That(_scenarioContext.ContainsKey("accessToken"), Is.True);
            Assert.That(_scenarioContext["accessToken"] as string, Is.Not.Null.And.Not.Empty);
        }

        [Then("response JSON path {string} should contain {int} item")]
        [Then("response JSON path {string} should contain {int} items")]
        public async Task ThenResponseJsonPathShouldContainItems(string jsonPath, int expectedCount)
        {
            var value = await GetJsonValueAsync(jsonPath);
            Assert.That(value.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(value.GetArrayLength(), Is.EqualTo(expectedCount));
        }

        [Then("every item in response JSON array {string} should have {string} equal to {string}")]
        public async Task ThenEveryArrayItemShouldHavePropertyValue(
            string arrayPath,
            string propertyPath,
            string expectedValue)
        {
            var array = await GetJsonValueAsync(arrayPath);
            Assert.That(array.ValueKind, Is.EqualTo(JsonValueKind.Array));

            foreach (var item in array.EnumerateArray())
            {
                var actualValue = GetValueAtPath(item, propertyPath);
                Assert.That(GetScalarValue(actualValue), Is.EqualTo(expectedValue));
            }
        }

        [Then("the cart discount percentage calculated from each item's prices should equal {string}")]
        public async Task ThenCartDiscountPercentageShouldEqual(string expectedPercentage)
        {
            var array = await GetJsonValueAsync("products");
            Assert.That(array.ValueKind, Is.EqualTo(JsonValueKind.Array));

            var originalPriceTotal = 0m;
            var discountedPriceTotal = 0m;
            foreach (var item in array.EnumerateArray())
            {
                originalPriceTotal += GetValueAtPath(item, "total").GetDecimal();
                discountedPriceTotal += GetValueAtPath(item, "discountedPrice").GetDecimal();
            }

            Assert.That(originalPriceTotal, Is.GreaterThan(0m));
            Assert.That(discountedPriceTotal, Is.LessThanOrEqualTo(originalPriceTotal));

            var actualPercentage =
                (originalPriceTotal - discountedPriceTotal) / originalPriceTotal * 100m;
            var expected = decimal.Parse(expectedPercentage, CultureInfo.InvariantCulture);
            Assert.That(actualPercentage, Is.EqualTo(expected).Within(0.01m));
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
            return GetValueAtPath(document.RootElement, jsonPath).Clone();
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

        private static JsonElement GetValueAtPath(JsonElement root, string jsonPath)
        {
            var value = root;
            foreach (var segment in GetPathSegments(jsonPath))
            {
                value = value.ValueKind == JsonValueKind.Array
                    ? value[int.Parse(segment, CultureInfo.InvariantCulture)]
                    : value.GetProperty(segment);
            }

            return value;
        }

        private void SetResponse(HttpResponseMessage response)
        {
            _response?.Dispose();
            _response = response;
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