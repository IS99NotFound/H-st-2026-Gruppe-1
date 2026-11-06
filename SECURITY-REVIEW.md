# Sikkerhetsgjennomgang

## Testinformasjon

- **Gjennomført:** 5. november 2026
- **Utført av:** AI-assistent using Copilot SDK in VS Code
- **Testmetode:** Statisk gjennomgang av kildekode, konfigurasjon og
  Docker-oppsett. Det er ikke gjennomført en full dynamisk penetrasjonstest.
- **Prompt:** «Act as a Senior Application Security Engineer reviewing an
  ASP.NET Core MVC application. Analyze the codebase for security
  vulnerabilities, configuration flaws, and bad practices. Categorize
  feedback into what is good, acceptable for a local student project, and
  must fix for production. Specifically check SQL injection, XSS, CSRF,
  broken access control/IDOR, over-posting/mass assignment and leaked
  secrets. Reference files and provide ASP.NET Core fix snippets.»

Denne gjennomgangen gjelder ASP.NET Core MVC-applikasjonen slik den er
implementert for lokal utvikling i Docker. Den er laget som et arbeidsdokument
for gruppen og er ikke en produksjonssertifisering.

## 1. Det som er bra

- `Program.cs` legger inn global `AuthorizeFilter` og
  `AutoValidateAntiforgeryTokenAttribute`. State-changing POST-kall har derfor
  en antiforgery-kontroll som standard.
- Kontrollere bruker også eksplisitt `[Authorize]` og
  `[ValidateAntiForgeryToken]` på relevante handlinger.
- EF Core/LINQ brukes til databaseoppslag. Det ble ikke funnet dynamisk rå-SQL
  eller SQL-konstruksjon fra brukerinput, og dermed ingen identifisert SQL
  injection-sink.
- Razor-output blir normalt HTML-encodet. Det ble ikke funnet
  `Html.Raw`-bruk eller annen identifisert lagret eller reflektert XSS-sink.
- Ressurser, behov og incidents har eierskaps- eller
  organisasjonskontroller ved endring. Match-funksjonene filtrerer behov på
  innlogget aktørs organisasjon før `needId` brukes.
- Ved oppdatering av behov kopieres tillatte felter eksplisitt til entiteten,
  i stedet for å stole på at alle bindede felter kan oppdateres.
- Passord håndteres gjennom ASP.NET Core Identity med hashing, unike
  brukernavn/e-post og lockout. Login-feil er generiske.
- Det finnes flere nyttige HTTP-sikkerhetstiltak: CSP med nonce,
  `HttpOnly`-cookie, `X-Content-Type-Options`, frame-denial,
  referrer-policy, HTTPS-redirection og HSTS utenfor Development.

## 2. Akseptabelt foreløpig

Følgende er akseptabelt for en isolert studentdemo på en lokal utviklermaskin,
men må ikke tolkes som produksjonskonfigurasjon:

- `docker-compose.yml` kjører webapplikasjonen i `Development` og bruker HTTP
  mellom lokale Docker-tjenester. Dette forenkler debugging, men kan eksponere
  detaljerte feilsider og mangler transportkryptering mot eksterne klienter.
- Demo-brukere, seed-data og utviklingsforbindelsen i
  `appsettings.Development.json` er greit for lokal testing, men skal ikke
  følge med i en produksjonsdeploy.
- MariaDB-porten er publisert til vertsmaskinen for lokale verktøy som
  DBeaver. Det er akseptabelt når maskinen er betrodd og brannmuren ikke
  eksponerer tjenesten.
- Root-passordet i compose-oppsettet kan brukes i en lokal, ikke-delt demo.
  Det er likevel et produksjonsproblem som må håndteres før ekstern drift.
- Tofaktorsteget er en demosimulering der koden vises i nettleseren. Det kan
  brukes for undervisning og lokal testing, men er ikke reell 2FA.

## 3. Må rettes før produksjon / anbefalt praksis

