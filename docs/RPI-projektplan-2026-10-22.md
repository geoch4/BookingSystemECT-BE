# RPI för ECT och rTMS projektet

## Mål och avgränsning

Granskning och genomförandeplan för frontend, backend, databas, tester och pilotdrift. Målet är en kontrollerad pilot med rätt åtkomst och verifierbara arbetsflöden. Första genomförandesteget nedan är implementerat lokalt. Hela pilotförberedelsen är inte färdig. Inga verksamhetsdata har rättats eller migrerats i denna omgång och pilotaktivering ingår inte i kodleveransen.

## Research och verifierat nuläge

Utgångspunkt 22 oktober 2026: frontendens main är `59c678e`, backendens main `99597f3`. Båda arbetskopiorna var rena vid start. Detta är lokal branchstatus; senaste fjärrändringar har inte hämtats i denna granskning.

Frontend använder React, TypeScript, Vite, React Query och rollstyrda routes. Backend använder ASP.NET Core 8, MediatR, EF Core och SQL Server. RBAC, patient–enhet-relationer, 403 och sessionsåterkallning finns sedan tidigare. Databasens patienttabell förvaltas utanför migrationsmodellen, vilket måste beaktas vid ny miljö och återställning.

Granskade underlag: README, API-uppstart, JWT-/inloggningsflöde, kontroller för autentisering, projektfiler, befintliga tester och tidigare databas-/pilotrapporter. Detta är ingen fullständig penetrationstestning eller granskning av varje kodrad.

### Viktigaste fynd

| Område | Fynd vid start | Status efter första genomförandet |
|---|---|---|
| Databasuppstart | Uppstart migrerade alltid och skapade exempeldata | Båda funktionerna kräver nu uttryckligt opt-in |
| Demodata | Seedern kördes oberoende av miljö | Nekas utanför Development |
| Webbåtkomst | CORS tillät alla ursprung | Explicit lista; HTTPS krävs utanför Development |
| Inloggning | Saknade begränsning av upprepade försök | Högst 10 anrop per minut per anslutande IP och serverprocess; därefter 429 |
| Gamla konton | Felaktig hash kunde ge undantag | Kontrollerade format-/saltfel ger nekad inloggning |
| Frontend | Generiskt fel vid 429 | Tydligt meddelande om att vänta och försöka igen |
| Kodkvalitet | Lint misslyckas i DashboardPage och varnar i AuditLogPage | Rättat; lint godkänd utan fel eller varningar |
| Drifthemligheter | JWT-konfiguration i versionshanterad inställningsfil | Kvar; separat pilotnyckel och hemlighetshantering behövs |
| Verksamhetsdata | Tidigare granskning hittade ofullständiga enheter/konton | Beslut och korrigeringar kvar; ingen ny datagranskning utförd |

Databasfynden från 11 oktober är historiska: 14 aktiva bokningar saknade avdelning, 17 behandlingsenhet, 3 patientrelationer saknade registrerad avdelning, 1 patient hade flera avdelningar, 2 konton hade okänd roll och 10 saknade förväntat BCrypt-prefix. Dessa antal ska inte antas vara aktuella utan ny kontroll av avsedd pilotdatabas. Tidigare uppgradering på återställd kopia lyckades.

## Plan och ordning

| Steg | Leverans | Klart när | Status |
|---|---|---|---|
| 1 | Säker uppstart och inloggning | Opt-in för databasskrivning, nekad produktionsseeding, specifik CORS, 429 och robust hashverifiering testade | Implementerat lokalt |
| 2 | Frontendkvalitet | Befintligt lintfel och varning åtgärdade; patientåtgärder/remissflöden regressionstestade | Lint rättad; build och 68 tester godkända, full verksamhetsacceptans kvar |
| 3 | Driftsättning | Pilotmiljö med byggd frontend, HTTPS, separat drifthemlighet, proxy-/IP-hantering och rätt API-adress | Kvar; miljö behövs |
| 4 | Databasberedskap | Ny backup/återställningsövning i pilotmiljö, granskad migrationsplan, extern patienttabell verifierad | Tidigare lokal övning klar; pilotmiljön kvar |
| 5 | Pilotunderlag | Beslutade konton, roller, avdelningar, mottagare och giltiga patientrelationer | Kvar; verksamhetsbeslut behövs |
| 6 | Full acceptans | Skapande, prioritering, bokning, avbokning, överflyttning, dokument, utskrift, logg och felåtkomst testade i pilotmiljön | Delvis automatiserat; verksamhetstest kvar |
| 7 | Förvaltning och releaser | CI för build/test/lint, releaseversion, driftövervakning, support, återställnings- och stoppregel | Kvar |
| 8 | Pilotbeslut | Datatyp, integrationsbehov, ansvar och resultat godkända av organisationen | Kvar |

