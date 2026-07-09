FILESYSTEM SAFETY RULES:

FORBIDDEN:
- Hardcoded Windows path traversal logic
- Linux assumptions about Windows drive structure
- Direct access to C:\ or D:\ in development context

REQUIRED:
- Use IFileSystem abstraction where possible
- Treat all external paths as opaque strings
- Separate "logical game path" vs "physical OS path"

TESTING:
- Only use provided test directories
