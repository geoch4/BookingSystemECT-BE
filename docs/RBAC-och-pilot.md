# Samlad dokumentation: frontend, backend och databas

Den här sammanfattningen beskriver ändringarna för Administration/behörigheter, RBAC och pilotförberedelser. Dokumentationen sammanställdes 11 oktober 2026. Samma sammanfattning finns i frontend- och backendrepositoryt.

## Frontend

- Administration visar användarens systemvy, behörighetschips och rollernas behörighetsöversikt.
- Gemensam behörighetslogik styr routes, vyer och tillgängliga åtgärder. Okända roller saknar behörighet. Otillåten åtkomst visar en tydlig 403-vy.
- Avdelningspersonal arbetar med egna patienter/remisser och väljer mottagande behandlingsenhet separat. Klinisk prioritering och behandlingens åtgärder följer respektive roll.
- Administratör kan hantera patientens avdelnings-/enhetsåtkomst, återkalla relationer och överflytta patienter med angiven orsak. Behandlingsenhet kan tilldelas från remissdetaljen.
- Inloggning och utloggning rensar patientcache. Serverns 401-svar rensar ogiltig inloggning. Utloggning väntar på serverbekräftelse och visar fel vid misslyckande.
- Demoåtkomst är begränsad till utvecklingsläge. Demoobjekt filtreras efter roll och enhet. Produktionsbygget tillåter inte frontendskapade demotoken.
- Befintliga arbetsflödesändringar för remisser, patientåtgärder, listor, utskrift, statistik och felvisning ingår också i leveransen.

Frontendkontroller: bygge godkänt, 68 tester i 12 testfiler passerade. Tidigare verifierades 7 vanliga webbläsartester. Separat API/SQL-integration körde ytterligare 3 webbläsartester. Bygget har en varning om stor JavaScript-fil.

## Backend

- JWT-autentisering kontrollerar signatur, giltighetstid, aktuellt konto, roll, enhet och sessionsversion mot databasen.
- Controllers och relevanta hanterare begränsar både läsning och skrivning. Admin behåller övergripande åtkomst. Avdelning och behandlings-/medicinska roller kontrolleras mot olika enhetsfält.
- Patientåtkomst kräver en aktiv, beständig patient–enhet-relation. Historiska bokningar och logghändelser kan inte återaktivera återkallad åtkomst efter migreringen.
- Servern bestämmer avdelning och skapare från inloggningen vid avdelningsskapande. Angivna mottagare kontrolleras mot registrerade behandlingsenheter.
- Klinisk prioritering kräver rätt klinisk roll eller admin. Generell bokningsuppdatering kan inte användas för att kringgå remissåtgärdernas regler.
- Byte av roll, enhet, lösenord, e-post, borttagning eller rollnamn återkallar äldre token genom sessionsrotation. Återställning av gamla värden återaktiverar inte gamla token.
- `POST /api/auth/logout` återkallar kontots befintliga sessioner, även kopierade token. En jämförelse med tidigare sessionsversion skyddar en nyare session mot fördröjd utloggning.
- Underliggande nullbarhet, API-kontrakt och övriga befintliga kompatibilitetsändringar ingår i backendleveransen.

Backendkontroller: bygge med 0 varningar och 0 fel, 69 ordinarie tester passerade. Explicita externa tester för HTTP/JWT/SQL och migrationer passerade separat. SQL-testerna körs uttryckligen och skapar endast särskilt namngivna isolerade testdatabaser.

## Databas och migrationsordning

| Migration | Innehåll |
|---|---|
| `20261006120000_AddDepartmentPriorityToBookings` | Avdelningens prioritet på bokningar. Ingår i leveransen och var redan tillämpad i den granskade lokala databasen. |
| `20261011130000_SeparateReferralUnits` | Separata `DepartmentId` och `TreatmentUnitId`. Äldre källavdelning förs över; behandlingsenhet gissas inte fram. |
| `20261011140000_PatientCareUnitMemberships` | Beständiga patientrelationer per enhet och typ, aktiv status, orsak och ändringstid. Engångsöverföring från historiska bokningar och avdelningsskapandeloggar. |
| `20261011150000_AccountSessionVersion` | Sessionsversion på användarkonton. Befintliga konton får nya värden och tidigare token behöver ersättas genom ny inloggning. |

