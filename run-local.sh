#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION="$ROOT_DIR/HomeLabManager.slnx"
API_PROJECT="$ROOT_DIR/HomeLabManager.API/HomeLabManager.API.csproj"
WEB_PROJECT="$ROOT_DIR/HomeLabManager.WEBUI/HomeLabManager.WEBUI.csproj"
API_URL="http://localhost:5015"
WEB_URL="http://localhost:5282"
LOCAL_APP_DATA="$ROOT_DIR/.localappdata"

cleanup() {
  if [[ -n "${API_PID:-}" ]] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" || true
  fi
  if [[ -n "${WEB_PID:-}" ]] && kill -0 "$WEB_PID" 2>/dev/null; then
    kill "$WEB_PID" || true
  fi
}

trap cleanup EXIT INT TERM

echo "Starting HomeLabManager local stack..."
dotnet restore "$SOLUTION"
mkdir -p "$LOCAL_APP_DATA"

ASPNETCORE_ENVIRONMENT=Development \
DOTNET_ENVIRONMENT=Development \
ASPNETCORE_URLS="$API_URL" \
ASPNETCORE_HTTPS_PORT= \
LOCALAPPDATA="$LOCAL_APP_DATA" \
dotnet run --no-launch-profile --project "$API_PROJECT" --no-restore -p:UseSharedCompilation=false &
API_PID=$!

ASPNETCORE_ENVIRONMENT=Development \
DOTNET_ENVIRONMENT=Development \
ASPNETCORE_URLS="$WEB_URL" \
ASPNETCORE_HTTPS_PORT= \
Api__BaseUrl="$API_URL" \
LOCALAPPDATA="$LOCAL_APP_DATA" \
dotnet run --no-launch-profile --project "$WEB_PROJECT" --no-restore -p:UseSharedCompilation=false &
WEB_PID=$!

echo "API PID: $API_PID"
echo "WEBUI PID: $WEB_PID"
echo "API URL: $API_URL"
echo "WEBUI URL: $WEB_URL"
echo "Press Ctrl+C to stop both services."

wait -n "$API_PID" "$WEB_PID"
