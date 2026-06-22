# Deployment

Configuratia de productie ruleaza patru containere:

- `wardrobe-frontend`: build React servit de Nginx, singurul serviciu expus public;
- `wardrobe-api`: API-ul .NET si hub-ul SignalR;
- `ml-api`: procesarea FashionCLIP/FastAPI;
- `postgres-db`: PostgreSQL 17 cu extensia pgvector.

Frontend-ul face proxy pentru `/api` si `/hubs`, astfel incat autentificarea si SignalR functioneaza same-origin. Baza de date nu publica niciun port pe host.

## Cerinte pentru host

- Linux x86-64 cu Docker Engine si Docker Compose v2;
- minimum 4 GB RAM; 8 GB recomandat pentru serviciul ML;
- minimum 20 GB spatiu liber pentru imagini si cache-urile modelelor;
- acces la internet la prima pornire, pentru descarcarea modelelor Hugging Face/rembg;
- un domeniu si HTTPS furnizat de platforma sau de un reverse proxy extern pentru productie publica.

Ollama este optional. Fara Ollama, clasificarea ML de baza functioneaza, dar imbogatirea Gemma si stylist-ul Gemma nu sunt disponibile. Stylist-ul este dezactivat implicit.

## Configurare

```bash
cp .env.example .env
openssl rand -base64 48
openssl rand -base64 32
```

Copiaza primul secret in `JWT_KEY`, al doilea in `POSTGRES_PASSWORD`, apoi seteaza:

- `APP_ORIGIN` la URL-ul public exact, fara slash final, de exemplu `https://wardrobe.example.com`;
- `APP_PORT` la portul ascultat pe host, implicit `80`;
- `WEATHER_API_KEY` daca functiile meteo trebuie activate;
- `OLLAMA_BASE_URL` si `OUTFIT_STYLIST_ENABLED=true` numai daca exista un server Ollama accesibil.

Fisierul `.env` este ignorat de Git si nu trebuie comis.

## Pornire si verificare

```bash
docker compose up -d --build
docker compose ps
docker compose logs --tail=100 wardrobe-api ml-api
curl --fail http://localhost:${APP_PORT:-80}/health
```

Prima pornire a `ml-api` poate dura cateva minute. Compose asteapta health check-ul serviciului inainte de a porni API-ul.

Pentru actualizare:

```bash
git pull --ff-only
docker compose up -d --build --remove-orphans
docker image prune -f
```

## HTTPS

Containerul Nginx asculta HTTP pe `APP_PORT`. Pentru un deploy public, termina TLS in platforma de hosting, Caddy, Traefik sau Nginx-ul hostului si transmite headerele `X-Forwarded-For` si `X-Forwarded-Proto`. API-ul proceseaza aceste headere, astfel incat cookie-ul de autentificare sa fie marcat `Secure` pe HTTPS.

## Backup PostgreSQL

```bash
docker compose exec -T postgres-db pg_dump \
  -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc > wardrobe-backup.dump
```

Restaurarea trebuie testata intr-un mediu separat inainte de a fi folosita pe productia activa.

## Oprire

```bash
docker compose down
```

Nu folosi `docker compose down -v` in productie: optiunea `-v` sterge baza de date si cache-urile persistente.
