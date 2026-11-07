#!/usr/bin/env bash
# Compiles the project, runs the EditMode tests in batch mode and checks Fast IoC code generation.
# Exit codes: 0 passed, 1 failed, 2 couldn't run.
# Set UNITY_PATH to use a specific Unity executable. Close the project in the Editor first.
# Writes Logs/verify/summary.json so agents and CI can read the result without parsing text.
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/Logs/verify"
LOG="$OUT/unity.log"
RESULTS="$OUT/results.xml"
SUMMARY_JSON="$OUT/summary.json"
GENERATION_STATUS="$ROOT/Assets/FastIoC/Generated/GenerationStatus.json"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$ROOT/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"

default_unity_path() {
    case "$(uname -s)" in
        Darwin) echo "/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity" ;;
        Linux) echo "$HOME/Unity/Hub/Editor/$VERSION/Editor/Unity" ;;
        MINGW*|MSYS*|CYGWIN*) echo "/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe" ;;
        *) echo "" ;;
    esac
}

UNITY="${UNITY_PATH:-$(default_unity_path)}"

mkdir -p "$OUT"
rm -f "$LOG" "$RESULTS" "$SUMMARY_JSON"

# Writes the machine-readable result: summary <result> <message> [total] [failed]
summary() {
    local message="${2//\\/\\\\}"
    message="${message//\"/\\\"}"
    printf '{"result": "%s", "message": "%s", "unityVersion": "%s", "testsTotal": %s, "testsFailed": %s}\n' \
        "$1" "$message" "$VERSION" "${3:-null}" "${4:-null}" > "$SUMMARY_JSON"
}

if [ ! -x "$UNITY" ]; then
    echo "Unity $VERSION not found at: $UNITY"
    echo "Install it from Unity Hub, or set UNITY_PATH."
    summary "not-run" "Unity $VERSION not found"
    exit 2
fi

START_MARKER="$OUT/.started"
touch "$START_MARKER"

"$UNITY" -batchmode -nographics -projectPath "$ROOT" \
    -runTests -testPlatform EditMode -testResults "$RESULTS" -logFile "$LOG"
CODE=$?

if grep -q 'another Unity instance is running' "$LOG" 2>/dev/null; then
    echo "Close this project in the Unity Editor, then run verify again."
    summary "not-run" "The project is open in the Unity Editor"
    exit 2
fi

# Compiler errors (CS) and analyzer errors (UA and any other prefix) both fail the build.
ERRORS="$(grep -E 'error [A-Z]{2,}[0-9]{4}' "$LOG" 2>/dev/null | sort -u)"
if [ -n "$ERRORS" ]; then
    echo "Compilation failed:"
    echo "$ERRORS"
    summary "failed" "Compilation failed: $(echo "$ERRORS" | wc -l | tr -d ' ') error(s)"
    exit 1
fi

if [ ! -f "$RESULTS" ]; then
    echo "Unity exited with code $CODE and wrote no test results. See $LOG"
    summary "not-run" "Unity exited with code $CODE and wrote no test results"
    exit 2
fi

RUN="$(grep -m1 '<test-run' "$RESULTS")"
TOTAL="$(echo "$RUN" | sed -n 's/.* total="\([0-9]*\)".*/\1/p')"
FAILED="$(echo "$RUN" | sed -n 's/.* failed="\([0-9]*\)".*/\1/p')"

if [ "$CODE" -ne 0 ] || [ "${FAILED:-1}" != "0" ]; then
    echo "Tests failed: ${FAILED:-unknown} of ${TOTAL:-unknown}. See $RESULTS"
    grep '<test-case ' "$RESULTS" | grep 'result="Failed"' | sed -n 's/.* fullname="\([^"]*\)".*/  \1/p'
    summary "failed" "Tests failed: ${FAILED:-unknown} of ${TOTAL:-unknown}" "${TOTAL:-null}" "${FAILED:-null}"
    exit 1
fi

# A failed Fast IoC generation doesn't stop the compile, so check its status file.
if [ -f "$GENERATION_STATUS" ]; then
    if grep -Eq '"status": *1' "$GENERATION_STATUS"; then
        echo "Fast IoC code generation failed. Fix the installers or constructors, not the generated code:"
        sed -n 's/.*"errorReason": *"\(.*\)",*$/  \1/p' "$GENERATION_STATUS"
        grep -A50 '"errorDetails"' "$GENERATION_STATUS" | sed -n 's/^ *"\(.*\)",*$/  \1/p' | grep -v '^  items$'
        summary "failed" "Fast IoC code generation failed" "$TOTAL" "0"
        exit 1
    fi
    if [ ! "$GENERATION_STATUS" -nt "$START_MARKER" ]; then
        echo "Note: GenerationStatus.json wasn't rewritten during this run, so it may be stale."
    fi
fi

echo "Verify passed: $TOTAL tests."
summary "passed" "Verify passed: $TOTAL tests" "$TOTAL" "0"
