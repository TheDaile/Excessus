# Texel Splatting dla Unity 6 / URP / Metal

Implementacja jest wariantem GPU-driven techniki Dylana Eberta, dostrojonym do gry FPS
na Apple Silicon. CPU zarządza wyłącznie sześcioma stałymi kamerami sondy i parametrami
klatki. Nie tworzy obiektów ani transformów dla tekseli i nigdy nie odczytuje buforów GPU.

## Uruchomienie

1. Otwórz projekt w Unity i poczekaj na kompilację.
2. Zaznacz główną kamerę FPS.
3. Wybierz `Tools > Excessus > Texel Splatting > Setup Selected Camera`.
4. W komponencie `Texel Splatting Controller` ustaw `Capture Mask` na warstwy świata,
   które mają zostać zastąpione splatami. Broń, ręce gracza, efekty i UI najlepiej
   pozostawić na osobnych warstwach.
5. Uruchom Play Mode. Domyślny profil `128 × 128 × 6` rezerwuje maksymalnie 98 304
   elementy, ale GPU rysuje wyłącznie elementy wpisane do bufora `Append` po cullingu.

## Potok

1. Sonda jest przyciągana do światowej siatki, co stabilizuje przypisanie tekseli przy
   translacji kamery.
2. Aktywne są tylko ściany cubemapy przecinające aktualny kierunek widoku. Każda
   aktywna kamera renderuje kolor i głębię URP.
3. Render Graph kopiuje kolor bezpośrednio do sześciowarstwowego atlasu GPU. Drugi
   blit rekonstruuje pozycję świata z głębi i zapisuje znormalizowaną odległość
   Czebyszewa. Nie ma readbacku ani kopii przez CPU.
4. Compute shader `[numthreads(8, 8, 1)]` rekonstruuje centrum teksela, wykonuje
   frustum/distance culling, wykrywa ciągłość głębi z sąsiadami i dopisuje widoczny
   `SplatData` do `AppendStructuredBuffer`.
5. Licznik bufora jest kopiowany na GPU do argumentów indirect. URP wydaje jedno
   `DrawMeshInstancedIndirect`, a vertex shader buduje światowy quad z indeksu teksela.

`SplatData` ma 24 bajty:

- `float3 position`;
- kolor RGBA8 spakowany do `uint`;
- współrzędne, ściana cubemapy i maska krawędzi spakowane do `uint`;
- odległość Czebyszewa jako `float`.

Układ 24-bajtowy jest przewidywalny na Metal. Celowo nie zastosowano `half4` w buforze
strukturalnym: oszczędność jest niewielka, a zgodność layoutu HLSL → MSL jest bardziej
krucha.

## Ustawienia M1

- Zacznij od rozdzielczości ściany `128`. `256` zwiększa liczbę kandydatów czterokrotnie.
- `Face Cull Dot = -0.23` odpowiada szerokiemu marginesowi podobnemu do implementacji
  referencyjnej i zwykle aktywuje 2–3 ściany zamiast wszystkich sześciu.
- `Capture Every Nth Frame = 2` ogranicza koszt kamer przy statycznym świecie, kosztem
  opóźnienia animowanych obiektów i oświetlenia.
- Bufor `Append` jest GPU-writable, więc nie może być `Immutable`. Optymalizacja pamięci
  zunifikowanej polega tutaj na braku mapowania i readbacku: tekstury, lista widoczności
  i argumenty indirect pozostają przez cały potok na GPU.

## Świadome ograniczenia profilu FPS

Wersja referencyjna używa sond `eye`, `grid` i `previous` oraz przejścia Bayera. Ten
profil używa jednej stabilnej sondy `grid`, aby nie podwajać lub potrajać kosztu kamer
URP na M1. Skutkiem jest klasyczny problem disocclusion: obszar zasłonięty z punktu sondy
nie istnieje w cubemapie. Mniejszy `Grid Step` zmniejsza ten problem, ale częściej
odświeża sondę.

Przezroczystości nie są przechwytywane do stabilnej geometrii splatów. Renderują się
normalnie później, jeżeli ich warstwa nie znajduje się w `Capture Mask`.

## Źródła

- Film: <https://www.youtube.com/watch?v=GhlTMsPoaJw>
- Artykuł i demo: <https://dylanebert.com/texel-splatting/>
- Kod referencyjny: <https://github.com/dylanebert/texel-splatting>
