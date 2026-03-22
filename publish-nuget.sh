#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_DIR="$ROOT_DIR/artifacts/packages"
NUGET_SOURCE="https://api.nuget.org/v3/index.json"

if [[ -z "${NUGET_API_KEY:-}" ]]; then
  echo "NUGET_API_KEY is not set."
  echo "Export it first, for example:"
  echo "  export NUGET_API_KEY='your-key-here'"
  exit 1
fi

packages=(
  "PortableTextSearch.EntityFrameworkCore.8.0.0-alpha.4.nupkg"
  "PortableTextSearch.EntityFrameworkCore.9.0.9-alpha.4.nupkg"
  "PortableTextSearch.EntityFrameworkCore.10.0.0-alpha.4.nupkg"
)

symbols=(
  "PortableTextSearch.EntityFrameworkCore.8.0.0-alpha.4.snupkg"
  "PortableTextSearch.EntityFrameworkCore.9.0.9-alpha.4.snupkg"
  "PortableTextSearch.EntityFrameworkCore.10.0.0-alpha.4.snupkg"
)

for package in "${packages[@]}"; do
  path="$PACKAGE_DIR/$package"

  if [[ ! -f "$path" ]]; then
    echo "Missing package: $path"
    exit 1
  fi

  echo "Pushing $package"
  dotnet nuget push "$path" \
    --source "$NUGET_SOURCE" \
    --api-key "$NUGET_API_KEY" \
    --skip-duplicate
done

for symbol in "${symbols[@]}"; do
  path="$PACKAGE_DIR/$symbol"

  if [[ ! -f "$path" ]]; then
    echo "Missing symbols package: $path"
    exit 1
  fi

  echo "Pushing $symbol"
  dotnet nuget push "$path" \
    --source "$NUGET_SOURCE" \
    --api-key "$NUGET_API_KEY" \
    --skip-duplicate
done

echo "All packages pushed."
