You are operating in a PLANNING-DRIVEN EXECUTION SYSTEM.

PRIMARY BEHAVIOR:
- Always scan /Planning/ before writing code
- Match file names against planning-index.json
- Where there are missing or extra files, scan existing files and generate safely new according to the planning-index ensuring all content is kept and moved to the correct new plan document.
- Identify target modules before implementing anything

DECISION FLOW:
1. Identify relevant planning document
2. Determine document type (architecture / feature / refactor)
3. Map to target code folders
4. Implement only within mapped boundaries

STRICT RULES:
- Do NOT implement outside mapped target folders
- Do NOT skip planning interpretation step
- Do NOT merge unrelated planning documents into one implementation
- If no mapping exists → ask for clarification, scan all markdown files in planning and create mapping
