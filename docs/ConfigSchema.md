# ConfigSchema

ConfigSchema is a utility type used to define and validate configuration structures against a predefined schema. It ensures that required keys are present and that values conform to specified type hints, facilitating robust configuration management in applications.

## API

### RequiredKeys
- **Type**: `List<string>`
- **Purpose**: Specifies the set of configuration keys that must be present in the validated configuration.
- **Exceptions**: None. This property is read-only and does not throw exceptions.

### TypeHints
- **Type**: `Dictionary<string, string>`
- **Purpose**: Maps configuration keys to their expected type names (e.g., `"string"`, `"int"`). Used during validation to verify value types.
- **Exceptions**: None. This property is read-only and does not throw exceptions.

### LoadFromJson
- **Signature**: `public static ConfigSchema LoadFromJson(string jsonPath)`
- **Purpose**: Deserializes a `ConfigSchema` instance from a JSON file located at `jsonPath`.
- **Parameters**: 
  - `jsonPath` (`string`): The file system path to a JSON file containing schema definitions.
- **Returns**: A `ConfigSchema` instance populated with data from the JSON file.
- **Exceptions**: 
  - `FileNotFoundException`: If the specified file does not exist.
  - `JsonException`: If the file contains invalid JSON or schema data.

### Validate
- **Signature**: `public IReadOnlyList<SchemaViolation> Validate(Dictionary<string, object> config)`
- **Purpose**: Validates a configuration dictionary against the schema's `RequiredKeys` and `TypeHints`.
- **Parameters**: 
  - `config` (`Dictionary<string, object>`): The configuration to validate.
- **Returns**: A read-only list of `SchemaViolation` objects representing discrepancies between the schema and the provided configuration.
- **Exceptions**: 
  - `ArgumentNullException`: If `config` is `null`.

### Key
- **Type**: `string`
- **Purpose**: Represents the configuration key associated with a schema violation.
- **Exceptions**: None. This property is read-only and does not throw exceptions.

### Message
- **Type**: `string`
- **Purpose**: Contains a human-readable description of a schema violation.
- **Exceptions**: None. This property is read-only and does not throw exceptions.

### IsMissing
- **Type**: `bool`
- **Purpose**: Indicates whether a required key is absent from the configuration.
- **Exceptions**: None. This property is read-only and does not throw exceptions.

## Usage

### Example 1: Loading and Validating a Configuration
```csharp
var schema = ConfigSchema.LoadFromJson("schema.json");
var config = new Dictionary<string, object>
{
    { "name", "AppSettings" },
    { "version", 1 }
};

var violations = schema.Validate(config);
foreach (var violation in violations)
{
    Console.WriteLine($"Key: {violation.Key}, Message: {violation.Message}");
}
```

### Example 2: Handling Missing Keys
```csharp
var schema = ConfigSchema.LoadFromJson("schema.json");
var incompleteConfig = new Dictionary<string, object>();

var violations = schema.Validate(incompleteConfig);
var missingKeys = violations.Where(v => v.IsMissing).Select(v => v.Key);
Console.WriteLine($"Missing keys: {string.Join(", ", missingKeys)}");
```

## Notes

- **Edge Cases**:
  - If `RequiredKeys` is empty, the `Validate` method will not report missing keys.
  - If a key in `TypeHints` is not present in the configuration, it is ignored during validation.
  - The `Validate` method does not perform deep type checking; it relies on the string representation of types provided in `TypeHints`.

- **Thread Safety**:
  - `ConfigSchema` instances are immutable after creation via `LoadFromJson`. However, concurrent access to the returned `IReadOnlyList<SchemaViolation>` from `Validate` is safe only if the underlying configuration dictionary is not modified during enumeration.
  - The `LoadFromJson` method is thread-safe for concurrent reads of the same JSON file, provided the file is not modified during execution.
