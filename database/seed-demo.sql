SET NAMES utf8mb4;

-- Demo accounts and data for local testing.

INSERT INTO Organization (OrganizationID, OrganizationName, Sector, RegionID, Latitude, Longitude)
VALUES (1, 'Agder Heimevernsdistrikt 07', 'armed_forces_home_guard', 8, 58.146700, 7.995600);

-- Passwords are intentionally simple for the local demo environment.
-- They are stored as ASP.NET Core Identity hashes, not as plain text.
INSERT INTO `User`
    (UserID, UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash,
     Name, Role, PhoneNumber) VALUES
    (1, 'hv07',    'HV07',    'hv07@demo.local',    'HV07@DEMO.LOCAL',
     'AQAAAAIAAYagAAAAECMHLZZ9Bt6bTZcX9HfqKmmHN8b8+1fIVkgW8Q1QSNAOZ6L05J5jjsjD7xsUW65fDw==',
     'Elias Pedersen', 'PublicActor', '+47 37 00 07 07'),
    (2, 'person1', 'PERSON1', 'person1@demo.local', 'PERSON1@DEMO.LOCAL',
     'AQAAAAIAAYagAAAAEKA8eMs0laJ0qQrSzhxhLHPln4t26D5w+NJ5zcZQMgcqt3JCjnlXl97Asvapxlatzw==',
     'Anders Berg', 'CivilianProvider', '+47 900 10 101'),
    (4, 'person2', 'PERSON2', 'person2@demo.local', 'PERSON2@DEMO.LOCAL',
     'AQAAAAIAAYagAAAAEPrOexVselbeJ7AmKqBrM3827Yl1kP83vvEOaS/0LVxmSPWIkYwCz+UyY+tgrwg8iA==',
     'Nora Solheim', 'CivilianProvider', '+47 900 10 102');

INSERT INTO PublicActor (PublicActorID, OrganizationID) VALUES (1, 1);

INSERT INTO CivilianProvider (ProviderID, ProviderType) VALUES
    (2, 'private_person'),
    (4, 'private_person');

INSERT INTO Incident (IncidentID, PublicActorID, Title, NormalizedTitle, IncidentType, Description, Latitude, Longitude) VALUES
    (1, 1, 'Storm i Agder',              'storm i agder',              'Storm',        'Veltede trær og stengte veier',          58.146700, 7.995600),
    (2, 1, 'Flom i Rogaland',           'flom i rogaland',            'Flom',         'Elver som flommer over',                 58.970000, 5.733100),
    (3, 1, 'Strømbrudd i Trøndelag',    'strømbrudd i trøndelag',     'Strømbrudd',   'Stort strømbrudd etter kraftig snøfall', 63.430500, 10.395100);

INSERT INTO ResourceNeed
    (NeedID, IncidentID, PublicActorID, CategoryID, Title, Description, Quantity,
     SearchRadiusKm, Priority, Status, Email, PhoneNumber, LocationName, Latitude, Longitude) VALUES
    (1, 1, 1, 6, 'Hjullaster til rydding av veier',
     'Det trengs en hjullaster for å rydde veltede trær og stengte adkomstveier.', 1,
     50, 'critical', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Grimstad', 58.340500, 8.593400),
    (2, 1, 1, 3, 'Nødstrømsaggregat',
     'Det trengs et aggregat for å holde den midlertidige kommandoplassen og ladestasjonen i drift.', 1,
     50, 'critical', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Grimstad', 58.340500, 8.593400),
    (3, 1, 1, 9, 'Ekstra mannskap til opprydding',
     'Det trengs frivillige eller erfarent personell for trygg opprydding og utdeling av forsyninger.', 4,
     50, 'planned', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Kristiansand', 58.159900, 8.018200),
    (4, 1, 1, 1, 'Transportkjøretøy',
     'Det trengs et kjøretøy for å flytte utstyr og forsyninger mellom lokale innsatspunkter.', 1,
     50, 'planned', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Kristiansand', 58.159900, 8.018200);

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
     'Arendal', 58.461500, 8.772500);