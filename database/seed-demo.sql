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
    (1, 1, 'Storm in Agder',            'storm in agder',            'Storm',        'Fallen trees and blocked roads',          58.146700, 7.995600),
    (2, 1, 'Flooding in Rogaland',      'flooding in rogaland',      'Flood',        'Flooding rivers',                 58.970000, 5.733100),
    (3, 1, 'Power outage in Trøndelag', 'power outage in trøndelag', 'Power outage', 'Large power outage after heavy snowfall', 63.430500, 10.395100);

INSERT INTO ResourceNeed
    (NeedID, IncidentID, PublicActorID, CategoryID, Title, Description, Quantity,
     SearchRadiusKm, Priority, Status, Email, PhoneNumber, LocationName, Latitude, Longitude) VALUES
    (1, 1, 1, 6, 'Wheel loader for clearing roads',
     'A wheel loader is needed to clear fallen trees and blocked access roads.', 1,
     50, 'critical', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Grimstad', 58.340500, 8.593400),
    (2, 1, 1, 3, 'Emergency generator',
     'A generator is needed to keep the temporary command post and charging station running.', 1,
     50, 'urgent', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Grimstad', 58.340500, 8.593400),
    (3, 1, 1, 9, 'Extra crew for cleanup',
     'Volunteers or experienced personnel are needed for safe cleanup and distribution of supplies.', 4,
     50, 'urgent', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Kristiansand', 58.159900, 8.018200),
    (4, 1, 1, 1, 'Transport vehicle',
     'A vehicle is needed to move equipment and supplies between local response points.', 1,
     50, 'planned', 'new', 'hv07@demo.local', '+47 37 00 07 07',
     'Kristiansand', 58.159900, 8.018200);

INSERT INTO Resource
    (ResourceID, ProviderID, CategoryID, RegionID, Name, Description, Quantity,
     AvailableFrom, Status, Email, PhoneNumber, LocationName, Latitude, Longitude) VALUES
    (1, 2, 6, 8, 'Wheel loader with operator',
     'Medium wheel loader suitable for clearing roads and moving fallen timber.', 1,
     '2026-11-02 08:00:00', 'available', 'person1@demo.local', '+47 900 10 101',
     'Kristiansand', 58.159900, 8.018200),
    (2, 2, 3, 8, 'Mobile emergency generator',
     'Diesel generator for temporary power supply, including cables and basic distribution equipment.', 1,
     '2026-11-02 08:00:00', 'available', 'person1@demo.local', '+47 900 10 101',
     'Lillesand', 58.248800, 8.377800),
    (3, 4, 9, 8, 'Cleanup crew',
     'A small trained crew available for debris removal, logistics and supply distribution.', 4,
     '2026-11-02 09:00:00', 'available', 'person2@demo.local', '+47 900 10 102',
     'Grimstad', 58.340500, 8.593400),
    (4, 4, 1, 8, 'Four-wheel-drive transport vehicle',
     'Four-wheel-drive vehicle with trailer for transporting equipment and emergency supplies.', 1,
     '2026-11-02 09:00:00', 'available', 'person2@demo.local', '+47 900 10 102',
     'Arendal', 58.461500, 8.772500);