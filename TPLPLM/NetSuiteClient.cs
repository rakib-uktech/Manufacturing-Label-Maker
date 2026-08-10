using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NetSuite
{
    public class NetSuiteClient
    {
        private readonly string _baseUrl;
        private readonly OAuth1HeaderGenerator _oauthHeaderGenerator;

        public NetSuiteClient(string consumerKey, string consumerSecret, string accessToken, string tokenSecret, string realm, string baseUrl)
        {
            if (string.IsNullOrEmpty(consumerKey) || string.IsNullOrEmpty(consumerSecret) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(tokenSecret))
            {
                throw new ArgumentException("OAuth credentials cannot be null or empty.");
            }

            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _oauthHeaderGenerator = new OAuth1HeaderGenerator(consumerKey, consumerSecret, accessToken, tokenSecret, realm);
        }

        public async Task<AssemblyItem> GetAssemblyItemAsync(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException(nameof(itemId), "Item ID cannot be null or empty.");
            }

            var url = $"{_baseUrl}/assemblyitem/{itemId}";
            using var client = new HttpClient();

            try
            {
                var authHeader = _oauthHeaderGenerator.Generate("GET", url);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = authHeader;
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error: {response.StatusCode}, Details: {errorContent}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AssemblyItem>(jsonContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve assembly item data: {ex.Message}", ex);
            }
        }

        public async Task<List<AssemblyItem>> GetAllAssemblyItemsAsync(int pageSize = 100)
        {
            var url = $"{_baseUrl}/assemblyitem?limit={pageSize}";
            using var client = new HttpClient();
            var allItems = new List<AssemblyItem>();
            int offset = 0;

            try
            {
                while (true)
                {
                    var paginatedUrl = $"{url}&offset={offset}";
                    var authHeader = _oauthHeaderGenerator.Generate("GET", paginatedUrl);

                    var request = new HttpRequestMessage(HttpMethod.Get, paginatedUrl);
                    request.Headers.Authorization = authHeader;
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new HttpRequestException($"Error: {response.StatusCode}, Details: {errorContent}");
                    }

                    var jsonResult = await response.Content.ReadAsStringAsync();
                    var assemblyItemResponse = JsonConvert.DeserializeObject<AssemblyItemResponse>(jsonResult);

                    if (assemblyItemResponse?.Data == null || assemblyItemResponse.Data.Count == 0)
                    {
                        break;
                    }

                    allItems.AddRange(assemblyItemResponse.Data);
                    offset += pageSize;
                }

                return allItems;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve assembly items: {ex.Message}", ex);
            }
        }

        public async Task<WorkOrder> GetWorkOrderAsync(string workOrderId)
        {
            if (string.IsNullOrEmpty(workOrderId))
            {
                throw new ArgumentNullException(nameof(workOrderId), "Work Order ID cannot be null or empty.");
            }

            var url = $"{_baseUrl}/workorder/{workOrderId}";
            using var client = new HttpClient();

            try
            {
                var authHeader = _oauthHeaderGenerator.Generate("GET", url);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = authHeader;
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error: {response.StatusCode}, Details: {errorContent}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<WorkOrder>(jsonContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve work order data: {ex.Message}", ex);
            }
        }

        public async Task<string> CreateWorkOrderCompletionAsync(WorkOrderCompletionRequest workOrderCompletion)
        {
            var url = $"{_baseUrl}/workOrderCompletion";
            using var client = new HttpClient();

            try
            {
                var authHeader = _oauthHeaderGenerator.Generate("POST", url);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(workOrderCompletion))
                };

                request.Headers.Authorization = authHeader;
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to create Work Order Completion. Status: {response.StatusCode}, Details: {content}");
                }

                return content;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error posting Work Order Completion: {ex.Message}", ex);
            }
        }

        public async Task<bool> CompleteWorkOrder(string workOrderId, int quantity, string itemId, string memo)
        {
            var url = $"{_baseUrl}/workordercompletion";
            var payload = new
            {
                workOrder = new { id = workOrderId },
                quantity = quantity,
                item = new { id = itemId },
                memo = memo
            };

            using var client = new HttpClient();
            var authHeader = _oauthHeaderGenerator.Generate("POST", url);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = authHeader;

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"WOC failed: {response.StatusCode} - {error}");
            }

            return true;
        }
    }

    public class AssemblyItem
    {
        public string Id { get; set; }
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }

       
        public string Custitemproduct_Spec_Qtyperinnerlayer { get; set; }
        
        public string Custitemproduct_Spec_Innlaypercase { get; set; }

        public string Custitemproduct_Spec_Casewtgrosskg { get; set; }
        
        public string Custitemproduct_Spec_Casewtnet { get; set; }
        [JsonConverter(typeof(RefNameOnlyConverter))] 
        public string Custitemproduct_Spec_Sku { get; set; }
        [JsonConverter(typeof(RefNameOnlyConverter))] 
        public string Custitemcustom_Product_Sepc_Case_Gtin { get; set; }
        [JsonConverter(typeof(RefNameOnlyConverter))] 
        public string Custitemproduct_Spec_Gtin { get; set; }
        [JsonConverter(typeof(RefNameOnlyConverter))] 
        public string Custitem13 { get; set; }
        [JsonConverter(typeof(RefNameOnlyConverter))] 
        public string Custitem17 { get; set; }
        [JsonConverter(typeof(RefNameOnlyConverter))] 
        public string Custitem12 { get; set; }
        
        public string Custitemproduct_Spec_Palletwtnetkg { get; set; }
      
        public string Custitemproduct_Spec_Palletwtgrosskg { get; set; }
       
        public string Custitemproduct_Spec_Caseperpallet { get; set; }
        public string Custitemitem_Shipping_Address { get; set; }
        

        public string Custitemproduct_Spec_Qtyperpallet { get; set; }
       
        public string Custitemproduct_Spec_Qtyperouter { get; set; }
        [JsonConverter(typeof(RefNameOnlyConverter))] 
        public string Custitemproduct_Spec_Productcode { get; set; }
        [JsonConverter(typeof(RefNameOnlyConverter))] 
        public string Custitemproduct_Spec_Description { get; set; }
        [JsonConverter(typeof(RefNameOnlyConverter))] 
        public string Custitemproduct_Spec_Label_Temp { get; set; }
        [JsonConverter(typeof(RefNameOnlyConverter))]
        public string Custitem20 { get; set; }
        
        [JsonConverter(typeof(RefNameOnlyConverter))]
        public string Custitemproduct_Spec_Pallet_Label_Temp { get; set; }

        public string RefName { get; set; }
    }

    public class AssemblyItemResponse
    {
        [JsonProperty("data")]
        public List<AssemblyItem> Data { get; set; }
        [JsonProperty("links")]
        public object Links { get; set; }
    }

    public class WorkOrder
    {
        public string Id { get; set; }
        public string TranId { get; set; }
        public string TranDate { get; set; }
        public AssemblyItem AssemblyItem { get; set; }
    }

    public class WorkOrderCompletionRequest
    {
        [JsonProperty("createdFrom")] public Reference CreatedFrom { get; set; }
        [JsonProperty("item")] public Reference Item { get; set; }
        [JsonProperty("quantity")] public double Quantity { get; set; }
        [JsonProperty("location")] public Reference Location { get; set; }
        [JsonProperty("subsidiary")] public Reference Subsidiary { get; set; }
        [JsonProperty("department")] public Reference Department { get; set; }
        [JsonProperty("class")] public Reference Class { get; set; }
        [JsonProperty("tranDate")] public string TranDate { get; set; }
        [JsonProperty("customForm")] public Reference CustomForm { get; set; }
    }

    public class Reference
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class RefNameOnlyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(string);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
                return reader.Value?.ToString();

            if (reader.TokenType == JsonToken.StartObject)
            {
                var obj = JObject.Load(reader);
                return obj["refName"]?.ToString();
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }
}
