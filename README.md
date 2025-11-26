# Category Attribute Generator

Small ASP.NET Core application that takes a catalog category hierarchy as input, calls the OpenAI Chat Completions API for each subcategory, and returns the three most relevant product attributes per subcategory.

The goal of the exercise is to:

- accept a nested JSON structure with categories and subcategories,
- for every subcategory ask the LLM for three useful attributes,
- return a normalized JSON result that can be easily consumed by other systems,
- expose a simple web UI to demo the flow end-to-end.

---

## Tech Stack

- **Runtime**: .NET 8
- **Web framework**: ASP.NET Core (minimal hosting model, controllers)
- **HTTP client**: `HttpClient` (no external SDKs)
- **JSON**: `System.Text.Json`
- **UI**: Static HTML + vanilla JavaScript under `wwwroot`

---

## Project Structure

```text
src/
  CategoryAttributeGenerator/
    CategoryAttributeGenerator.csproj
    Program.cs
    appsettings.json
    Controllers/
      CategoryAttributesController.cs
    Models/
      CategoryModels.cs
    Services/
      ICategoryAttributeService.cs
      CategoryAttributeService.cs
      CategoryPromptOptions.cs
      OpenAI/
        OpenAiClient.cs
        Data/
          OpenAiChatCompletionRequest.cs
          OpenAiChatCompletionResponse.cs
          OpenAiMessage.cs
          OpenAiResponseFormat.cs
    wwwroot/
      index.html
```

**Responsibility split:**

- `Controllers/`
  - HTTP API surface, validation, error translation.
- `Models/`
  - API DTOs for input and output (category groups, subcategories, attributes, error responses).
- `Services/`
  - Application service (`CategoryAttributeService`) containing the orchestration logic.
  - `CategoryPromptOptions` provides configurable prompt text.
  - `OpenAI` subfolder contains the HTTP client and low-level request/response models for the OpenAI API.
- `wwwroot/`
  - Single-page HTML/JS UI used to manually test the endpoint.

---

## Business Flow

1. **Input** – client sends a JSON array of category groups:

   ```json
   [
     {
       "categoryName": "TVs",
       "subCategories": [
         { "categoryId": 80,  "categoryName": "TVs" },
         { "categoryId": 948, "categoryName": "All-Weather TVs" },
         { "categoryId": 37,  "categoryName": "TV Accessories" }
       ]
     }
   ]
   ```

2. **Backend**:
   - Validates that the root payload is a non-empty array.
   - Flattens subcategories across groups.
   - For each `SubCategoryDto` builds a prompt and calls OpenAI Chat Completions.
   - Expects a JSON object in the shape:

     ```json
     {
       "attributes": [
         "Attribute 1",
         "Attribute 2",
         "Attribute 3"
       ]
     }
     ```

   - Parses the response and enforces “exactly three attributes”.
   - Aggregates results into a flat list of `{ categoryId, attributes }`.

3. **Output** – response is a JSON array:

   ```json
   [
     {
       "categoryId": 80,
       "attributes": [
         "Screen Size",
         "Resolution",
         "Panel Type"
       ]
     },
     {
       "categoryId": 948,
       "attributes": [
         "Weatherproof Rating",
         "Brightness",
         "Screen Size"
       ]
     },
     {
       "categoryId": 37,
       "attributes": [
         "Compatible Devices",
         "Cable Type",
         "Length"
       ]
     }
   ]
   ```

4. **UI** (index.html):
   - Provides a textarea with a sample payload.
   - Sends `POST /api/category-attributes` via `fetch`.
   - Displays the result JSON and shows request latency (in µs) on the button.

---

## API Contract

### Endpoint

```http
POST /api/category-attributes
Content-Type: application/json
```

### Request

- Type: `List<CategoryGroupDto>`.
- Shape (simplified):

  ```csharp
  public sealed class CategoryGroupDto
  {
      public string CategoryName { get; init; }
      public List<SubCategoryDto> SubCategories { get; init; }
  }

  public sealed class SubCategoryDto
  {
      public int CategoryId { get; init; }
      public string CategoryName { get; init; }
  }
  ```

- JSON is case-insensitive on the server.

### Successful Response

- Type: `List<CategoryAttributesResultDto>`:

  ```csharp
  public sealed class CategoryAttributesResultDto
  {
      public int CategoryId { get; init; }
      public IReadOnlyList<string> Attributes { get; init; } // always 3 items
  }
  ```

