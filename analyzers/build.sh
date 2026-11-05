#!/usr/bin/env bash
# Tests and builds the analyzers, then installs them into a Unity project.
# Usage: analyzers/build.sh <unity-project> [assembly-folder]
# The default folder is Assets/Client, so the rules apply to the Client assembly only.
set -eu

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJECT="${1:?Pass the path to the Unity project}"
FOLDER="${2:-Assets/Client}"
TARGET="$PROJECT/$FOLDER/Analyzers"
NAME="UnityArchitecture.Analyzers"

dotnet test "$HERE/$NAME.Tests/$NAME.Tests.csproj" --nologo
dotnet build "$HERE/$NAME/$NAME.csproj" -c Release --nologo

mkdir -p "$TARGET"
cp "$HERE/$NAME/bin/Release/netstandard2.0/$NAME.dll" "$TARGET/$NAME.dll"
cp "$HERE/dll.meta.template" "$TARGET/$NAME.dll.meta"

echo "Installed in $TARGET"
