# System arkitektur

For denne webapplikasjonen har vi tatt i bruk MVC rammeverket (Model, View og Controllers). Webapplikasjonen er bygget som en monolittisk løsning, hvor kjernefunksjonaliteten er samlet i en webapplikasjon og kjører som en enhet. Dette gjør at de ulike delene av systemet er samlet i samme prosess, samtidig som webapplikasjonen kan kommunisere med og benytte eksterne tjenester ved behov (Microsoft Learn, 2023).

## Drift og Kjøring

Webapplikasjonen er en ASP.NET Core MVC-applikasjon som kjøres i en Docker-container.

### Forutsetninger

- Docker Desktop må være installert og kjøre.
- Maskinen må ha tilgang til internett første gang prosjektet bygges, fordi Docker
    henter .NET-images og NuGet-pakker.
- Nettleseren må ha internettilgang for å laste kartdata fra Leaflet/OpenStreetMap.

### Starte applikasjonen
Siden prosjektet fortsatt er i testfasen, så kjører vi disse to kodene, dette er hver gang vi gjør endringer og ønsker å starte med ren database. Dette sletter tidligere testdata.
Ved nye endringer, i vårt tilfelle:
```bash
docker compose down -v
```

Til vanlig bruker man bare:
```bash
docker compose down
```

Deretter starter du den opp igjen:

```bash
docker compose up --build
```

Når containerne er startet, er webapplikasjonen tilgjengelig på:

`http://localhost:8081`

Ressurser, behov, brukere og tilhørende informasjon lagres nå permanent i MariaDB-databasen. Webapplikasjonen kommuniserer med databasen for å hente og lagre data. Dataene blir bevart selv om webcontaineren starter på nytt, så lenge databasen og dataen ikke slettes.


### Feilsøking

Se status og logger med:

```bash
docker compose ps
docker compose logs web
```

Dersom port `8081` allerede er i bruk, må portmappingen i `docker-compose.yml`
endres.


## MVC rammeverket og hvordan det henger sammen

MVC rammeverket består av Models, Views og Controllers, hvor hver del har sitt eget ansvarsområde. Controller fungerer som et bindeledd mellom brukeren og webapplikasjonen. Den mottar forespørsler fra nettleseren, behandler forespørselen og bestemmer hva som skal returneres tilbake til brukeren. Controlleren kan hente eller lagre data gjennom webapplikasjonens datakomponenter før den returnerer et View.
Et view er det brukeren ser og samhandler med i nettleseren. Det består av HTML markup og dynamisk innhold som sendes til nettleseren. En controller kan returnere en view som viser informasjon til brukeren.
Til sist har vi Model som representerer dataen som brukes i webapplikasjonen. Den kan blant annet inneholde egenskaper for dataene og regler for validering. I webapplikasjonen brukes modeller og viewmodeller til å overføre og håndtere data mellom ulike deler av systemet. I webapplikasjonen brukes modeller til å representere informasjonen som systemet jobber med. 

ResourceEntry representerer en ressurs som registreres i systemet. Når informasjonen skal lagres eller hentes, kommuniserer den med MariaDB-databasen. 
Sammen gjør disse komponentene at webapplikasjonen får en tydelig struktur, hvor presentasjon, håndtering av forespørsler og data er delt opp i separate deler (Walther, 2022).

## Hvordan data flyter gjennom systemet (eksempel fra koden)
Data flyter gjennom systemet ved at brukeren sender en forespørsel fra nettleseren til webapplikasjonen. Forespørselen mottas av en Controller, som behandler informasjon og vil da eventuelt kommunisere med databasen gjennom AppDbContext om dette er relevant. Controlleren returnerer deretter en View som presenterer resultatet for brukeren.

Et eksempel på dette er når en bruker registrerer en ny ressurs. Brukeren fyller ut registreringsskjemaet på nettsiden og trykker på "Registrer ressurs". Skjemaet sender informasjonen til HomeController gjennom en POST-forespørsel til metoden RegisterResource.
Dataene sendes til controlleren som et ResourceEntry-objekt. Dette objektet inneholder informasjon brukeren har fylt inn om ressursen. 

Før ressursen lagres, kontrollerer systemet om informasjonen er gyldig ved hjelp av ModelState.IsValid. Dersom informasjonen ikke består valideringen, sendes brukeren tilbake til registreringsskjemaet med en eller flere feilmeldinger som forklarer hva som "feilet". Hvis informasjonen er gyldig, kontrollerer systemet hvilken rolle brukeren har, og knytter deretter ressursen til riktig bruker ved å sette blant annet ProviderId, RegionId og registreringspunktet.

