# WardrobeManager

WardrobeManager este o aplicație web pentru organizarea garderobei și generarea de ținute personalizate. Poți încărca fotografii cu hainele tale, salva outfituri, planifica ținute pentru evenimente și primi recomandări adaptate vremii și preferințelor tale.

## Ce poți face

- adaugi și organizezi articole vestimentare;
- generezi outfituri din garderoba proprie;
- primești un Outfit of the Day;
- setezi culori favorite și culori de evitat;
- salvezi outfituri și înregistrezi când le porți;
- planifici ținute pentru călătorii și evenimente;
- activezi opțional Gemma3 pentru selecția finală a outfiturilor.

## Instalare recomandată

### Cerințe

Ai nevoie de:

- Windows, macOS sau Linux;
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) sau Docker Engine cu Docker Compose v2;
- minimum 8 GB RAM disponibili. Prima pornire poate necesita mai mult timp și spațiu pentru modelele ML.

Nu trebuie să instalezi separat PostgreSQL, .NET, Node.js sau Python.

### 1. Descarcă proiectul

Descarcă arhiva proiectului și extrage-o sau clonează repository-ul:

```bash
git clone https://github.com/paulcojocaru3/WardrobeManager.git
cd WardrobeManager
```

### 2. Creează configurația locală

Pe Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

Pe macOS sau Linux:

```bash
cp .env.example .env
```

Deschide fișierul `.env` și înlocuiește obligatoriu aceste valori:

```env
POSTGRES_PASSWORD=alege_o_parola_lunga
JWT_KEY=alege_o_cheie_aleatoare_de_minimum_32_de_caractere
```

Nu publica și nu trimite altor persoane fișierul `.env`.

### 3. Pornește aplicația

Din directorul proiectului rulează:

```bash
docker compose up -d --build
```

Verifică starea serviciilor:

```bash
docker compose ps
```

Când toate serviciile sunt `healthy`, deschide [http://localhost](http://localhost) și creează un cont.

Prima pornire poate dura câteva minute deoarece serviciul ML descarcă modelele necesare. Descărcările sunt păstrate în volume Docker și nu se repetă la fiecare pornire.

## Pornire și oprire

Pornește aplicația instalată:

```bash
docker compose up -d
```

Oprește aplicația fără să pierzi datele:

```bash
docker compose down
```

Datele garderobei rămân în volumul PostgreSQL.

> Nu folosi `docker compose down -v`. Opțiunea `-v` șterge volumele și baza de date.

## Actualizare fără pierderea bazei de date

Dacă ai instalat proiectul cu Git:

```bash
git pull
docker compose up -d --build --no-deps ml-api wardrobe-api wardrobe-frontend
```

Această comandă reconstruiește aplicația fără să recreeze serviciul PostgreSQL. Volumul bazei de date rămâne intact.

## Configurare opțională

### Alt port

Pentru a deschide aplicația pe alt port, modifică în `.env`:

```env
APP_PORT=8080
APP_ORIGIN=http://localhost:8080
```

Aplicația va fi disponibilă la `http://localhost:8080`.

### Prognoza meteo

Adaugă cheia serviciului meteo în `.env`:

```env
WEATHER_API_KEY=cheia_ta
```

### Gemma3 Stylist

Gemma3 este opțional. Aplicația poate genera outfituri și fără el.

1. Instalează [Ollama](https://ollama.com/).
2. Descarcă modelul:

```bash
ollama pull gemma3
```

3. Modifică `.env`:

```env
OLLAMA_BASE_URL=http://host.docker.internal:11434
OLLAMA_MODEL=gemma3
OUTFIT_STYLIST_ENABLED=true
```

4. Reconstruiește API-ul:

```bash
docker compose up -d --build --no-deps wardrobe-api
```

Activează apoi opțiunea Gemma3 din Settings. Culorile favorite, culorile de evitat și preferințele învățate sunt luate în calcul la generare.

## Probleme frecvente

### Aplicația nu se deschide

Verifică serviciile și logurile:

```bash
docker compose ps
docker compose logs --tail=100 wardrobe-frontend wardrobe-api ml-api
```

### Serviciul ML apare `starting`

La prima pornire, descărcarea și inițializarea modelelor poate dura câteva minute. Urmărește progresul:

```bash
docker compose logs -f ml-api
```

### Portul 80 este ocupat

Schimbă `APP_PORT` și `APP_ORIGIN` în `.env`, apoi rulează:

```bash
docker compose up -d
```

### Gemma3 nu răspunde

Verifică dacă Ollama rulează și modelul este instalat:

```bash
ollama list
```

Dezactivează temporar Gemma3 din Settings sau setează `OUTFIT_STYLIST_ENABLED=false` dacă vrei să folosești generatorul standard.

## Date și backup

Hainele, outfiturile, preferințele și conturile sunt păstrate în volumul Docker `postgres_data`. Imaginile și modelele descărcate sunt păstrate tot în volume Docker.

Pentru instalări publice, HTTPS, backup și restaurare consultă [DEPLOYMENT.md](DEPLOYMENT.md).
