-- Heimevernet "Kriseberedskap" - databaseskjema (MariaDB)
-- Basert på klassediagram, event table og statecharts fra Deliverable 1
-- v0.1

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- basis for alle brukere, public sector entity og civilian provider arver fra denne
CREATE TABLE user_account (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,

    role ENUM(
        'public_actor',
        'privileged_public_actor',
        'civilian_provider',
        'administrator'
    ) NOT NULL,

    -- 2FA løses av rammeverket, ikke egne felt her

    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- stat, kommune, politi, brann, helse, sivilforsvaret, heimevernet osv
CREATE TABLE public_sector_entity (
    user_account_id INT PRIMARY KEY,
    organization_name VARCHAR(255) NOT NULL,
    sector_type ENUM(
        'stat', 'kommune', 'politi', 'brann', 'helse',
        'sivilforsvaret', 'forsvaret_heimevernet', 'annet'
    ) NOT NULL,
    contact_point VARCHAR(255) NULL,

    FOREIGN KEY (user_account_id) REFERENCES user_account(id) ON DELETE CASCADE
);

-- privatperson, bedrift, bonde, entreprenør, frivillig org, droneoperatør
CREATE TABLE civilian_provider (
    user_account_id INT PRIMARY KEY,
    provider_type ENUM(
        'privatperson', 'bedrift', 'bonde', 'entreprenor',
        'frivillig_organisasjon', 'droneoperator'
    ) NOT NULL,
    contact_point VARCHAR(255) NULL,

    FOREIGN KEY (user_account_id) REFERENCES user_account(id) ON DELETE CASCADE
);

CREATE TABLE geographic_location (
    id INT AUTO_INCREMENT PRIMARY KEY,
    latitude DECIMAL(9,6) NOT NULL,
    longitude DECIMAL(9,6) NOT NULL,
    address VARCHAR(255) NULL,
    region VARCHAR(100) NULL
);

CREATE TABLE category (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(255) NULL
);

CREATE TABLE crisis_event (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    category_id INT NULL,
    location_id INT NULL,
    created_by INT NOT NULL,

    priority_color ENUM('red', 'yellow', 'green', 'blue') NOT NULL DEFAULT 'yellow',
    status ENUM('active', 'resolved') NOT NULL DEFAULT 'active',

    registered_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    FOREIGN KEY (category_id) REFERENCES category(id),
    FOREIGN KEY (location_id) REFERENCES geographic_location(id),
    FOREIGN KEY (created_by) REFERENCES public_sector_entity(user_account_id)
);

CREATE TABLE resource_need (
    id INT AUTO_INCREMENT PRIMARY KEY,
    crisis_event_id INT NOT NULL,
    category_id INT NOT NULL,
    location_id INT NULL,
    created_by INT NOT NULL,

    description TEXT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    deadline DATETIME NULL,

    priority ENUM('red', 'yellow', 'green', 'blue') NOT NULL DEFAULT 'yellow',
    status ENUM('new', 'under_review', 'allocated', 'resolved', 'cancelled')
        NOT NULL DEFAULT 'new',

    registered_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    FOREIGN KEY (crisis_event_id) REFERENCES crisis_event(id) ON DELETE CASCADE,
    FOREIGN KEY (category_id) REFERENCES category(id),
    FOREIGN KEY (location_id) REFERENCES geographic_location(id),
    FOREIGN KEY (created_by) REFERENCES public_sector_entity(user_account_id)
);

CREATE TABLE civilian_resource (
    id INT AUTO_INCREMENT PRIMARY KEY,
    civilian_provider_id INT NOT NULL,
    category_id INT NOT NULL,
    location_id INT NULL,

    name VARCHAR(255) NOT NULL,
    description TEXT NULL,
    quantity INT NOT NULL DEFAULT 1,

    status ENUM('active', 'temporarily_unavailable', 'cancelled')
        NOT NULL DEFAULT 'active',

    -- midlertidig utilgjengelig, jf brukerhistorie om utstyr som er opptatt
    unavailable_reason VARCHAR(255) NULL,
    unavailable_until DATETIME NULL,

    -- for offline registrering / synk når nett er nede
    is_draft BOOLEAN NOT NULL DEFAULT FALSE,
    synced_at DATETIME NULL,

    registered_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    FOREIGN KEY (civilian_provider_id) REFERENCES civilian_provider(user_account_id) ON DELETE CASCADE,
    FOREIGN KEY (category_id) REFERENCES category(id),
    FOREIGN KEY (location_id) REFERENCES geographic_location(id)
);

-- egen tabell, ikke bare kobling - holder egen status/data (se argumentasjon i rapporten)
CREATE TABLE resource_match (
    id INT AUTO_INCREMENT PRIMARY KEY,
    resource_need_id INT NOT NULL,
    civilian_resource_id INT NOT NULL,

    status ENUM('found', 'approved', 'allocated', 'completed', 'freed')
        NOT NULL DEFAULT 'found',

    approved_by INT NULL,

    matched_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    approved_at DATETIME NULL,
    completed_at DATETIME NULL,

    FOREIGN KEY (resource_need_id) REFERENCES resource_need(id) ON DELETE CASCADE,
    FOREIGN KEY (civilian_resource_id) REFERENCES civilian_resource(id) ON DELETE CASCADE,
    FOREIGN KEY (approved_by) REFERENCES public_sector_entity(user_account_id)
);

CREATE TABLE incident_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    crisis_event_id INT NULL,
    resource_need_id INT NULL,
    civilian_resource_id INT NULL,
    resource_match_id INT NULL,
    actor_user_id INT NULL,

    event_type ENUM(
        'resource_registered', 'resource_allocated', 'resource_cancelled',
        'resource_freed', 'resource_updated',
        'need_registered', 'need_cancelled', 'need_completed', 'need_updated',
        'match_found', 'match_approved',
        'crisis_registered'
    ) NOT NULL,

    description TEXT NULL,
    logged_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (crisis_event_id) REFERENCES crisis_event(id) ON DELETE SET NULL,
    FOREIGN KEY (resource_need_id) REFERENCES resource_need(id) ON DELETE SET NULL,
    FOREIGN KEY (civilian_resource_id) REFERENCES civilian_resource(id) ON DELETE SET NULL,
    FOREIGN KEY (resource_match_id) REFERENCES resource_match(id) ON DELETE SET NULL,
    FOREIGN KEY (actor_user_id) REFERENCES user_account(id) ON DELETE SET NULL
);

