# Copilot-instruksjoner for prosjektet

## Arbeidsmåte

- Start med å lese relevant kode og dokumentasjon før du foreslår løsninger.
- Når brukeren ber om forklaring, vurdering eller kvalitetssikring, skal du ikke endre filer. Forklar heller hva som bør endres og vis eventuelt et lite eksempel.
- Gjør bare endringer når brukeren uttrykkelig ber om at noe skal implementeres eller redigeres.
- Foreslå enkle løsninger som passer med eksisterende struktur og nivået på prosjektet.
- Bruk kjente prinsipper og best practice, men unngå unødvendig kompliserte abstraksjoner eller store omskrivinger.
- Kontroller etter endringer at dokumentasjonen stemmer med den faktiske koden.
- Se etter eksisterende felles metoder, JavaScript-funksjoner og CSS-klasser før du lager ny logikk eller lokal styling. Gjenbruk eller utvid felles løsninger når samme funksjon eller visuelle regel brukes på flere sider.
- Når du oppretter en pull request i dette prosjektet, skal målbranch være `Prøve-Main` inntil brukeren uttrykkelig ber om noe annet.

## Språk og stil

- Dokumentasjon, forklaringer og README skal skrives på norsk.
- Kode, klassenavn, metoder, variabler og filnavn skal følge prosjektets engelske navngiving der dette allerede brukes.
- Behold studentenes egen språkbruk og stil i dokumentasjonen. Foreslå bare språklige endringer som gjør teksten tydeligere eller mer korrekt.
- Skriv kort og konkret, og forklar tekniske begreper når det er nødvendig.

## Kvalitetssikring

- Se etter at beskrivelser av MVC, dataflyt, responsivt design, Docker og database samsvarer med implementasjonen.
- Pek på feil eller uoverensstemmelser mellom README, kommentarer og kode før du foreslår en løsning.
- Foreslå relevante tester eller en enkel kontroll etter endringer.
- Ikke presenter KI-forslag som ferdig kvalitetssikret. Gruppen skal selv vurdere, tilpasse og teste forslagene.

## Databasefeil i Docker

- Ved feil som `Unknown column` fra EF Core/MySQL skal du først sammenligne modellen og `AppDbContext` med det faktiske schemaet i den kjørende MariaDB-containeren. Ikke anta at feil MariaDB-versjon er årsaken.
- Kontroller alltid om `docker-compose.yml` bruker et persistent volum. Init-script i `database/schema.sql` kjøres bare når volumet opprettes første gang, og oppdaterer ikke en eksisterende database automatisk.
- Hvis schema-scriptet allerede inneholder kolonnen, men den mangler i den kjørende databasen, skal du bruke en trygg, idempotent `ALTER TABLE` med `IF NOT EXISTS`, bevare eksisterende data, og verifisere kolonnen og applikasjonsruten etterpå.
- Hvis schema-scriptet ikke inneholder endringen, skal du oppdatere schemaet og beskrive hvordan eksisterende databaser kan oppgraderes. Ikke slett eller nullstill databasevolumet uten uttrykkelig godkjenning.
