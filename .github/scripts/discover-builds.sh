#!/usr/bin/env bash
# Discovers every buildable sample in the repository and emits GitHub Actions matrices.
#
# - every .sln/.slnx (except the root samples.slnx)
# - every .csproj not referenced by one of those solutions
# - every npm project with a package-lock.json
#
# Outputs (to $GITHUB_OUTPUT when set, otherwise stdout):
#   dotnet=[{"path": "...", "os": "...", "kind": "sdk|maui|netfx", "framework": "..."}, ...]
#   node=[{"path": "..."}, ...]
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

prune=(-path ./build -prune -o -path '*/node_modules' -prune -o -path '*/bin' -prune -o -path '*/obj' -prune -o)

find_files() {
  find . "${prune[@]}" -type f "$@" -print | sed 's|^\./||' | LC_ALL=C sort
}

# Prints the project paths referenced by a solution, relative to the repository root.
solution_projects() {
  local solution=$1 dir
  dir=$(dirname "$solution")
  if [[ $solution == *.slnx ]]; then
    grep -oE 'Path="[^"]+proj"' "$solution" | sed -E 's/^Path="(.*)"$/\1/'
  else
    grep -oE '"[^"]+proj"' "$solution" | tr -d '"'
  fi | tr '\\' '/' | while IFS= read -r project; do
    realpath -m --relative-to=. "$dir/$project"
  done
}

dotnet_entries=()

# Adds a matrix entry for a solution or project; the remaining arguments are the project files it builds.
add_dotnet_entry() {
  local path=$1 kind=sdk os=ubuntu-latest framework=
  shift

  if (($#)) && grep -lsq '<UseMaui>true</UseMaui>' "$@"; then
    # iOS/MacCatalyst need a Mac; build the Windows head only.
    kind=maui
    os=windows-latest
    framework=$(grep -hos 'net[0-9.]*-windows[0-9.]*' "$@" | head -n1)
  elif (($#)) && grep -lsq 'ToolsVersion=' "$@"; then
    # Old-style (non-SDK) projects target .NET Framework and need MSBuild + NuGet on Windows.
    kind=netfx
    os=windows-latest
  fi

  dotnet_entries+=("$(jq -cn --arg path "$path" --arg os "$os" --arg kind "$kind" --arg framework "$framework" \
    '{path: $path, os: $os, kind: $kind, framework: $framework}')")
}

declare -A covered=()
while IFS= read -r solution; do
  projects=()
  while IFS= read -r project; do
    covered[$project]=1
    [[ -f $project ]] && projects+=("$project")
  done < <(solution_projects "$solution")
  if ((${#projects[@]} == 0)); then
    # Broken references: classify by the projects next to the solution instead.
    mapfile -t projects < <(find "$(dirname "$solution")" -name '*.csproj')
  fi
  add_dotnet_entry "$solution" "${projects[@]}"
done < <(find_files \( -name '*.sln' -o -name '*.slnx' \) | grep -vx 'samples.slnx')

while IFS= read -r project; do
  [[ -n ${covered[$project]:-} ]] || add_dotnet_entry "$project" "$project"
done < <(find_files -name '*.csproj')

node_entries=()
while IFS= read -r lockfile; do
  node_entries+=("$(jq -cn --arg path "$(dirname "$lockfile")" '{path: $path}')")
done < <(find_files -name package-lock.json)

dotnet_json=$(printf '%s\n' "${dotnet_entries[@]}" | jq -cs .)
node_json=$(printf '%s\n' "${node_entries[@]}" | jq -cs .)

out=${GITHUB_OUTPUT:-/dev/stdout}
{
  echo "dotnet=$dotnet_json"
  echo "node=$node_json"
} >> "$out"
