SET NAMES utf8mb4;

-- Demo data so the application can save needs and resources before real login exists.
-- Remove when users are created through ASP.NET Core Identity.

INSERT INTO Organization (OrganizationID, OrganizationName, Sector, RegionID, Latitude, Longitude)
VALUES (1, 'Demo Organization', 'other', 8, 58.146700, 7.995600);

INSERT INTO `User` (UserID, UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash, Name, Role) VALUES
    (1, 'demo.publicactor', 'DEMO.PUBLICACTOR', 'publicactor@demo.local', 'PUBLICACTOR@DEMO.LOCAL', 'not-a-real-hash', 'Demo Public Actor', 'PublicActor'),
    (2, 'demo.provider',    'DEMO.PROVIDER',    'provider@demo.local',    'PROVIDER@DEMO.LOCAL',    'not-a-real-hash', 'Demo Provider',     'CivilianProvider');

INSERT INTO PublicActor (PublicActorID, OrganizationID) VALUES (1, 1);

INSERT INTO CivilianProvider (ProviderID, ProviderType) VALUES (2, 'private_person');

INSERT INTO Incident (IncidentID, PublicActorID, Title, NormalizedTitle, IncidentType, Description, Latitude, Longitude) VALUES
    (1, 1, 'Storm in Agder',            'storm in agder',            'Storm',        'Fallen trees and blocked roads',          58.146700, 7.995600),
    (2, 1, 'Flooding in Rogaland',      'flooding in rogaland',      'Flood',        'Flooding rivers',                 58.970000, 5.733100),
    (3, 1, 'Power outage in Trøndelag', 'power outage in trøndelag', 'Power outage', 'Large power outage after heavy snowfall', 63.430500, 10.395100);
