ENVIRONMENT CONTEXT:

- Host OS: Linux
- Target OS: Windows (.exe output)

RULES:
- Windows file paths are NOT real paths in Linux
- Do not attempt to "convert" C:/ or D:/ into Linux filesystem paths
- Only interact with mounted test folders or provided abstractions
- Never assume Windows registry or filesystem access exists during dev

If file access is required:
- Use abstract interfaces
- Use dependency injection for filesystem access
