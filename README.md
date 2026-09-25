# H-st-2026-Gruppe-1

Felles repository for gruppe 1 i IS-20X



Database (MariaDB via Docker)



Forutsetter at Docker Desktop er installert og kjører.



Start containeren:

&#x20;  docker run --name hv-mariadb -e MARIADB\_ROOT\_PASSWORD=root -e MARIADB\_DATABASE=heimevernet -p 3306:3306 -d mariadb:11

Vent til den er klar (sjekk med docker logs hv-mariadb, se etter "ready for connections").

Importer skjemaet:

&#x20;  docker cp database/schema.sql hv-mariadb:/schema.sql

&#x20;  docker exec -it hv-mariadb mariadb -uroot -proot heimevernet --default-character-set=utf8mb4 -e "SOURCE /schema.sql"

Verifiser at tabellene ble opprettet:

&#x20;  docker exec -it hv-mariadb mariadb -uroot -proot heimevernet -e "SHOW TABLES;"



Du skal se 11 tabeller: category, civilian\_provider, civilian\_resource, crisis\_event, geographic\_location, incident\_log, message\_alert, public\_sector\_entity, resource\_match, resource\_need, user\_account.



Connection string (for ASP.NET Core)

Server=localhost;Port=3306;Database=heimevernet;User=root;Password=root;

Status



Førsteutkast av databaseskjema. Se database/schema.sql. 