Til slutt legges ressursen inn i databasen gjennom AppDbContext ved hjelp av _db.Resources.Add(resource). Og endringer lagres med _db.SaveChanges(), da vil Entity Framework Core håndtere kommunikasjonen med MariaDB-databasen. 

## Hvordan bruker, webapplikasjon og eventuelle andre komponenter henger sammen
Systemet består av en bruker som kommuniserer med webapplikasjonen gjennom en nettleser. Webapplikasjonen er utviklet med ASP.NET Core og MVC og håndterer forespørsler, data og visning. Webapplikasjonen har også mulighet til å kommunisere med andre komponenter, som karttjenesten, for å hente og lagre nødvendig informasjon. 

Ressurser, behov, brukere og annen informasjon lagres i MariaDB-databasen. Webapplikasjonen bruker AppDbContext og Entity Framework Core for å hente og lagre informasjon. Databasen kjører i egen Docker-container, adskilt fra selve webapplikasjonen.

Docker brukes til å pakke webapplikasjonen og dens avhengigheter inn i et isolert miljø, dette gjør at webapplikasjonen kan kjøres på samme måte på ulike maskiner og miljøer (dockerdocs, u.d.). I prosjektet bruker vi Docker Compose til å starte og administrere webapplikasjonen lokalt. ASP.NET Core-webapplikasjonen bygges ved hjelp av en Dockerfile.

Slik henger komponentene sammen illustrert med et diagram:
### Systemarkitektur

Slik henger komponentene sammen:

```mermaid
flowchart TD
    Bruker["Bruker"] --> Nettleser["Nettleser"]

    subgraph Docker["Docker-miljø"]
        App["ASP.NET Core MVC-webapplikasjon"]

        subgraph MVC["MVC"]
            Controller["Controllers"]
            Model["Model / ViewModels"]
            View["Views"]
        end

        DbContext["AppDbContext / Entity Framework Core"]
        Database[("MariaDB Database")]

        App --> Controller
        Controller --> Model
        Controller --> View
        Controller --> DbContext
        DbContext <--> Database
    end

    Nettleser <--> App

    KartAPI  --> Leaflet["Leaflet"]

    App --> KartAPI["Kart / API"]
```

## Responsivt design

Vi har tilpasset webapplikasjonen slik at den fungerer på både store og små skjermer. På større skjermer vises ressursregistreringen i et sidepanel ved siden av kartet. På mindre skjermer får kartet mest mulig plass, mens navigasjonen flyttes til bunnen av skjermen og ressursregistreringen åpnes som et panel over kartet.

Det responsive oppsettet er laget med CSS media queries og fleksible høyder og bredder. Når sidepanelet åpnes eller lukkes på mobil, beregnes kartets størrelse på nytt slik at kartet fortsatt vises riktig. Vi har også beholdt koblingen mellom kartet og ressursregistreringen, slik at et valgt punkt i kartet fyller inn breddegrad og lengdegrad i skjemaet. På denne måten kan brukeren registrere ressurser uavhengig av hvilken skjermstørrelse som brukes (MDN, u.d.).

Navigasjonen bruker Bootstrap Icons for å gjøre knappene og sidene lettere å kjenne igjen.

## Testbrukere
| Brukernavn | Rolle | Organisasjon | Passord |
|---|---|---|---|
| `hv07` | Offentlig aktør | 1 | `hv07` |
| `hv072` | Offentlig aktør | 1 (samme som hv07) | `hv072` |
| `hv08` | Offentlig aktør | 2 | `hv08` |
| `person1` | Ressursleverandør | | `person1` |
| `person2` | Ressursleverandør | | `person2` |

### Enhetstester

Enhetstestene ligger i prosjektet `H-st-2026-Gruppe-1.Tests` (xUnit, Moq og SQLite i minnet). De krever ikke at Docker kjører. Kjør dem slik:

```bash
cd H-st-2026-Gruppe-1.Tests
dotnet test
```

### Brukervennlighetstest

#### Test 1

Testere:
- Tester 1: IT-bakgrunn, 22
- Tester 2: Musikkstudent, 16
- Tester 3: Vaskehjelp, 43

**Oppgave 1 (ressursleverandør):** Du har en generator du kan låne ut hvis noen trenger den. Logg inn og registrer den i systemet. Sett den på kartet der du bor.

