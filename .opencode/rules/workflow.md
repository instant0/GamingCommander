WORKFLOW RULES:

Hooks:
- router.ts is authoritative for phase enforcement
- state.json is the single source of execution lock
- no agent may bypass hook validation

- Only one phase may execute at a time
- Phases must follow order:
  1. plan
  2. build
  3. review

NO EXCEPTIONS:
- No skipping phases
- No parallel agent execution
- No implicit self-triggering of other agents

AUTOMATION LIMIT:
- Agents may suggest next steps but cannot execute them