CREATE TABLE message_alert (
    id INT AUTO_INCREMENT PRIMARY KEY,
    recipient_user_id INT NOT NULL,
    crisis_event_id INT NULL,
    resource_need_id INT NULL,
    resource_match_id INT NULL,

    message TEXT NOT NULL,
    alert_type ENUM('info', 'warning', 'critical') NOT NULL DEFAULT 'info',

    sent_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    read_at DATETIME NULL,

    FOREIGN KEY (recipient_user_id) REFERENCES user_account(id) ON DELETE CASCADE,
    FOREIGN KEY (crisis_event_id) REFERENCES crisis_event(id) ON DELETE CASCADE,
    FOREIGN KEY (resource_need_id) REFERENCES resource_need(id) ON DELETE CASCADE,
    FOREIGN KEY (resource_match_id) REFERENCES resource_match(id) ON DELETE CASCADE
);

SET FOREIGN_KEY_CHECKS = 1;

-- kategorier fra casebeskrivelsen
INSERT INTO category (name) VALUES
    ('Transport'), ('Droneobservasjon'), ('Strøm/aggregat'),
    ('Snørydding'), ('Sand/grus'), ('Maskiner'), ('Evakuering'),
    ('Samband'), ('Mannskap'), ('Lokaler'), ('Shelter');

CREATE INDEX idx_resource_location ON civilian_resource(location_id);
CREATE INDEX idx_need_location ON resource_need(location_id);
CREATE INDEX idx_crisis_location ON crisis_event(location_id);