| Tester | Tid | Alene? | Stopp/feil | Høyt tenkt |
|---|---|---|---|---|
| 1 | 02.22 | Ja | Ingen | Ingenting bemerkelsesverdig |
| 2 | 03.22 | Ja | Knotet med å finne ressursregistrering | "Skjønner ikke helt hvor man registrerer" |
| 3 | 08.03 | Ja | Slet med å finne ressursregistrering og hva som skulle fylles ut | "Det er jo ingen registrerings-side?" og "Hva skal jeg fylle inn her?" |

**Oppgave 2 (offentlig aktør):** Du jobber i kommunen, og dere trenger strøm til et sted der det er strømbrudd. Logg inn og registrer at dere trenger det. Det haster veldig.

| Tester | Tid | Alene? | Stopp/feil | Høyt tenkt |
|---|---|---|---|---|
| 1 | 02.56 | Ja | Å registrere behov via kartet og ikke behov siden | "Er det under Behov? Nei, der er det bare en liste." |
| 2 | 03.45 | Ja | Å registrere behov via kartet og ikke behov siden og tok planlagt istedenfor akkutt. | Ingenting bemerkelsesverdig |
| 3 | 04.23 | Ja | Å registrere behov via kartet og ikke behov siden | "Nå ble jeg veldig forvirret" |

**Oppgave 3 (offentlig aktør):** Finn en ressurs som passer til behovet du la inn, og gi den til behovet. Når strømmen er på igjen, marker behovet som løst.

| Tester | Tid | Alene? | Stopp/feil | Høyt tenkt |
|---|---|---|---|---|
| 1 | 00.45 | Ja | Ingen | Ingenting bemerkelsesverdig |
| 2 | 01.37 | Ja | Usikker på hva «Vurder» betyr | Ingenting bemerkelsesverdig |
| 3 | 02.30 | Ja | Usikker på hva «Matching» betyr | Ingenting bemerkelsesverdig |

**Spørsmål:**

| Spørsmål | Tester 1 | Tester 2 | Tester 3 |
|---|---|---|---|
| Hva var vanskeligst? | Å skjønne at man må klikke på kartet for å registrere | Å finne hvor man registrerer ressursen | Å finne ressursregistreringen og vite hva som skulle fylles ut |
| Hva var lettest å forstå? | Kartet og menyen | Å fylle ut skjemaene | Matching og «Tildel» |
| Hvor lett var det å bruke, fra 1 (veldig vanskelig) til 5 (veldig lett)? | 4 | 4 | 3 |

**Funn i Test 1**

Alle tre greide oppgavene alene. Snitt-tid ble 4:36(registrere ressurs), 3:41(registrere behov) og 1:37(matching). Snittet på letthetsgraden ble: 3,7 av 5.

Vanskeligst var å finne hvor man registrerer. Ved ressurs ble to av tre forvirret, og én brukte over åtte minutter. Ved behov gikk alle tre først til Behov-siden, som bare viser en liste, i stedet for kartet. Under matching var to usikre på «Vurder» og «Matching». Skjemaene var enkle å fylle ut.

Det brukerne hadde til felles var at alle slet med å finne ut hvor man registrerte ressurser og behov, men skjemaene var enkle å fylle ut når de først fant dem.

**Hva vi vil endre**

- Tydelig knapp for å registrere ressurs og behov på listesidene.
- Enklere navn på de forskjellige knappene, selv om case-beskrivelsen bruker dem. 

# Test scenarioer
En essensiell del av utviklingsprosessen er å teste produktet både underveis og etterpå. Det er viktig at en både avgrenser hva en skal teste, samtidig som at det holdes relevant til hva brukerne skal gjøre i applikasjonen. Ved å lage test scenarier kan en sette spesifikke deler av applikasjonen i rampelyset og fokusere på det som fungerer eller ikke fungerer. Det er også mulig at en avslører flere problemstillinger enn en originalt hadde forestilt seg, noe som kan være enklere å fikse i de tidligere fasene enn ved slutten av produksjonen. Hvis en kun tester avsluttende kan konsekvensene bli mye større da det kan oppstå en domino-effekt der en feil leder til flere feil videre i systemet.