### Tekniska beslut för nästa steg

Bevara RBAC:s princip att okänd roll eller saknad relation inte ger åtkomst. Gissa inte historiska enhetstilldelningar. Använd inga riktiga patientuppgifter i testartefakter. Håll migration och applikationsstart separata i pilotdrift. Ingen automatisk uppgradering av beroenden eller aktivering av journal-/SSO-integration utan kompatibilitetskontroll.

Inloggningsbegränsningen använder serverns anslutande IP, inte godtyckliga inkommande forwarded headers. Bakom proxy kan därför många användare dela samma begränsning. Konfigurera endast betrodda proxyer och verifiera verklig IP-hantering i nästa driftsteg. Begränsningen är lokal per serverprocess och ersätter inte distribuerat skydd eller konto-/identitetsleverantörens skydd.

## Implementation i första steget

Backend: `API/DeploymentSafety.cs`, `API/LoginRateLimiting.cs`, ändrad `Program.cs` och `AuthController.cs`, robust `PasswordVerification` i applikationslagret och uppdaterad inloggningshanterare. Nya tester finns i `DeploymentSafetyTests.cs` och `LoginSafetyTests.cs`; HTTP/SQL-testvärden har samma rate-limiterregistrering som API:t.

Frontend: `LoginPage.tsx` visar ett begripligt fel vid 429. Dashboardens förvalda patientåtgärd synkroniseras utan en extra effect-rendering och auditloggens memo-beroende är stabilt. Ingen ny frontendbehörighet eller förändring av patientscope infördes.

### Konfiguration efter ändringen

`Database:ApplyMigrationsOnStartup` och `Database:SeedDemoData` är false om de inte anges. För miljövariabler används `Database__ApplyMigrationsOnStartup` respektive `Database__SeedDemoData`. Demoseeding kräver dessutom Development. Aktivera inget av dessa mot befintlig verksamhetsdatabas av misstag.

`Cors:AllowedOrigins` är en lista över hela ursprung, till exempel `https://pilot.example`, utan sökväg, query eller inloggningsuppgifter. Miljövariabel för första adressen: `Cors__AllowedOrigins__0`. Production och Staging stoppar uppstart om listan saknas eller innehåller HTTP. Development har enbart `http://localhost:5173` och `http://127.0.0.1:5173` som standard; andra lokala portar behöver läggas till uttryckligen.

Serverstart utför inga databasskrivningar med standardinställningen. Operatören behöver först uppgradera en avsedd test-/pilotdatabas kontrollerat. Denna ändring kan göra att tidigare okonfigurerad produktionsstart nu stoppas; det är avsiktligt och behöver hanteras före driftsättning.

## Tester och begränsningar

Backendbygget lyckades med 0 varningar och 0 fel, och 84 ordinarie tester passerade efter ändringarna (tidigare 69). Nya tester provar säkra standardvärden utan databasregistrering, CORS-validering, miljögräns för seeding, felaktiga hashvärden, rätt/fel lösenord samt HTTP 429 och fortsatt åtkomst till annan route.

Frontendens slutkörning: bygge lyckades, lint passerade utan fel eller varningar och 68 tester passerade. Den befintliga byggvarningen om stor JavaScript-fil kvarstår.

De 7 ordinarie Playwright-testerna för roller, 403/401 och tillgänglighet kördes också om och passerade. Dessa använder kontrollerade API-svar och ska skiljas från de 3 webbläsartesterna mot SQL/API nedan.

Det explicita HTTP/JWT/SQL-testet kördes om och passerade med de nya inloggningsreglerna; det inkluderade 3 webbläsartester mot isolerad SQL/API. Integrationstestet använder delad autentisering och rate limiter men testar inte hela produktionsuppstarten eller extern proxy. Uppstartens konfigurationsregler testas separat. Databasschemat har inte ändrats i denna omgång; det explicita migrationstestet har därför inte körts om. Ändringarna levereras på en separat GitHub-branch för granskning. Merge och pilotaktivering ingår inte i denna leverans.

Teknisk referens för middlewareordning och rate limiting: [Microsofts dokumentation](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0).

## Underlag som behövs för att avsluta pilotförberedelsen

Ange separat pilotmiljö och ansvarig, om enbart syntetiska eller riktiga uppgifter ska användas, beslutade användare/enheter och vilka integrationer som är obligatoriska. Dessa uppgifter behövs för de beroende stegen; kodförbättringar som inte kräver dem kan genomföras vidare separat.
