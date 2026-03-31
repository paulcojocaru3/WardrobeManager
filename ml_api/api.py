import json
import numpy as np
from io import BytesIO
from PIL import Image
from fastapi import FastAPI, File, UploadFile
from rembg import remove
import base64
import torch
import torch.nn.functional as F
from transformers import CLIPProcessor, CLIPModel
import joblib
import os

app = FastAPI(title="Fashion AI API")

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

# Incarcare culori pentru Zero-Shot
with open("colors.json", "r") as f:
    COLORS = json.load(f)

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
    img_bytes = await file.read()
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

@app.get("/health")
async def health():
    return {"status": "ready", "model": CLIP_MODEL_NAME}
