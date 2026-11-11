SET NAMES utf8mb4;

-- Demo accounts and data for local testing.
-- Droneobservasjon og Evakuering er ikke registrerbare ressurstyper.

INSERT INTO Organization (OrganizationID, OrganizationName, Sector, RegionID, Latitude, Longitude)
VALUES
    (1, 'Agder Heimevernsdistrikt 07', 'armed_forces_home_guard', 8, 58.146700, 7.995600),
    (2, 'Rogaland Heimevernsdistrikt 08', 'armed_forces_home_guard', 9, 58.970000, 5.733100);

-- Passwords are intentionally simple for the local demo environment.
-- They are stored as ASP.NET Core Identity hashes, not as plain text.
INSERT INTO `User`
    (UserID, UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash, SecurityStamp,
     Name, Role, PhoneNumber) VALUES
    (1, 'hv07',    'HV07',    'hv07@demo.local',    'HV07@DEMO.LOCAL',
     'AQAAAAIAAYagAAAAECMHLZZ9Bt6bTZcX9HfqKmmHN8b8+1fIVkgW8Q1QSNAOZ6L05J5jjsjD7xsUW65fDw==', UUID(),
     'Elias Pedersen', 'PublicActor', '+47 37 00 07 07'),
    (2, 'person1', 'PERSON1', 'person1@demo.local', 'PERSON1@DEMO.LOCAL',
     'AQAAAAIAAYagAAAAEKA8eMs0laJ0qQrSzhxhLHPln4t26D5w+NJ5zcZQMgcqt3JCjnlXl97Asvapxlatzw==', UUID(),
     'Anders Berg', 'CivilianProvider', '+47 900 10 101'),
    (4, 'person2', 'PERSON2', 'person2@demo.local', 'PERSON2@DEMO.LOCAL',
     'AQAAAAIAAYagAAAAEPrOexVselbeJ7AmKqBrM3827Yl1kP83vvEOaS/0LVxmSPWIkYwCz+UyY+tgrwg8iA==', UUID(),
     'Nora Solheim', 'CivilianProvider', '+47 900 10 102'),
    (6, 'hv08',    'HV08',    'hv08@demo.local',    'HV08@DEMO.LOCAL',
     'AQAAAAIAAYagAAAAEGuYE8wZDQYvvgWAPSNOENfy6idFk5cuq/ZE7CpIawxe+/hiBMM0Y8Y4bvatL5mF3g==', UUID(),
     'Ingrid Haugland', 'PublicActor', '+47 51 00 08 08'),
    (7, 'hv072',   'HV072',   'hv072@demo.local',   'HV072@DEMO.LOCAL',
     'AQAAAAIAAYagAAAAECeFp8v3nCI4Mn+VZQKZTv53pCfoclzcK6U2OA+Tcc/+/xNEuN8MFoLA0bb4pd27hw==', UUID(),
     'Sindre Vassbotn', 'PublicActor', '+47 37 00 07 72');

INSERT INTO PublicActor (PublicActorID, OrganizationID) VALUES
    (1, 1),
    (6, 2),
    (7, 1);

INSERT INTO CivilianProvider (ProviderID, ProviderType) VALUES
    (2, 'private_person'),
    (4, 'private_person');

INSERT INTO Incident (IncidentID, PublicActorID, RegionID, Title, NormalizedTitle, IncidentType, Description, Latitude, Longitude) VALUES
    (1, 1, 8, 'Storm i Agder',              'storm i agder',              'Storm',        'Veltede trær og stengte veier',          58.146700, 7.995600),
    (2, 1, 9, 'Flom i Rogaland',           'flom i rogaland',            'Flom',         'Elver som flommer over',                 58.970000, 5.733100),
    (3, 1, 12, 'Strømbrudd i Trøndelag',    'strømbrudd i trøndelag',     'Strømbrudd',   'Stort strømbrudd etter kraftig snøfall', 63.430500, 10.395100),
    (4, 6, 9, 'Storm langs Jærkysten',    'storm langs jærkysten',       'Storm',        'Kraftig vind har ført til stengte veier og skader på strømnettet.', 58.850000, 5.650000),
    (5, 6, 9, 'Flom ved Sandnes',         'flom ved sandnes',            'Flom',         'Vannstanden stiger raskt etter mye nedbør i nedslagsfeltet.', 58.852000, 5.735000),
    (6, 7, 8, 'Skogbrannøvelse i Setesdal','skogbrannøvelse i setesdal','Skogbrann', 'Øvelse med behov for logistikk og støtte under skogbrannøvelsen.', 58.700000, 7.800000),
    -- Krisesentrum i Arendal: fem hendelser tett samlet, slik at kartet viser en klyngesirkel.
    (7,  1, 8, 'Flom i Pollen',              'flom i pollen',              'Flom',       'Havnivået har steget og vann renner inn i gatene rundt Pollen.', 58.461200, 8.767200),
    (8,  1, 8, 'Strømbrudd på Tyholmen',     'strømbrudd på tyholmen',     'Strømbrudd', 'Flere kvartaler på Tyholmen er uten strøm.',                     58.460000, 8.766000),
    (9,  1, 8, 'Veltede trær ved stasjonen', 'veltede trær ved stasjonen', 'Storm',      'Trær har blåst ned og sperrer veien ved Arendal stasjon.',       58.462500, 8.770000),
    (10, 1, 8, 'Brann i bygård Kittelsbukt', 'brann i bygård kittelsbukt', 'Other',      'Brann i eldre trebygning, behov for evakuering.',                58.458000, 8.774000),
    (11, 1, 8, 'Stengt vei ved Barbu',       'stengt vei ved barbu',       'Storm',      'Ras og nedfall har stengt veien mot Barbu.',                     58.466000, 8.776000);

