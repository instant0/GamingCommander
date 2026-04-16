# Windows Validation Checklist

Use this checklist for milestones that include Windows-specific behavior.

## Environment

- [ ] Windows machine available
- [ ] .NET SDK installed
- [ ] Target launcher clients installed as needed

## UI

- [ ] Application launches
- [ ] Keyboard navigation behaves correctly
- [ ] Fonts, colors, borders, and layout fit the intended retro style

## Detection

- [ ] Steam discovery works against a real install
- [ ] Stand-alone folder scan works on sample game folders
- [ ] Detection failures are logged and non-fatal

## Migration

- [ ] Dry-run path validation works
- [ ] Move-only flow works
- [ ] Move plus link flow works
- [ ] Manifest backup is created before mutation
- [ ] Recovery or rollback path is documented
