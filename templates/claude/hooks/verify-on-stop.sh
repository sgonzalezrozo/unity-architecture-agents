#!/usr/bin/env bash
# Stop hook: an agent can't finish with a broken build.
# Runs scripts/verify.sh when Unity files changed, and sends failures back to the agent.
set -u

MAX_ATTEMPTS=3

ROOT="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "$0")/../.." && pwd)}"
STATE="$ROOT/Logs/verify"
PASSED_FILE="$STATE/last-passed"
ATTEMPTS_FILE="$STATE/attempts"
PATHS="Assets Packages ProjectSettings"

cat > /dev/null
cd "$ROOT" || exit 0
mkdir -p "$STATE"

notify() {
    printf '{"systemMessage": "%s"}\n' "$1"
}

fingerprint() {
    {
        git status --porcelain -- $PATHS
        git diff HEAD -- $PATHS
        git ls-files --others --exclude-standard -z -- $PATHS | xargs -0 git hash-object --
    } 2>/dev/null | git hash-object --stdin
}

# Nothing to check: no pending changes, or the same files already passed.
if [ -z "$(git status --porcelain -- $PATHS 2>/dev/null)" ]; then
    exit 0
fi

CURRENT="$(fingerprint)"
if [ "$CURRENT" = "$(cat "$PASSED_FILE" 2>/dev/null)" ]; then
    exit 0
fi

OUTPUT="$(scripts/verify.sh 2>&1)"
CODE=$?

case "$CODE" in
    0)
        echo "$CURRENT" > "$PASSED_FILE"
        rm -f "$ATTEMPTS_FILE"
        notify "Verify passed."
        exit 0
        ;;
    1)
        ATTEMPTS=$(( $(cat "$ATTEMPTS_FILE" 2>/dev/null || echo 0) + 1 ))
        if [ "$ATTEMPTS" -ge "$MAX_ATTEMPTS" ]; then
            rm -f "$ATTEMPTS_FILE"
            notify "Verify still fails after $MAX_ATTEMPTS attempts. Check Logs/verify/."
            exit 0
        fi

        echo "$ATTEMPTS" > "$ATTEMPTS_FILE"
        {
            echo "scripts/verify.sh failed (attempt $ATTEMPTS of $MAX_ATTEMPTS). Fix this before finishing:"
            echo "$OUTPUT"
        } >&2
        exit 2
        ;;
    *)
        notify "Verify couldn't run, so this change isn't checked. See Logs/verify/."
        exit 0
        ;;
esac
