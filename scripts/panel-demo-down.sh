#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ "${1:-}" == "-p" || "${1:-}" == "--purge" ]]; then
    echo "docker compose down -v (removing postgres volume)"
    docker compose down -v
else
    echo "docker compose down"
    docker compose down
fi
