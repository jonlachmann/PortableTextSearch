#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ARTIFACT_DIR="$ROOT_DIR/artifacts/packages"

projects=(
  "PortableTextSearch/PortableTextSearch.csproj"
  "PortableTextSearch.EF9/PortableTextSearch.EF9.csproj"
  "PortableTextSearch.EF10/PortableTextSearch.EF10.csproj"
)

mkdir -p "$ARTIFACT_DIR"

for project in "${projects[@]}"; do
  echo "Packing $project"
  dotnet pack "$ROOT_DIR/$project" \
    --no-restore \
    --configuration Release \
    -p:RunAnalyzers=false \
    --output "$ARTIFACT_DIR"
done

echo
echo "Packages written to $ARTIFACT_DIR"
find "$ARTIFACT_DIR" -maxdepth 1 \( -name '*.nupkg' -o -name '*.snupkg' \) | sort
