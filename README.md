# Sistem pentru Gestionarea Garderobei Digitale si Recomandari Personalizate bazate pe Inteligenta Artificiala

Versiune stabila: **1.0.0**

Acest proiect reprezinta o solutie software integrata destinata digitalizarii pieselor vestimentare si optimizarii procesului de selectie a tinutelor prin utilizarea algoritmilor de invatare automata si cautare vectoriala.

## Obiectivele Proiectului
Obiectivul principal al aplicatiei este de a oferi utilizatorilor un instrument eficient de organizare a garderobei proprii. Sistemul automatizeaza clasificarea articolelor vestimentare si genereaza recomandari personalizate (outfits) bazate pe coerenta vizuala si contextul utilizarii (sezon, gen, tipul ocaziei).

## Arhitectura Tehnica

Sistemul este dezvoltat utilizand o arhitectura modulara, compusa din trei servicii distincte care comunica prin protocoale standard (HTTP/REST):

### 1. Serviciul Backend (Core API)
*   **Tehnologie:** .NET 10 (C#)
*   **Structura:** Arhitectura stratificata (Clean Architecture) implementand modelul CQRS prin intermediul bibliotecii MediatR.
*   **Persistenta:** PostgreSQL cu extensia `pgvector` pentru stocarea si interogarea embedding-urilor vectoriale.
*   **Validare:** Implementarea regulilor de business prin FluentValidation.

### 2. Interfata Utilizator (Frontend)
*   **Tehnologie:** React.js (TypeScript/JavaScript) cu procesor de build Vite.
*   **Design:** Interfata Reactiva, optimizata pentru o experienta utilizator fluida, utilizand standarde moderne de CSS.

### 3. Serviciul de Inteligenta Artificiala (ML Service)
*   **Tehnologie:** Python (FastAPI).
*   **Modele:** Integrarea modelului Fashion-CLIP pentru extragerea caracteristicilor vizuale si modele scikit-learn pentru clasificarea atributelor (article type, color, gender, season, usage).
*   **Procesare:** Extragerea embedding-urilor necesare cautarii prin similaritate cosinusoidala in baza de date.

## Organizarea Proiectului

Structura radacina a repository-ului este divizata dupa cum urmeaza:

*   `src/WardrobeManager/` - Contine solutia principala .NET, divizata in proiecte de Domain, Application, Infrastructure si API.
*   `wardrobe_web/` - Contine codul sursa al aplicatiei web React.
*   `ml_api/` - Contine implementarea serviciului Python si modelele pre-antrenate.

## Flux de Functionare

1.  **Digitizare:** Incarcarea unei imagini de catre utilizator este preluata de API-ul principal.
2.  **Analiza Automata:** Serviciul ML proceseaza imaginea, elimina fundalul si extrage metadatele (atributele) si vectorul de caracteristici.
3.  **Indexare:** Datele sunt stocate in PostgreSQL, unde vectorul de embedding permite cautari ultra-rapide prin similaritate.
4.  **Generare Tinute:** Sistemul utilizeaza cautarea vectoriala pentru a gasi piese vestimentare complementare celor selectate, formand un ansamblu coerent.

## Instructiuni de Configurare si Rulare

### Pornire completa cu Docker (recomandat)

Sunt necesare Docker Engine si Docker Compose v2.

```bash
cp .env.example .env
# Completeaza POSTGRES_PASSWORD si JWT_KEY in .env
docker compose up -d --build
```

Aplicatia este disponibila implicit la `http://localhost`. Prima pornire a serviciului ML poate dura cateva minute deoarece modelele sunt descarcate si memorate intr-un volum persistent.

```bash
docker compose ps
docker compose logs -f wardrobe-api ml-api
```

Configurarea completa pentru un host de productie, HTTPS, actualizari si backup este descrisa in [DEPLOYMENT.md](DEPLOYMENT.md).

### Cerinte Preliminare
*   .NET SDK 10.0+
*   Node.js 24+
*   Python 3.12+
*   Instanta PostgreSQL configurata cu extensia `pgvector`

### Instalare si Pornire

#### Serviciul Machine Learning
```bash
cd ml_api
pip install -r requirements.txt
uvicorn api:app --host 0.0.0.0 --port 8000
```

#### Serviciul Backend
```bash
cd src/WardrobeManager/WardrobeManager.API
dotnet restore
dotnet run
```

#### Aplicatia Frontend
```bash
cd wardrobe_web
npm ci
npm run dev
```

## Verificari de calitate

```bash
dotnet test src/WardrobeManager/WardrobeManager.sln -c Release
npm --prefix wardrobe_web run lint
npm --prefix wardrobe_web run build
python -m compileall -q ml_api/api.py
```

Workflow-ul GitHub Actions din `.github/workflows/ci.yml` ruleaza aceleasi verificari la fiecare push si pull request pe `master`.

## Publicarea versiunii finale pe GitHub

Inainte de publicare, verifica in mod explicit lista fisierelor incluse:

```bash
git status
git diff --check
git add -A
git status
git commit -m "Prepare v1.0.0 release"
git tag -a v1.0.0 -m "WardrobeManager v1.0.0"
git push origin master
git push origin v1.0.0
```

Nu comite fisierul `.env`, chei API, parole, backup-uri de baza de date sau date Kaggle. Regulile din `.gitignore` acopera aceste fisiere, dar lista staged trebuie verificata inainte de commit.