- Each subcategory from the input produces exactly one element in the output.
- `attributes` always contains *exactly three* strings.

### Error Responses

All error responses share a common shape:

```csharp
public sealed class ErrorResponse
{
    public string Message { get; }
    public IReadOnlyList<string>? Details { get; }
}
```

Examples:

- Invalid JSON:

  ```json
  {
    "message": "Invalid JSON format. Expected an array of category groups.",
    "details": [
      "The JSON value could not be converted..."
    ]
  }
  ```

- Empty array:

  ```json
  {
    "message": "Input must be a non-empty JSON array of category groups.",
    "details": null
  }
  ```

- Upstream OpenAI failure (network, invalid model, quota, etc.):

  ```json
  {
    "message": "Failed to generate attributes using OpenAI API.",
    "details": [
      "OpenAI request failed with status 401 (Unauthorized). Check logs for more details."
    ]
  }
  ```

- Unexpected server error:

  ```json
  {
    "message": "Unexpected server error while generating attributes.",
    "details": [
      "Object reference not set to an instance of an object."
    ]
  }
  ```

---

## OpenAI Integration

The application uses the standard **Chat Completions** endpoint:

```text
POST https://api.openai.com/v1/chat/completions
```

### Configuration

Configuration is bound to `OpenAiOptions` via `appsettings.json`:

```json
"OpenAI": {
  "ApiKey": "",
  "Model": "gpt-4o-mini",
  "ApiUrl": "https://api.openai.com/v1/chat/completions"
}
```

API key resolution order:

1. `OpenAI:ApiKey` from configuration.
2. `OPENAI_API_KEY` environment variable.

If no key is found, the code throws an `OpenAiException` with a clear error message, which is returned as `502 Bad Gateway` to the caller.

### Request Shape

The `OpenAiChatCompletionRequest` is a minimal wrapper around the fields needed for this task:

```csharp
/// <summary>
/// Request payload for OpenAI's chat completions API.
/// Only the fields relevant for this exercise are included.
/// </summary>
public sealed class OpenAiChatCompletionRequest
{
    /// <summary>
    /// Identifier of the model to use for this request
    /// (for example, <c>gpt-4o-mini</c>).
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Ordered list of chat messages that define the conversation
    /// context (system, user, assistant, etc.).
    /// </summary>
    [JsonPropertyName("messages")]
    public List<OpenAiMessage> Messages { get; set; } = [];

    /// <summary>
    /// Controls randomness in the model output.
    /// Lower values (for example, 0–0.3) make responses more
    /// deterministic, higher values make them more diverse.
    /// </summary>
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.2f;

    /// <summary>
    /// Optional structured response format description.
    /// When set (for example, to type <c>json_object</c>),
    /// the model is instructed to produce output in that format.
    /// </summary>
    [JsonPropertyName("response_format")]
    public OpenAiResponseFormat? ResponseFormat { get; set; }
}
```

The service uses `response_format: { "type": "json_object" }` so that the model consistently returns JSON suitable for `System.Text.Json` deserialization.

---

## CategoryPrompting Configuration

Prompt text is not hardcoded in the service; it is provided via `CategoryPromptOptions` and can be configured in `appsettings.json` under the `CategoryPrompting` section:

```json
"CategoryPrompting": {
  "SystemPrompt": "You are an expert in ecommerce product data. Given a product subcategory name, you must return the three most important, commonly used product attributes for that subcategory. Return attributes that are useful for faceted navigation and product comparison.",
  "UserPromptTemplate": "Subcategory name: "{SubcategoryName}"

Return a JSON object in the following exact shape:

{
  "attributes": [
    "Attribute 1",
    "Attribute 2",
    "Attribute 3"
  ]
}

Rules:
- Always return exactly three attribute names.
- Attribute names must be concise (max 3 words), in English, and human-readable.
- Do not include explanations, comments, or additional fields."
}
```

- `SystemPrompt` defines the assistant role and global behavior.
- `UserPromptTemplate` is a template used for each subcategory. The placeholder `{SubcategoryName}` is replaced with the actual subcategory name at runtime.

If the section is omitted, sensible defaults from `CategoryPromptOptions` are used.

This makes prompt engineering part of configuration rather than source code, so prompts can be refined or A/B-tested per environment without redeploying the application.

---

## Running the Application

### 1. Prerequisites

- .NET 8 SDK installed
- An OpenAI API key with access to a Chat Completions model (e.g. `gpt-4o-mini`)

