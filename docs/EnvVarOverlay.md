# EnvVarOverlay

Utility class for reading, normalizing, and applying environment variable overlays to configuration dictionaries. It provides static methods to extract environment variables, normalize their keys, and merge them into existing configuration dictionaries while preserving precedence rules.

## API

### `public static Dictionary<string, string> ReadFromEnvironment()`

Reads all environment variables from the current process and returns them as a dictionary.

- **Return value**: A new `Dictionary<string, string>` where each key-value pair corresponds to an environment variable name and its value.
- **Exceptions**: None.

### `public static Dictionary<string, string> Normalize(Dictionary<string, string> input)`

Normalizes the keys of the input dictionary by converting them to uppercase and trimming whitespace.

- **Parameters**:
  - `input`: The dictionary whose keys are to be normalized.
- **Return value**: A new `Dictionary<string, string>` with normalized keys (uppercase, trimmed) and the same values as the input.
- **Exceptions**: Throws `ArgumentNullException` if `input` is `null`.

### `public static Dictionary<string, string> Apply(Dictionary<string, string> baseConfig, Dictionary<string, string> overlay)`

Merges an overlay dictionary into a base configuration dictionary, with the overlay taking precedence for overlapping keys.

- **Parameters**:
  - `baseConfig`: The base configuration dictionary to which the overlay will be applied.
  - `overlay`: The overlay dictionary whose values will override those in `baseConfig` for matching keys.
- **Return value**: A new `Dictionary<string, string>` representing the merged result of `baseConfig` and `overlay`.
- **Exceptions**: Throws `ArgumentNullException` if either `baseConfig` or `overlay` is `null`.

### `public static Dictionary<string, string> Apply(Dictionary<string, string> baseConfig, string prefix)`

Applies environment variables with a given prefix as an overlay to a base configuration dictionary.

- **Parameters**:
  - `baseConfig`: The base configuration dictionary to which the overlay will be applied.
  - `prefix`: The prefix used to filter environment variables (case-insensitive) that will form the overlay.
- **Return value**: A new `Dictionary<string, string>` representing the merged result of `baseConfig` and the filtered environment variables.
- **Exceptions**: Throws `ArgumentNullException` if `baseConfig` is `null` or if `prefix` is `null`.

## Usage

### Example 1: Reading and applying all environment variables
