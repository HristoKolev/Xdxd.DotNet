#!/usr/bin/env bash

TMP_SOURCE="${BASH_SOURCE[0]}"
while [ -h "$TMP_SOURCE" ]; do
  SCRIPT_PATH="$( cd -P "$( dirname "$TMP_SOURCE" )" >/dev/null 2>&1 && pwd )"
  TMP_SOURCE="$(readlink "$TMP_SOURCE")"
  [[ $TMP_SOURCE != /* ]] && TMP_SOURCE="$SCRIPT_PATH/$TMP_SOURCE"
done
SCRIPT_PATH="$( cd -P "$( dirname "$TMP_SOURCE" )" >/dev/null 2>&1 && pwd )"

set -eu -o pipefail

# Clean the solution - remove obj and bin directories from all projects.
# shellcheck disable=SC2046
rm -rf $(find "$SCRIPT_PATH" -type d -name obj)
# shellcheck disable=SC2046
rm -rf $(find "$SCRIPT_PATH" -type d -name bin)

VAR_PACKAGE_DIR="$SCRIPT_PATH/packages";

# Recreate the package directory
mkdir -p "$VAR_PACKAGE_DIR" && rm "$VAR_PACKAGE_DIR" -rf

VAR_HERE=$(pwd)
cd "$SCRIPT_PATH"

# Build packages
dotnet pack -c release -o "$VAR_PACKAGE_DIR" --include-symbols --include-source -p:SymbolPackageFormat=snupkg || cd "$VAR_HERE"

# Push packages
# shellcheck disable=SC2046
dotnet nuget push $( ls "$VAR_PACKAGE_DIR"/*.nupkg ) -k "$NUGET_KEY" -s https://api.nuget.org/v3/index.json --skip-duplicate || cd "$VAR_HERE"

cd "$VAR_HERE"

# Recreate the package directory
mkdir -p "$VAR_PACKAGE_DIR" && rm "$VAR_PACKAGE_DIR" -rf