Check the SDK:

```bash
dotnet --version
```

### 2. Configure API key

Recommended via environment variable:

```bash
# PowerShell
$env:OPENAI_API_KEY = "sk-..."

# bash
export OPENAI_API_KEY="sk-..."
```

Alternatively (local development only), you can hardcode it into `appsettings.json` under `OpenAI:ApiKey`.

### 3. Restore and run

From the project folder:

```bash
cd src/CategoryAttributeGenerator
dotnet restore
dotnet run
```

By default ASP.NET Core will listen on URLs similar to:

- http://localhost:5000  
- https://localhost:7000  

(the actual ports are printed in the console).

### 4. Use the UI

Open the HTTP URL in your browser (e.g. `http://localhost:5000`).  
You should see:

- A dark-themed page with a sample JSON payload.
- A **Generate Attributes** button.
- A status line and an output JSON panel.

Click **Generate Attributes**:

- The UI sends a `POST /api/category-attributes` call.
- The backend calls OpenAI for each subcategory.
- The result appears in the output area.
- The button text is updated with the request duration in microseconds.

---

## Running with Docker

The service can be containerized and run as a standalone container.

### 1. Build the image

From the solution root (where the `src/` folder lives):

```bash
docker build -t category-attribute-generator .
```

### 2. Run the container

You need to pass the OpenAI API key via environment variable and publish the HTTP port:
```bash
docker run --rm \
  -e OPENAI_API_KEY="sk-..." \
  -p 8080:8080 \
  category-attribute-generator
```

### 3. Access the UI and API

- By default the app listens on port `8080` inside the container, so the example above exposes it as:
http://localhost:8080

- Call the API directly:

```bash
curl -X POST "http://localhost:8080/api/category-attributes" \
  -H "Content-Type: application/json" \
  -d '[{ "categoryName": "TVs", "subCategories": [ { "categoryId": 80, "categoryName": "TVs" } ] }]'
```

---

## Design Notes

- **Separation of concerns**
  - Controllers are thin and responsible only for HTTP concerns.
  - `CategoryAttributeService` holds the orchestration logic and prompt construction.
  - `OpenAiClient` encapsulates all HTTP/OpenAI-specific details.
  - `CategoryPromptOptions` pushes prompt engineering into configuration.

- **Error handling**
  - Input validation errors return `400` with structured error messages.
  - Upstream failures (OpenAI) return `502` with a high-level message and details.
  - Unexpected internal errors return `500`, but without leaking sensitive information.

- **Configuration**
  - All OpenAI settings are externalized (`ApiUrl`, `Model`, `ApiKey`).
  - Prompt text is externalized via `CategoryPrompting`.
  - API keys are not hardcoded into code, can come from env vars or secret stores.

- **Extensibility**
  - `ICategoryAttributeService` can be replaced or extended (e.g. caching responses per subcategory).
  - `IOpenAiClient` can be mocked or swapped for another provider in tests.
  - The UI is intentionally simple, easy to replace with a SPA/React/Razor front-end if needed.

- **Determinism**
  - Low `temperature` is used to keep outputs stable and easier to parse/validate.
  - JSON-mode (`response_format`) reduces parsing errors and makes the integration more robust.

---

## Possible Next Steps

If this were to evolve beyond a test task:

- Add unit tests around:
  - attribute parsing logic,
  - error handling and boundary cases,
  - OpenAI client failures (with a fake `IOpenAiClient`).
- Add logging/metrics for:
  - per-subcategory latency,
  - OpenAI error rates,
  - number of processed subcategories per request.
- Support:
  - localization (attributes in multiple languages),
  - configurable number of attributes,
  - per-category overrides for business rules.

---

## Sample call

The screenshot above shows a successful end-to-end run of the application:

- A sample category hierarchy JSON is pasted into the input textarea.
- The **Generate Attributes** button is clicked, triggering a `POST /api/category-attributes` call.
- The backend validates the payload, calls the OpenAI Chat Completions API once per subcategory,
  and aggregates the results.
- The response is rendered in the **Output JSON** panel, where each `categoryId` has exactly three
  generated attributes.
- The button label is updated with the total request duration in microseconds, so it is easy to
  see how long the end-to-end call took on this machine.

![Category Attribute Generator sample run](assets/screencapture-localhost-5000-2025-11-26-13_54_02.png "Category Attribute Generator UI")
