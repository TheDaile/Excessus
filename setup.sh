#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

error() {
  echo "[SETUP][BŁĄD] $1"
  exit 1
}

command -v git >/dev/null || error "Git nie jest zainstalowany."
git rev-parse --is-inside-work-tree >/dev/null 2>&1 ||
  error "Skrypt musi znajdować się w repozytorium."

git lfs version >/dev/null 2>&1 ||
  error "Git LFS nie jest zainstalowany."

VERSION="$(sed -n 's/^m_EditorVersion: //p' \
  ProjectSettings/ProjectVersion.txt)"

[[ -n "$VERSION" ]] || error "Nie można odczytać wersji Unity."

echo "[SETUP] Konfigurowanie Git LFS..."
git lfs install --local --skip-repo
git config --local core.hooksPath .githooks
git lfs pull

echo "[SETUP] Konfigurowanie hooków..."
git config --local core.hooksPath .githooks
chmod +x .githooks/pre-push 2>/dev/null || true

case "$(uname -s)" in
  Darwin*)
    OS="macOS"
    UNITY="/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity"
    PROJECT_PATH="$ROOT"
    ;;

  MINGW*|MSYS*|CYGWIN*)
    OS="Windows"
    PROGRAM_FILES="${PROGRAMFILES:-C:\\Program Files}"
    UNITY_WINDOWS="$PROGRAM_FILES\\Unity\\Hub\\Editor\\$VERSION\\Editor\\Unity.exe"
    UNITY="$(cygpath -u "$UNITY_WINDOWS")"
    PROJECT_PATH="$(cygpath -w "$ROOT")"
    ;;

  *)
    error "Nieobsługiwany system: $(uname -s)"
    ;;
esac

echo "[SETUP] System: $OS"
echo "[SETUP] Wymagana wersja Unity: $VERSION"

if [[ ! -e "$UNITY" ]]; then
  echo "[SETUP][UWAGA] Nie znaleziono Unity $VERSION."
  echo "Zainstaluj tę wersję przez Unity Hub."
else
  echo "[SETUP] Unity znalezione."
fi

if [[ -z "$(git config user.name || true)" ]] ||
   [[ -z "$(git config user.email || true)" ]]; then
  echo "[SETUP][UWAGA] Ustaw autora commitów:"
  echo 'git config user.name "TwojaNazwa"'
  echo 'git config user.email "adres@noreply.github.com"'
fi

echo "[SETUP] Aktualny branch: $(git branch --show-current)"
echo "[SETUP] Konfiguracja zakończona."

if [[ "${1:-}" == "--open" && -e "$UNITY" ]]; then
  "$UNITY" -projectPath "$PROJECT_PATH" >/dev/null 2>&1 &
fi