Patienttabellen är externt förvaltad och undantagen från EF-migrationernas modellgenerering. Den måste finnas i målmiljön. Schema och migrationshistorik ska granskas före aktivering. Aktiverings-SQL i frontendrepositoryt omfattar de tre RBAC-migrationerna efter avdelningsprioritetsmigrationen.

En COPY_ONLY-backup av den befintliga lokala databasen återställdes till en separat testdatabas. De tre RBAC-migrationerna lyckades där och en andra körning ändrade inget. Testkopian och tillfällig backup togs bort. Originalets migrationshistorik och kontrollerade antal poster förblev oförändrade. Inga RBAC-migrationer har aktiverats på originalet genom detta arbete.

## Fynd och kvar före pilot

Granskningen av befintliga uppgifter visade 14 aktiva bokningar utan källavdelning, 17 utan behandlingsenhet efter migrering, 3 avdelningsrelationer utan motsvarande registrerad avdelning och 1 patient med flera avdelningar. 2 aktiva konton har roller som inte stöds och 10 saknar förväntat BCrypt-prefix. Prefixkontrollen validerar inte hela hashvärdet.

Före pilot med dessa uppgifter behöver tilldelningar och konton rättas eller uteslutas från pilotunderlaget. Verksamheten behöver godkänna historisk/delad patientåtkomst. Driftmiljön behöver kontrollerad migrationsaktivering, avstängd demoseeding, separat signeringsnyckel som drifthemlighet, HTTPS, begränsad CORS och skydd mot upprepade inloggningsförsök. Gör acceptanstest i den verkliga pilotmiljön och fastställ backup, återställning, stöd, reservrutin och stoppregel.

Extern SSO-/journalintegration är inte implementerad av nuvarande gateway. Om piloten kräver dessa funktioner måste de bli verkligt anslutna och verifierade. En fristående pilot behöver uttryckligt beslutad manuell rutin. Vid riktiga patientuppgifter krävs organisationens bedömning och godkännande av datahanteringen.

Modellgränser kvarstår: ett enhets-ID per användare, ingen automatisk tidsgräns för patientrelationer och ingen automatisk radering av historiska notifieringar vid återkallning. Serverutloggning gäller alla befintliga sessioner för kontot. Redan påbörjade anrop avbryts inte. Kontoändringar genom direkt SQL behöver också rotera sessionsversionen.

## Dokumentationskarta

Alla nedanstående fördjupningar finns i [frontendrepositoryts outputs-mapp](https://github.com/geoch4/BookingSystemECT-FE/tree/main/outputs):

- `RBAC-punkt-1-2.md`: separata enhetsscope, patientrelationer och administrationsflöden.
- `RBAC-punkt-3-4.md`: sessionsåterkallning och första fullständiga testverifieringen.
- `RBAC-utloggning-aktivering.md`: serverutloggning och isolerat migrationstest.
- `Pilotberedskap.md`: aktuell granskning av befintlig datakopia, konkreta fynd och kriterier före pilot.
- `Pilot-forhandskontroll.sql`: read-only-frågor för förhandsgranskning av schemat före RBAC-migrationerna. Kör bara i avsedd databas; resultat med interna ID:n ska hanteras skyddat.
- `RBAC-aktivering.sql`: genererad migrations-SQL för granskning och kontrollerad aktivering.

`RBAC-verifiering.md` i frontendens rot är en historisk rapport från det första steget. Senare rapporter ersätter dess äldre beskrivningar. Tillfälliga testservrar, skärmbilder, råa lokala granskningsresultat och genererade browserrapporter ingår inte i GitHub-leveransen.

Merge till main är en kodleverans. Den innebär inte att databasmigrationer körs eller att pilotdrift är godkänd.
