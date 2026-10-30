#!/bin/bash
# Writes Editor/Repair/StrangeToolkitFiles.txt: every file the package ships with its GUID ("<guid><TAB><path>"),
# which the Repair window compares a project against. Run from the package root. Lists tracked and untracked
# (not ignored) files, so it can be run before a commit; the release workflow runs it on the checkout.
set -e
out="Editor/Repair/StrangeToolkitFiles.txt"
{ git ls-files; git ls-files --others --exclude-standard; } | sort -u | grep -v '\.meta$' \
  | grep -vE '^(\.|docs/|Documentation~/|Shelved~/)' | while IFS= read -r f; do
  [ -f "$f.meta" ] || continue
  guid=$(grep -m1 '^guid:' "$f.meta" | awk '{print $2}' | tr -d '\r')
  [ -n "$guid" ] && printf '%s\t%s\n' "$guid" "$f"
done > "$out"
echo "Wrote $(wc -l < "$out") entries to $out"
