# KARTAN_INDEX.md

Senast uppdaterad: 2026-05-12

## Status

Kartan är en viktig framtida feature, men den ska inte byggas före
**Project Index + Background Jobs MVP**.

Först stabil Jarvis-kärna. Sedan Kartan.

## Produktgräns

- Kartan ska byggas i `cleen-jarvis` när core-MVP är stabil.
- Kartan får inte införa osäkra skrivvägar.
- Kartan ska ha egen plan och små verifierbara steg.

## MVP

Första Kartan-version:

- egen sida eller panel: "Kartan"
- CesiumJS eller liknande 3D-glob research
- enkel 3D-glob
- fly-to-city
- markörer
- enkel provider-arkitektur
- enkel UI-plan
- mini-chat i hörn som inte täcker kartan

MVP ska inte kräva premium-API:er för att starta.

## Later

Senare:

- map scenes
- kart-rapporter
- mätverktyg
- routing
- offline packs
- places/POI
- score 0-100
- top 5 platsrekommendationer

## Premium/API-dependent

Bygg inte detta först:

- Google Photorealistic 3D Tiles
- Google Places
- live flyg
- live båtar
- avancerade väderlager
- global företagsdata

Det kräver API-nycklar, kostnadskontroll, licenser och tydlig secrets-policy.

## Research-needed

Innan större Kartan-build behövs research om:

- offline-kartformat
- MBTiles/PMTiles/3D Tiles pipeline
- offline geocoding
- offline routing
- OSM building extraction
- lagringsstorlek för Sverige/Skåne
- GPU/RAM-budget för 60 FPS
- licenser för externa kart- och platsdatakällor

## Safety

- Inga API-nycklar i repo.
- Inga tokens i loggar.
- Ingen hidden background-fetch utan synlig status.
- Tunga nedladdningar kräver explicit användarbeslut.
- Kartan får inte blockera Jarvis-chatten.
