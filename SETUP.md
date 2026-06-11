# Excessus - konfiguracja projektu

## Wymagania

Przed rozpoczęciem zainstaluj:

- Git
- Git LFS
- Unity Hub
- Unity `6000.3.14f1`
- Git Bash, jeśli korzystasz z Windowsa

## Pobranie projektu

```bash
git clone https://github.com/KrajewskiD/Excessus.git
cd Excessus
git switch development
```

## Automatyczna konfiguracja

Skrypt `setup.sh`:

- sprawdza dostępność Git i Git LFS,
- konfiguruje Git LFS dla repozytorium,
- pobiera pliki przechowywane przez Git LFS,
- ustawia `.githooks` jako katalog hooków,
- nadaje hookowi `pre-push` uprawnienia wykonywania,
- odczytuje wymaganą wersję Unity,
- sprawdza, czy Unity jest zainstalowane,
- ostrzega, jeśli autor commitów nie jest skonfigurowany,
- opcjonalnie otwiera projekt w Unity.

Skrypt nie instaluje automatycznie Git, Git LFS, Unity ani Unity Hub.

### macOS

Nadaj skryptowi uprawnienia:

```bash
chmod +x setup.sh
```

Uruchom konfigurację:

```bash
./setup.sh
```

Konfiguracja i otwarcie projektu:

```bash
./setup.sh --open
```

### Windows

Otwórz folder projektu w Git Bash, a następnie uruchom:

```bash
./setup.sh
```

Konfiguracja i otwarcie projektu:

```bash
./setup.sh --open
```

Skrypt nie jest przeznaczony do uruchamiania w `cmd.exe`.

W PowerShellu uruchom go przez Git Bash:

```powershell
& "C:\Program Files\Git\bin\bash.exe" ./setup.sh
```

## Konfiguracja autora commitów

Każdy współpracownik powinien używać własnych danych Git.

Sprawdzenie obecnej konfiguracji:

```bash
git config user.name
git config user.email
```

Ustawienie danych tylko dla tego repozytorium:

```bash
git config user.name "NazwaGitHub"
git config user.email "ADRES_NOREPLY_Z_GITHUB"
```

Adres `noreply` można znaleźć w:

```text
GitHub -> Settings -> Emails
```

## Otwieranie projektu

Projekt wymaga wersji Unity zapisanej w:

```text
ProjectSettings/ProjectVersion.txt
```

Obecnie jest to:

```text
6000.3.14f1
```

Jeśli skrypt nie znajdzie tej wersji, zainstaluj ją przez Unity Hub.

Możesz wskazać niestandardową ścieżkę do Unity.

### macOS

```bash
export UNITY_PATH="/sciezka/Unity.app/Contents/MacOS/Unity"
./setup.sh --open
```

### Windows - Git Bash

```bash
export UNITY_PATH="/c/sciezka/do/Unity.exe"
./setup.sh --open
```

## Praca z branchami

Nową pracę rozpoczynamy od aktualnego `development`:

```bash
git switch development
git pull origin development
git switch -c feature/nazwa-zmiany
```

Po zakończeniu pracy:

```bash
git add .
git commit -m "Opis zmiany"
git push -u origin feature/nazwa-zmiany
```

Następnie tworzymy Pull Request:

```text
feature/* -> development -> main
```

Nie wykonujemy bezpośredniego push do `main`.

## Testy przed push

Hook `.githooks/pre-push` automatycznie uruchamia:

- testy EditMode,
- testy PlayMode.

Jeśli którykolwiek test nie przejdzie, push zostanie zatrzymany.

Wyniki i logi znajdują się w:

```text
TestResults/PrePush/
```

Projekt Unity musi być zamknięty podczas wykonywania testów przez hook.

## GitHub Actions

Pull Request:

```text
development -> main
```

uruchamia testy na GitHub Actions.

Merge do `main` wymaga przejścia:

- `validate-source`,
- `unity-tests (EditMode)`,
- `unity-tests (PlayMode)`.

## Git LFS

Repozytorium jest przygotowane do przechowywania dużych plików przez Git LFS.

Sprawdzenie śledzonych plików:

```bash
git lfs ls-files
```

> Obecny hook `pre-push` uruchamia testy Unity, ale nie wykonuje jeszcze
> `git lfs pre-push`. Przed dodaniem pierwszych plików LFS należy rozszerzyć
> hook o obsługę wysyłania obiektów LFS.

## Pliki zabronione

Nie dodawaj do repozytorium:

- `.env` i jego wariantów,
- plików licencji Unity,
- haseł, tokenów i kluczy prywatnych,
- `Library/`,
- `Temp/`,
- `Obj/`,
- `Logs/`,
- `UserSettings/`,
- `TestResults/`,
- lokalnych buildów.

Sekrety wymagane przez CI przechowujemy wyłącznie w:

```text
GitHub -> Repository Settings -> Secrets and variables -> Actions
```

## Ponowna konfiguracja

Skrypt można uruchamiać wielokrotnie:

```bash
./setup.sh
```

Nie powinien usuwać lokalnych zmian ani modyfikować historii Git.