INSERT INTO ResourceNeed
    (NeedID, IncidentID, PublicActorID, RegionID, CategoryID, Title, Description, Quantity,
     SearchRadiusKm, Priority, Status, Email, PhoneNumber, LocationName, Latitude, Longitude) VALUES
    (1, 1, 1, 8, 6, 'Hjullaster til rydding av veier',
     'Det trengs en hjullaster for å rydde veltede trær og stengte adkomstveier.', 1,
     50, 'critical', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Grimstad', 58.340500, 8.593400),
    (2, 1, 1, 8, 3, 'Nødstrømsaggregat',
     'Det trengs et aggregat for å holde den midlertidige kommandoplassen og ladestasjonen i drift.', 1,
     50, 'critical', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Grimstad', 58.340500, 8.593400),
    (3, 1, 1, 8, 9, 'Ekstra mannskap til opprydding',
     'Det trengs frivillige eller erfarent personell for trygg opprydding og utdeling av forsyninger.', 4,
     50, 'planned', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Kristiansand', 58.159900, 8.018200),
    (4, 1, 1, 8, 1, 'Transportkjøretøy',
     'Det trengs et kjøretøy for å flytte utstyr og forsyninger mellom lokale innsatspunkter.', 1,
     50, 'planned', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Kristiansand', 58.159900, 8.018200),
    (5, 4, 6, 9, 3, 'Mobilt sambandspunkt',
     'Det trengs et mobilt sambandspunkt for å opprettholde kommunikasjon mellom innsatslagene langs Jærkysten.', 1,
     35, 'critical', 'new', 'hv08@demo.local', '+47 51 00 08 08',
     'Bryne', 58.735000, 5.648000),
    (6, 5, 6, 9, 1, 'Terrengkjøretøy til evakuering',
     'Et terrenggående kjøretøy trengs for å hjelpe innbyggere ut fra flomutsatte områder.', 2,
     30, 'critical', 'new', 'hv08@demo.local', '+47 51 00 08 08',
     'Sandnes', 58.852000, 5.735000),
    (7, 6, 7, 8, 9, 'Ekstra mannskap til øvelse',
     'Det trengs ekstra mannskap til vakthold, logistikk og støtte under skogbrannøvelsen.', 8,
     60, 'planned', 'new', 'hv072@demo.local', '+47 37 00 07 72',
     'Evje', 58.585000, 7.808000);

INSERT INTO Resource
    (ResourceID, ProviderID, CategoryID, RegionID, Name, Description, Quantity,
     AvailableFrom, Status, Email, PhoneNumber, LocationName, Latitude, Longitude) VALUES
    (1, 2, 6, 8, 'Hjullaster med fører',
     'Mellomstor hjullaster egnet til å rydde veier og flytte veltet tømmer.', 1,
     '2026-11-02 08:00:00', 'available', 'person1@demo.local', '+47 900 10 101',
     'Kristiansand', 58.159900, 8.018200),
    (2, 2, 3, 8, 'Mobilt nødstrømsaggregat',
     'Dieselaggregat for midlertidig strømforsyning, inkludert kabler og enkelt fordelingsutstyr.', 1,
     '2026-11-02 08:00:00', 'available', 'person1@demo.local', '+47 900 10 101',
     'Lillesand', 58.248800, 8.377800),
    (3, 4, 9, 8, 'Oppryddingsmannskap',
     'Et lite, opplært mannskap som kan fjerne rusk, ta logistikkoppgaver og dele ut forsyninger.', 4,
     '2026-11-02 09:00:00', 'available', 'person2@demo.local', '+47 900 10 102',
     'Grimstad', 58.340500, 8.593400),
    (4, 4, 1, 8, 'Firehjulstrukket transportkjøretøy',
     'Firehjulstrukket kjøretøy med tilhenger for transport av utstyr og nødforsyninger.', 1,
     '2026-11-02 09:00:00', 'available', 'person2@demo.local', '+47 900 10 102',
     'Arendal', 58.461500, 8.772500),
    (5, 2, 8, 9, 'Beltevogn for krevende terreng',
     'Robust beltevogn med vinsj og lysutstyr for transport i flom- og vindutsatte områder.', 1,
     '2026-11-05 08:00:00', 'available', 'person1@demo.local', '+47 900 10 101',
     'Bryne', 58.735000, 5.648000),
    (6, 4, 9, 9, 'Frivillig beredskapsmannskap',
     'Erfarent mannskap for samband, logistikk og praktisk støtte ved lokale hendelser.', 6,
     '2026-11-05 08:00:00', 'available', 'person2@demo.local', '+47 900 10 102',
     'Sandnes', 58.852000, 5.735000);