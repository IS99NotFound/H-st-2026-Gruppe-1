-- ResQ (Kriseberedskap) database schema for MariaDB

SET NAMES utf8mb4;

-- Lookup tables (lists used in dropdown menus)

-- A county. Used to print resource lists per region.
CREATE TABLE Region (
    RegionID    INT          NOT NULL AUTO_INCREMENT,
    RegionName  VARCHAR(100) NOT NULL,

    PRIMARY KEY (RegionID),
    UNIQUE KEY uq_Region_RegionName (RegionName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Type of need or resource. Shared, so a need and a resource can be compared on type.
CREATE TABLE Category (
    CategoryID    INT          NOT NULL AUTO_INCREMENT,
    CategoryName  VARCHAR(100) NOT NULL,
    Description   VARCHAR(255) NULL,

    PRIMARY KEY (CategoryID),
    UNIQUE KEY uq_Category_CategoryName (CategoryName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Users and organizations

-- A public body, e.g. a police district or a municipality.
CREATE TABLE Organization (
    OrganizationID    INT           NOT NULL AUTO_INCREMENT,
    OrganizationName  VARCHAR(150)  NOT NULL,
    Sector            ENUM('state', 'municipality', 'police', 'fire', 'health',
                           'civil_defence', 'armed_forces_home_guard', 'other')
                      NOT NULL,
    RegionID          INT           NOT NULL,
    Latitude          DECIMAL(9,6)  NULL,
    Longitude         DECIMAL(9,6)  NULL,

    PRIMARY KEY (OrganizationID),
    CONSTRAINT fk_Organization_Region
        FOREIGN KEY (RegionID) REFERENCES Region (RegionID),

    CONSTRAINT ck_Organization_Position CHECK (
        (Latitude IS NULL AND Longitude IS NULL)
        OR (Latitude IS NOT NULL AND Longitude IS NOT NULL
            AND Latitude BETWEEN -90 AND 90
            AND Longitude BETWEEN -180 AND 180)
    )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- All users, with the columns ASP.NET Core Identity expects. Administrator is only Role = 'Administrator'.
CREATE TABLE `User` (
    UserID                INT           NOT NULL AUTO_INCREMENT,
    UserName              VARCHAR(50)   NOT NULL,
    NormalizedUserName    VARCHAR(50)   NOT NULL,
    Email                 VARCHAR(255)  NOT NULL,
    NormalizedEmail       VARCHAR(255)  NOT NULL,
    EmailConfirmed        BOOLEAN       NOT NULL DEFAULT FALSE,
    PasswordHash          VARCHAR(255)  NOT NULL,
    SecurityStamp         VARCHAR(64)   NULL,
    ConcurrencyStamp      VARCHAR(64)   NULL,
    PhoneNumber           VARCHAR(20)   NULL,
    PhoneNumberConfirmed  BOOLEAN       NOT NULL DEFAULT FALSE,
    TwoFactorEnabled      BOOLEAN       NOT NULL DEFAULT FALSE,
    LockoutEnd            DATETIME(6)   NULL,
    LockoutEnabled        BOOLEAN       NOT NULL DEFAULT TRUE,
    AccessFailedCount     INT           NOT NULL DEFAULT 0,
    Name                  VARCHAR(100)  NOT NULL,
    Role                  ENUM('PublicActor', 'CivilianProvider', 'Administrator')
                          NOT NULL,
    CreatedAt             DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (UserID),
    UNIQUE KEY uq_User_NormalizedUserName (NormalizedUserName),
    UNIQUE KEY uq_User_NormalizedEmail (NormalizedEmail)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Identity stores the two-factor key and recovery codes here.
CREATE TABLE UserToken (
    UserID         INT           NOT NULL,
    LoginProvider  VARCHAR(128)  NOT NULL,
    Name           VARCHAR(128)  NOT NULL,
    Value          TEXT          NULL,

    PRIMARY KEY (UserID, LoginProvider, Name),
    CONSTRAINT fk_UserToken_User
        FOREIGN KEY (UserID) REFERENCES `User` (UserID)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- A user who works for a public organization. PublicActorID is the same number as UserID.
CREATE TABLE PublicActor (
    PublicActorID   INT NOT NULL,
    OrganizationID  INT NOT NULL,

    PRIMARY KEY (PublicActorID),
    CONSTRAINT fk_PublicActor_User
        FOREIGN KEY (PublicActorID) REFERENCES `User` (UserID)
        ON DELETE CASCADE,
    CONSTRAINT fk_PublicActor_Organization
        FOREIGN KEY (OrganizationID) REFERENCES Organization (OrganizationID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- A user who offers resources. ProviderID is the same number as UserID.
CREATE TABLE CivilianProvider (
    ProviderID    INT NOT NULL,
    ProviderType  ENUM('private_person', 'company', 'farmer', 'contractor',
                       'volunteer_org', 'drone_operator')
                  NOT NULL,

    PRIMARY KEY (ProviderID),
    CONSTRAINT fk_CivilianProvider_User
        FOREIGN KEY (ProviderID) REFERENCES `User` (UserID)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- A code an employee needs to register as a public actor. It decides which organization they belong to.
CREATE TABLE InviteCode (
    InviteCodeID    INT          NOT NULL AUTO_INCREMENT,
    Code            VARCHAR(32)  NOT NULL,
    OrganizationID  INT          NOT NULL,
    ExpiresAt       DATETIME     NOT NULL,
    UsedByUserID    INT          NULL,
    UsedAt          DATETIME     NULL,

    PRIMARY KEY (InviteCodeID),
    UNIQUE KEY uq_InviteCode_Code (Code),
    UNIQUE KEY uq_InviteCode_UsedByUserID (UsedByUserID),
    CONSTRAINT fk_InviteCode_Organization
        FOREIGN KEY (OrganizationID) REFERENCES Organization (OrganizationID),
    CONSTRAINT fk_InviteCode_User
        FOREIGN KEY (UsedByUserID) REFERENCES `User` (UserID)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Incidents and needs

-- A crisis or event (storm, power outage). Needs are grouped under an incident.
CREATE TABLE Incident (
    IncidentID      INT           NOT NULL AUTO_INCREMENT,
    PublicActorID   INT           NOT NULL,
    Title           VARCHAR(150)  NOT NULL,
    Description     TEXT          NULL,
    Status          ENUM('active', 'resolved') NOT NULL DEFAULT 'active',
    Latitude        DECIMAL(9,6)  NOT NULL,
    Longitude       DECIMAL(9,6)  NOT NULL,
    CreatedAt       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (IncidentID),
    CONSTRAINT fk_Incident_PublicActor
        FOREIGN KEY (PublicActorID) REFERENCES PublicActor (PublicActorID),
    CONSTRAINT ck_Incident_Position CHECK (
        Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180
    )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- A request for help within an incident. The map colour is worked out from Priority.
CREATE TABLE ResourceNeed (
    NeedID         INT           NOT NULL AUTO_INCREMENT,
    IncidentID     INT           NOT NULL,
    PublicActorID  INT           NOT NULL,
    CategoryID     INT           NOT NULL,
    Title          VARCHAR(100)  NOT NULL,
    Description    TEXT          NULL,
    Quantity       INT           NOT NULL DEFAULT 1,
    SearchRadiusKm INT           NOT NULL DEFAULT 50,
    Priority       ENUM('planned', 'urgent', 'critical')
                   NOT NULL DEFAULT 'planned',
    Status         ENUM('new', 'under_review', 'assigned', 'resolved', 'cancelled')
                   NOT NULL DEFAULT 'new',
    RequiredFrom   DATETIME      NULL,
    Deadline       DATETIME      NULL,
    ContactInfo    VARCHAR(255)  NOT NULL,
    Latitude       DECIMAL(9,6)  NOT NULL,
    Longitude      DECIMAL(9,6)  NOT NULL,
    CreatedAt      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (NeedID),
    CONSTRAINT fk_ResourceNeed_Incident
        FOREIGN KEY (IncidentID) REFERENCES Incident (IncidentID),
    CONSTRAINT fk_ResourceNeed_PublicActor
        FOREIGN KEY (PublicActorID) REFERENCES PublicActor (PublicActorID),
    CONSTRAINT fk_ResourceNeed_Category
        FOREIGN KEY (CategoryID) REFERENCES Category (CategoryID),

    CONSTRAINT ck_ResourceNeed_Quantity CHECK (Quantity > 0),
    CONSTRAINT ck_ResourceNeed_Radius CHECK (SearchRadiusKm > 0),
    CONSTRAINT ck_ResourceNeed_Dates CHECK (
        RequiredFrom IS NULL OR Deadline IS NULL OR Deadline >= RequiredFrom
    ),
    CONSTRAINT ck_ResourceNeed_Position CHECK (
        Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180
    )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Resources

-- Something a provider offers. "Delete" sets Status to 'withdrawn', so the match history is kept.
CREATE TABLE Resource (
    ResourceID       INT           NOT NULL AUTO_INCREMENT,
    ProviderID       INT           NOT NULL,
    CategoryID       INT           NOT NULL,
    RegionID         INT           NOT NULL,
    Name             VARCHAR(100)  NOT NULL,
    Description      TEXT          NULL,
    Quantity         INT           NOT NULL DEFAULT 1,
    AvailableFrom    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ExpirationDate   DATETIME      NULL,
    Status           ENUM('available', 'unavailable', 'withdrawn')
                     NOT NULL DEFAULT 'available',
    ContactInfo      VARCHAR(255)  NOT NULL,
    Latitude         DECIMAL(9,6)  NOT NULL,
    Longitude        DECIMAL(9,6)  NOT NULL,
    CreatedAt        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (ResourceID),
    CONSTRAINT fk_Resource_CivilianProvider
        FOREIGN KEY (ProviderID) REFERENCES CivilianProvider (ProviderID),
    CONSTRAINT fk_Resource_Category
        FOREIGN KEY (CategoryID) REFERENCES Category (CategoryID),
    CONSTRAINT fk_Resource_Region
        FOREIGN KEY (RegionID) REFERENCES Region (RegionID),

    CONSTRAINT ck_Resource_Quantity CHECK (Quantity > 0),
    CONSTRAINT ck_Resource_Dates CHECK (
        ExpirationDate IS NULL OR ExpirationDate >= AvailableFrom
    ),
    CONSTRAINT ck_Resource_Position CHECK (
        Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180
    )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Matching (many-to-many between needs and resources)

-- Links one need to one resource. The primary key stops the same pair from being matched twice.
CREATE TABLE ResourceMatch (
    NeedID         INT           NOT NULL,
    ResourceID     INT           NOT NULL,
    PublicActorID  INT           NOT NULL,
    Quantity       INT           NOT NULL DEFAULT 1,
    Status         ENUM('proposed', 'accepted', 'declined', 'completed')
                   NOT NULL DEFAULT 'proposed',
    Comment        VARCHAR(500)  NULL,
    MatchDate      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (NeedID, ResourceID),
    CONSTRAINT fk_ResourceMatch_ResourceNeed
        FOREIGN KEY (NeedID) REFERENCES ResourceNeed (NeedID),
    CONSTRAINT fk_ResourceMatch_Resource
        FOREIGN KEY (ResourceID) REFERENCES Resource (ResourceID),
    CONSTRAINT fk_ResourceMatch_PublicActor
        FOREIGN KEY (PublicActorID) REFERENCES PublicActor (PublicActorID),

    CONSTRAINT ck_ResourceMatch_Quantity CHECK (Quantity > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Indexes

-- Resource list per region
CREATE INDEX idx_Resource_Region_Status ON Resource (RegionID, Status);

-- Need list
CREATE INDEX idx_ResourceNeed_Status_Priority ON ResourceNeed (Status, Priority);

-- Radius search on the map
CREATE INDEX idx_Resource_Position ON Resource (Latitude, Longitude);
CREATE INDEX idx_ResourceNeed_Position ON ResourceNeed (Latitude, Longitude);

-- Start data

-- Categories from the case description
INSERT INTO Category (CategoryName, Description) VALUES
    ('Transport',          'Vehicles and transport of people or equipment'),
    ('Drone observation',  'Drones and drone operators'),
    ('Power / generator',  'Generators and emergency power'),
    ('Snow clearing',      'Plowing and snow clearing'),
    ('Sand / gravel',      'Sand, gravel and other materials'),
    ('Heavy machinery',    'Excavator, tractor, wheel loader, forestry machine'),
    ('Evacuation',         'Evacuation and temporary accommodation'),
    ('Communications',     'Communication equipment'),
    ('Crew / personnel',   'Crew and volunteers'),
    ('Premises',           'Premises for accommodation, meeting or storage'),
    ('Boat',               'Boats and sea transport'),
    ('Fuel',               'Fuel for vehicles and generators'),
    ('Other',              'Anything that does not fit the other categories');

-- Regions (counties)
INSERT INTO Region (RegionName) VALUES
    ('Østfold'), ('Akershus'), ('Oslo'), ('Innlandet'), ('Buskerud'),
    ('Vestfold'), ('Telemark'), ('Agder'), ('Rogaland'), ('Vestland'),
    ('Møre og Romsdal'), ('Trøndelag'), ('Nordland'), ('Troms'), ('Finnmark');
