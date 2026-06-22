import json
import numpy as np
from io import BytesIO
from PIL import Image
from fastapi import FastAPI, File, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional
from rembg import remove
import base64
import torch
import torch.nn.functional as F
from transformers import CLIPProcessor, CLIPModel, pipeline as hf_pipeline
import joblib
import os

app = FastAPI(title="Fashion AI API")

MAX_UPLOAD_BYTES = int(os.getenv("MAX_UPLOAD_BYTES", str(10 * 1024 * 1024)))
ALLOWED_IMAGE_TYPES = {"image/jpeg", "image/png", "image/webp"}
Image.MAX_IMAGE_PIXELS = int(os.getenv("MAX_IMAGE_PIXELS", "20000000"))
cors_origins = [
    origin.strip()
    for origin in os.getenv("CORS_ALLOWED_ORIGINS", "http://localhost:5150,http://localhost:8080").split(",")
    if origin.strip()
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=cors_origins,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── Zero-shot prompt classifier (lazy-loaded on first request) ──────────────
_zs_classifier = None

def get_zs_classifier():
    global _zs_classifier
    if _zs_classifier is None:
        print("Loading zero-shot classifier (MoritzLaurer/mDeBERTa-v3-base-mnli-xnli)...")
        _zs_classifier = hf_pipeline(
            "zero-shot-classification",
            model="MoritzLaurer/mDeBERTa-v3-base-mnli-xnli",
            device=-1,  # CPU
        )
        print("Zero-shot classifier ready.")
    return _zs_classifier

# Descriptive labels so the multilingual model matches semantics, not just words
STYLE_LABEL_MAP = {
    "formal elegant outfit for a wedding, ceremony, gala or business meeting": "Formal",
    "smart casual outfit for office, work, dinner or date":                    "Smart Casual",
    "casual relaxed outfit for everyday, weekend, park or errands":            "Casual",
    "party or nightlife outfit for a club, birthday or celebration":           "Party",
    "sporty athletic outfit for gym, hiking, running or outdoor activity":     "Sports",
    "comfortable travel outfit for a flight, trip or vacation":                "Travel",
}
STYLE_LABELS    = list(STYLE_LABEL_MAP.keys())
STYLE_HYPOTHESIS = "The person needs {}."

KNOWN_CITIES = [
    "bucharest", "cluj", "cluj-napoca", "timisoara", "iasi", "constanta", "brasov",
    "sibiu", "craiova", "galati", "ploiesti", "oradea", "pitesti", "arad", "targu mures",
    "london", "paris", "berlin", "rome", "madrid", "amsterdam", "vienna", "prague",
    "budapest", "warsaw", "athens", "lisbon", "barcelona", "milan", "brussels", "zurich",
    "geneva", "stockholm", "oslo", "copenhagen", "dublin", "edinburgh", "istanbul",
    "porto", "florence", "venice", "munich", "hamburg", "dubai", "abu dhabi",
    "new york", "los angeles", "chicago", "toronto", "montreal", "sydney", "melbourne",
    "singapore", "tokyo", "bangkok", "seoul", "beijing", "shanghai", "mumbai",
    "miami", "san francisco", "boston", "cape town",
]

class PromptParseRequest(BaseModel):
    prompt: str

@app.post("/parse-prompt")
async def parse_prompt(request: PromptParseRequest):
    prompt = request.prompt.strip()
    if not prompt:
        return {"style": "Casual", "style_confidence": 0.0, "city": None}

    classifier = get_zs_classifier()
    result = classifier(prompt, candidate_labels=STYLE_LABELS, hypothesis_template=STYLE_HYPOTHESIS)
    top_label = result["labels"][0]
    style = STYLE_LABEL_MAP[top_label]
    confidence = round(float(result["scores"][0]), 3)

    lower = prompt.lower()
    city: Optional[str] = None
    for c in KNOWN_CITIES:
        if c in lower:
            city = " ".join(w.capitalize() for w in c.split("-" if "-" in c else " "))
            break

    return {"style": style, "style_confidence": confidence, "city": city}

# Parametri imagine
UPSCALE_SIZE = (1024, 1024)
CLIP_MODEL_NAME = "patrickjohncyh/fashion-clip"
device = "cuda" if torch.cuda.is_available() else "cpu"

print(f"Loading FashionCLIP model: {CLIP_MODEL_NAME} on {device}...")
clip_model = CLIPModel.from_pretrained(CLIP_MODEL_NAME).to(device)
clip_processor = CLIPProcessor.from_pretrained(CLIP_MODEL_NAME)

# Incarcare modele Logistic Regression
MODELS_DIR = "models"
print("Loading specialized fashion models...")
article_type_model = joblib.load(os.path.join(MODELS_DIR, "articleType_fashion_model.joblib"))
gender_model = joblib.load(os.path.join(MODELS_DIR, "gender_fashion_model.joblib"))
season_model = joblib.load(os.path.join(MODELS_DIR, "season_fashion_model.joblib"))
usage_model = joblib.load(os.path.join(MODELS_DIR, "usage_fashion_model.joblib"))

# Incarcare culori pentru Zero-Shot: taxonomie {main: {hex, shades}} -> lista plata de nume.
with open("colors.json", "r") as f:
    _color_groups = json.load(f)
COLORS = [name for main, group in _color_groups.items() for name in (main, *group["shades"].keys())]

COLOR_PROMPTS = [f"a photo of a {c} colored clothing item" for c in COLORS]

def extract_tensor(outputs):
    if torch.is_tensor(outputs):
        return outputs
    for attr in ["image_embeds", "text_embeds", "pooler_output", "last_hidden_state"]:
        val = getattr(outputs, attr, None)
        if val is not None and torch.is_tensor(val):
            return val
    if hasattr(outputs, "logits") and torch.is_tensor(outputs.logits):
        return outputs.logits
    if isinstance(outputs, (dict, list)) and len(outputs) > 0:
        return outputs[0] if torch.is_tensor(outputs[0]) else outputs
    return outputs

# Pre-calculam embedding-urile de text pentru culori
print(f"Pre-calculating text embeddings for {len(COLORS)} colors...")
with torch.no_grad():
    text_inputs = clip_processor(text=COLOR_PROMPTS, return_tensors="pt", padding=True).to(device)
    raw_outputs = clip_model.get_text_features(**text_inputs)
    text_features = extract_tensor(raw_outputs)
    text_features = F.normalize(text_features, p=2, dim=-1)

@app.post("/process-clothing")
async def process_clothing(file: UploadFile = File(...)):
    if file.content_type not in ALLOWED_IMAGE_TYPES:
        raise HTTPException(status_code=415, detail="Only JPEG, PNG, and WebP images are supported.")

    img_bytes = await file.read()
    if not img_bytes:
        raise HTTPException(status_code=400, detail="A non-empty image file is required.")
    if len(img_bytes) > MAX_UPLOAD_BYTES:
        raise HTTPException(status_code=413, detail="Image file is too large.")

    original_img = Image.open(BytesIO(img_bytes)).convert("RGB")

    # 1. Remove Background
    processed_img_bytes = remove(img_bytes)
    processed_img = Image.open(BytesIO(processed_img_bytes)).convert("RGBA")

    # 2. Clean & High-Quality Resize for UI (maintain aspect ratio)
    ui_img = processed_img.copy()
    ui_img.thumbnail((800, 800), Image.Resampling.LANCZOS)
    
    buffered = BytesIO()
    ui_img.save(buffered, format="PNG", quality=95)
    img_str = base64.b64encode(buffered.getvalue()).decode()

    # 3. Generate Image Embedding (using original image for best CLIP results)
    inputs = clip_processor(images=original_img, return_tensors="pt").to(device)
    with torch.no_grad():
        raw_outputs = clip_model.get_image_features(**inputs)
        image_features = extract_tensor(raw_outputs)
        image_features = F.normalize(image_features, p=2, dim=-1)
        embedding = image_features.cpu().numpy().tolist()[0]
    
    # 4. Predict Properties (using the embedding)
    emb_np = np.array([embedding])
    predicted_article_type = article_type_model.predict(emb_np)[0]
    predicted_gender = gender_model.predict(emb_np)[0]
    predicted_season = season_model.predict(emb_np)[0]
    predicted_usage = usage_model.predict(emb_np)[0]

    # 5. Zero-Shot Color Classification
    with torch.no_grad():
        similarities = (image_features @ text_features.T)
        best_color_idx = similarities.argmax().item()
        best_color = COLORS[best_color_idx]

    return {
        "type": str(predicted_article_type),
        "gender": str(predicted_gender),
        "season": str(predicted_season),
        "usage": str(predicted_usage),
        "color": best_color,
        "processed_image_b64": img_str,
        "embedding": embedding
    }

class EmbedTextRequest(BaseModel):
    text: str

@app.post("/embed-text")
async def embed_text(request: EmbedTextRequest):
    text = (request.text or "").strip()
    if not text:
        return {"embedding": []}

    with torch.no_grad():
        inputs = clip_processor(text=[text], return_tensors="pt", padding=True).to(device)
        raw_outputs = clip_model.get_text_features(**inputs)
        feats = extract_tensor(raw_outputs)
        feats = F.normalize(feats, p=2, dim=-1)
        embedding = feats.cpu().numpy().tolist()[0]

    return {"embedding": embedding}

class PredictArticleTypesRequest(BaseModel):
    embeddings: list  # list of CLIP embedding vectors (already stored per item)

@app.post("/predict-article-types")
async def predict_article_types(request: PredictArticleTypesRequest):
    # Backfill helper: derive the fine article type from stored embeddings (no image needed).
    if not request.embeddings:
        return {"types": []}
    emb_np = np.array(request.embeddings)
    preds = article_type_model.predict(emb_np)
    return {"types": [str(p) for p in preds]}

@app.get("/article-types")
async def article_types():
    # The full article-type vocabulary the model can output (source of truth for the UI dropdown).
    return {"types": [str(c) for c in article_type_model.classes_]}

@app.get("/health")
async def health():
    return {"status": "ready", "model": CLIP_MODEL_NAME}
