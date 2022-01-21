#!/usr/bin/env bash

TMP_SOURCE="${BASH_SOURCE[0]}"
while [ -h "$TMP_SOURCE" ]; do
  SCRIPT_PATH="$( cd -P "$( dirname "$TMP_SOURCE" )" >/dev/null 2>&1 && pwd )"
  TMP_SOURCE="$(readlink "$TMP_SOURCE")"
  [[ $TMP_SOURCE != /* ]] && TMP_SOURCE="$SCRIPT_PATH/$TMP_SOURCE"
done
SCRIPT_PATH="$( cd -P "$( dirname "$TMP_SOURCE" )" >/dev/null 2>&1 && pwd )"

set -eu -o pipefail

run-poco-generator() {
    local HERE;
    HERE=$(pwd);
    cd ../Xdxd.DotNet.PocoGenerator/
    dotnet run -- "$@" >&1
    cd "$HERE";
}

CONNECTION_STRING="Server=xdxd-db-playground;Port=6000;Database=net_orm;Uid=net_orm;Pwd=c85d7fb0-6c63-11ec-8ceb-7bbaf9051440;";

run-poco-generator -c $CONNECTION_STRING -n "Xdxd.DotNet.Postgres.Tests" -p "TestDbPocos" -o- > "./TestPocos.cs"

run-poco-generator -c $CONNECTION_STRING -n "Xdxd.DotNet.Postgres.Tests" -p "TestDbPocos" -t "../Xdxd.DotNet.Postgres.Tests/tests-template.txt" -o- > "./DbTests.cs"
