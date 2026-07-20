#!/bin/bash

# Create test JSON files
echo '{
  "string_to_number": "123",
  "number_to_string": 456,
  "boolean_to_string": true,
  "string_to_boolean": "false",
  "null_to_string": null,
  "string_to_null": "null",
  "object_to_string": {"key": "value"},
  "string_to_object": "{\"key\": \"value\"}"
}' > /tmp/base.json

echo '{
  "string_to_number": 123,
  "number_to_string": "456",
  "boolean_to_string": "true",
  "string_to_boolean": false,
  "null_to_string": "null",
  "string_to_null": null,
  "object_to_string": "not an object",
  "string_to_object": {"key": "value"}
}' > /tmp/target.json

# Run the diff
echo "=== Running diff ==="
dotnet run -- diff /tmp/base.json /tmp/target.json --format json

# Clean up
rm /tmp/base.json /tmp/target.json