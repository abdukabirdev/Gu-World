#!/usr/bin/env bash
# The size of GU World 2's own code: everything written for it, without the interface kit kept
# from main (Scripts/UI/Kit) and without the compile check's stubs of Unity packages.
cd "$(dirname "$0")/../.."
files=$( { find Assets/GuWorld/Core Assets/GuWorld/Scripts Assets/GuWorld/Editor -name '*.cs' -not -path '*/UI/Kit/*'; \
           find Tools/V2 \( -name '*.cs' -o -name '*.py' -o -name '*.sh' -o -name '*.ps1' \) -not -path '*/stubs/*' -not -path '*/bin/*' -not -path '*/obj/*'; \
           find Assets/GuWorld/Resources/Shaders -name '*.shader' -newer Docs/V2/GDD.md 2>/dev/null; } | sort -u)
total=$(cat $files | wc -l)
code=$(cat $files | grep -cv '^\s*$')
echo "GU World 2: $total lines ($code not blank) in $(echo "$files" | wc -l) files"
for part in Assets/GuWorld/Core Assets/GuWorld/Scripts Assets/GuWorld/Editor Tools/V2; do
    n=$(echo "$files" | grep "^$part" | xargs cat 2>/dev/null | wc -l)
    printf '  %-24s %6d\n' "$part" "$n"
done