| # | Severity | File | Lines | Vulnerability | Confidence |
|---|----------|------|-------|---------------|------------|
| 1 | 🟠 HIGH | [.env](.env#L1) | 1 | En Geoapify API-nøkkel er commitet i en tracked `.env`-fil. Nøkkelen kan misbrukes og må roteres. | 10/10 |
| 2 | 🟠 HIGH | [Controllers/RegisterController.cs](Controllers/RegisterController.cs#L46-L49) | 46-49 | Uautentiserte brukere kan velge privilegerte roller når miljøet er Development. Compose setter dette miljøet eksplisitt. | 10/10 |
| 3 | 🟠 HIGH | [Controllers/AuthController.cs](Controllers/AuthController.cs#L89-L94), [Controllers/AuthController.cs](Controllers/AuthController.cs#L138-L141) | 89-94, 138-141 | Engangskoden sendes til og beholdes i login-viewet. Dermed gir 2FA ingen uavhengig faktor i produksjon. | 10/10 |
| 4 | 🟠 HIGH | [docker-compose.yml](docker-compose.yml#L8-L14), [docker-compose.yml](docker-compose.yml#L38) | 8-14, 38 | Root-passordet er hardkodet, webappen bruker root, og databaseporten publiseres på alle hostens interfaces. | 9/10 |

### 1. Flytt og roter API-nøkkelen

Fjern nøkkelen fra repository-historikken, roter den hos Geoapify, og bruk
deployment secrets:

```powershell
git rm --cached .env
```

```yaml
# docker-compose.yml
environment:
  Geoapify__ApiKey: ${GEOAPIFY_API_KEY}
```

Legg `.env` i `.gitignore`, og sørg for at den faktiske nøkkelen ikke finnes i
git-historikken etter rotasjon.

### 2. Ikke tillat offentlig oppretting av privilegerte roller

Rollevalget må ikke bare begrenses når miljøet ikke er Development. Opprett
administratorer og Public Actors gjennom en autentisert admin-/invitasjonsflyt:

```csharp
if (model.Role != IdentityUserRole.CivilianProvider)
{
    ModelState.AddModelError(
        nameof(model.Role),
        "Administrator- og Public Actor-kontoer må opprettes av administrator.");
}
```

I tillegg bør privilegerte kontoer opprettes via en egen autorisert handling
med eksplisitt rolle- og organisasjonskontroll.

### 3. Ikke vis engangskoden i login-responsen

Returner bare en referanse til den ventende utfordringen, og lever koden via
en separat kanal eller bruk ASP.NET Identity sin innebygde 2FA:

```csharp
return View("TwoFactor", new TwoFactorViewModel
{
    PendingId = pendingId
});
```

Fjern også `model.DemoCode = pending.Code` ved feil forsøk. Demo-visning må
eventuelt ligge bak en eksplisitt utviklingsflagg som aldri er aktivert i
produksjon.

### 4. Bruk secrets, minste privilegium og lokal databasebinding

Bruk en egen MariaDB-bruker for applikasjonen, hent passord fra secrets, og
bind utviklingsporten kun til localhost. I produksjon bør databaseporten
vanligvis ikke publiseres i det hele tatt:

```yaml
environment:
  MARIADB_ROOT_PASSWORD: ${MARIADB_ROOT_PASSWORD}
  MARIADB_DATABASE: heimevernet
  MARIADB_USER: resq_app
  MARIADB_PASSWORD: ${MARIADB_APP_PASSWORD}
ports:
  - "127.0.0.1:3307:3306"
```

Connection string skal bruke `resq_app`, ikke `root`. Før ekstern drift må
webapplikasjonen også kjøre med `ASPNETCORE_ENVIRONMENT=Production`, HTTPS og
sikre produksjonssecrets.

### Kontroller uten identifisert kritisk funn

Gjennomgangen fant ikke en identifisert SQLi-, XSS-, CSRF- eller
over-posting-sårbarhet i dagens kode. Dette bør likevel verifiseres på nytt
med tester som manipulerer ID-er, roller, skjemaer og tekstfelter før
produksjonssetting.

## Anbefalte tilleggstester

Følgende områder bør testes i tillegg før applikasjonen deles eller settes i
produksjonslignende drift:

### Autentisering og sesjoner

- Test rate limiting og lockout ved mange login-, registrerings- og
  tofaktor-forsøk.
- Kontroller session fixation, cookie-attributter (`Secure`, `HttpOnly` og
  `SameSite`) og at logout ugyldiggjør sesjonen.
- Test utløp, gjenbruk og binding av tofaktorutfordringer til riktig
  innlogging.
- Kontroller passordreset, e-postverifisering og eventuelle
  kontogjenopprettingsflyter.

### Autorisasjon og datalekkasjer

- Test alle GET- og POST-endepunkter med andre bruker-, organisasjons- og
  ressurs-ID-er, ikke bare de viktigste match-endepunktene.
- Kontroller at API-nøkler, kontaktinformasjon, lokasjon, interne ID-er og
  stack traces ikke lekkes i HTML, JSON, logger eller feilmeldinger.
- Test rolleendring, organisasjonstilhørighet og tilgang etter at en konto
  har fått eller mistet en rolle.

### Input, filhåndtering og nettleser

- Test validering av lengde, tallgrenser, negative verdier, datoer,
  koordinater, enum-verdier og uventede Unicode-tegn.
- Test HTML-attributter, URL-felter og eventuelle JSON-/JavaScript-kontekster
  særskilt for XSS.
- Dersom filopplasting legges til: test filtype, innhold, størrelse,
  filnavn, lagringssted og tilgangskontroll.
- Verifiser CSP i en faktisk nettleser, inkludert nonce-håndtering og alle
  eksterne ressurser.

### Infrastruktur og avhengigheter

- Kjør en sårbarhetsskanning av NuGet-pakker, Docker-images og
  JavaScript-avhengigheter.
- Kontroller at databasebrukeren har minste nødvendige privilegier, at
  databaseporten ikke eksponeres eksternt, og at Docker-containere ikke
  kjører med unødvendige privilegier.
- Test produksjonskonfigurasjon separat: HTTPS, HSTS, secrets, logging,
  backup, restore og håndtering av database-/tjenestefeil.
- Kontroller at secrets ikke finnes i git-historikk, CI/CD-logger,
  container-lag eller build-artifacts.

### Robusthet og misbruk

- Test CSRF på alle muterende handlinger fra en fremmed origin.
- Test concurrency ved samtidig matching, oppdatering og sletting.
- Test ressursgrenser og misbruk av radius-, søke- og listeparametere for å
  avdekke denial-of-service eller uforholdsmessig tunge databasekall.
- Kontroller sikker logging: ingen passord, tofaktorkoder, tokens eller
  secrets i logger, og tilstrekkelig logging av privilegerte endringer.