##	Scenario - Forventninger - Resultat - Status
| # |	Test type | Scenario | Forventet resultat | Faktisk resultat | Status |
|---|------------|--------------------|------------------|--------|
| 1	| Systemtest | Data eksisterer fortsatt etter omstart | Data er der etter omstart | Opprettet ressurs dukket opp etter å lukket og startet applikasjon på nytt | Ok |
| 2	| Systemtest | Brukerkonto opprettes og lagres | Brukerkonto kan sees i bruker tabellen | Tabell tilhørende MariaDB viser ny bruker | Ok |
| 3	| Systemtest | Bruker kan opprette ressurs | Ressurs kommer i oversikt etter skjema er fylt ut | Ressurs ble opprettet og er synlig i oversikt og på kart | Ok |
| 4	| Systemtest | Hendelse kan opprettes | Hendelse er synlig for alle | Hendelse var synlig for alle brukergrupper | Ok |
| 5	| Systemtest | Mobil versjon kan gjøre det samme som PC versjon | Alle knapper fungerer likt på mobilversjon | Mobilversjon gjør det samme som webløsning | Ok |
| 6 | Sikkerhetstest | Brukersider er ikke tilgjengelig uten å logge på | Feilmelding når en prøver å navigere til bruker side | Ingen direkte feilmelding, men en blir returnert til pålogging | Ok, kan legge inn feilmelding etterhvert |
| 7 | Sikkerhetstest | Offentlig aktør side er ikke tilgjengelig når du er logget inn som bruker | Feilmelding på offentlig aktør sider | Bruker blir logget ut når de prøver å gå til offentlig aktør side | Delvis ok, burde være feilmelding i stedet for utlogging |

# Bruk av kunstig intelligens (KI/AI) under prosjektet
Vi har brukt KI som et støtteverktøy gjennom prosjektet. Vi har ikke brukt KI for å generere ferdig tekst eller kode (unntatt enhetstestene og oversetting av frontend, se under), men heller som læringsverktøy, til å skjønne hva oppgaven spør om, feilsøking og forklaring av feilmeldinger, men også om vi mangler noe i teksten, eller dokumentasjon.

## KI-Verktøy ##
Disse har hovedsakelig blitt brukt til å forklare tekniske konsepter innenfor programmering, forstå feilmeldinger, videreutvikle ideer til systemets funksjonalitet.
•	Claude, Sonnet 5/5.5
•	GitHub Copilot
Claude ble i tillegg brukt til å skrive enhetstestene og oversetting av frontend. Vi leste gjennom, kjørte og rettet testene og ordvalg før de ble tatt i bruk.

Disse ble brukt for å vise til skrivefeil i tekst og omstrukturering, slik at alt blir grammatisk riktig.
•	ChatGPT
•	Microsoft Copilot

Vi har selv vurdert, tilpasset og testet forslagene fra KI før implementasjon. KI har dermed blitt brukt som et støtteverktøy for læring, ideutvikling og problemløsning.
### Noen prompts vi har brukt aktivt gjennom innleveringen er:

•	Kan du hjelpe oss med å forstå denne feilmeldingen?

•	Hvorfor sliter docker/mariaDB med å starte?

•	Kan du forklare denne kodeblokken?

•	Hva er MVC komponenter?

•	Hvorfor vil ikke webapplikasjonen kjøre?

•   Gi meg tilbakemelding på besvarelse ifht. oppgavetekst

•	Legg til enhetstesting

•	Reduser antall tester til et antall vi kan lese og forstå

•   Kan du oversette frontenden fra Engelsk til Norsk?

## Refleksjon ##
Alle forslag fra KI ble vurdert og testet før det ble tatt i bruk. KI ble brukt som støtte for læring og problemløsning, ikke som erstatning for eget arbeid. Gruppen hadde selv ansvaret for implementasjon og endelige beslutninger.

# Bibliografi

dockerdocs. (u.d.). What is Docker? Hentet fra https://docs.docker.com/get-started/docker-overview/

Microsoft Learn. (2023, Juli 3). Common web application architectures . Hentet fra https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures

Walther, S. (2022, November 7). Understanding Models, Views, and Controllers (C#) . Hentet fra https://learn.microsoft.com/en-us/aspnet/mvc/overview/older-versions-1/overview/understanding-models-views-and-controllers-cs

MDN. (u.d.). Responsive web design. Hentet fra https://developer.mozilla.org/en-US/docs/Learn_web_development/Core/CSS_layout/Responsive_Design

I denne oppgaven brukte jeg ChatGPT-5.6 Luna til å identifisere og rette opp grammatiske feil, forbedre språkstrukturen, finne synonymer, forkorte teksten og hjelp med generering av diagrammet i GitHub. Jeg brukte ikke ChatGPT-5.6 Luna til å skrive hele avsnitt, men heller til å forbedre min egen tekst, der jeg korrekturleste og kvalitetssikret teksten. Jeg har brukt ChatGPT i tråd med UiAs retningslinjer for bruk av kunstig intelligens.
