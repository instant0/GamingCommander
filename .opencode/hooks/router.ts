export type Phase = "plan" | "build" | "review";

type State = {
  active: boolean;
  phase: Phase | null;
  job_id: string | null;
  last_updated: string | null;
};

const statePath = ".opencode/hooks/state.json";

function loadState(): State {
  return JSON.parse(Deno.readTextFileSync(statePath));
}

function saveState(state: State) {
  Deno.writeTextFileSync(statePath, JSON.stringify(state, null, 2));
}

/**
 * Called before each agent execution
 */
export function beforeAgentRun(agent: string, jobId: string) {
  const state = loadState();

  // If idle → only allow PLAN to start a new job
  if (!state.active) {
    if (agent !== "plan") {
      throw new Error("Pipeline locked. Start with PLAN agent.");
    }

    state.active = true;
    state.phase = "plan";
    state.job_id = jobId;
    state.last_updated = new Date().toISOString();

    saveState(state);
    return;
  }

  // Prevent cross-job contamination
  if (state.job_id !== jobId) {
    throw new Error("Another job is currently active.");
  }

  // Enforce strict phase ordering
  const order: Phase[] = ["plan", "build", "review"];
  const expected = order[order.indexOf(state.phase!) + 1];

  if (agent !== expected) {
    throw new Error(
      `Invalid phase transition. Expected: ${expected}, got: ${agent}`
    );
  }

  state.phase = agent as Phase;
  state.last_updated = new Date().toISOString();
  saveState(state);
}

/**
 * Called after review completes → unlock system
 */
export function afterReview(jobId: string) {
  const state = loadState();

  if (state.job_id !== jobId) return;

  state.active = false;
  state.phase = null;
  state.job_id = null;
  state.last_updated = new Date().toISOString();

  saveState(state);
}
