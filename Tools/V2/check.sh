#!/usr/bin/env bash
# Checks of GU World 2 that need no Unity. Usage: check.sh [core|unity|all]
set -u
here="$(cd "$(dirname "$0")" && pwd)"
dotnet="${DOTNET:-$(command -v dotnet || echo /opt/dotnet/dotnet)}"
what="${1:-all}"
code=0

if [ "$what" = "core" ] || [ "$what" = "all" ]; then
    echo "== Core: rules, content and a bot's playthrough =="
    (cd "$here/Sim" && "$dotnet" run -c Release) || code=1
fi

if [ "$what" = "unity" ] || [ "$what" = "all" ]; then
    if [ -d "$here/UnityCheck" ]; then
        echo "== Unity layer: compiling against reference assemblies and stubs =="
        out="$(cd "$here/UnityCheck" && "$dotnet" build -nologo -v q 2>&1)"
        errors="$(printf '%s\n' "$out" | grep -E "error" | sed -E 's/ \[.*//' | sort -u)"
        if [ -n "$errors" ]; then printf '%s\n' "$errors" | head -60; echo "UNITY LAYER: $(printf '%s\n' "$errors" | wc -l) error(s)"; code=1; else echo "UNITY LAYER COMPILES"; fi
    fi
fi

exit $code
