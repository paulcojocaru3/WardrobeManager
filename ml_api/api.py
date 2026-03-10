import json
import numpy as np
from io import BytesIO
from PIL import Image
import tensorflow as tf
from fastapi import FastAPI, File, UploadFile
from rembg import remove
import base64

app = FastAPI(title="ML API")

MODEL_PATH = "fashion_my_model.h5"
CLASSES_PATH = "classes.json"
IMG_SIZE = (224, 224)
UPSCALE_SIZE = (1024, 1024)

model = tf.keras.models.load_model(MODEL_PATH)
with open(CLASSES_PATH, "r") as f:
    CLASSES = json.load(f)

def preprocess(img: Image.Image) -> np.ndarray:
    img_resized = img.resize(IMG_SIZE)
    x = np.array(img_resized, dtype=np.float32)[None, ...]
    x = tf.keras.applications.resnet50.preprocess_input(x)
    return x

@app.post("/process-clothing")
async def process_clothing(file: UploadFile = File(...)):
    img_bytes = await file.read()
    original_img = Image.open(BytesIO(img_bytes)).convert("RGB")

    # 1. Remove Background
    processed_img_bytes = remove(img_bytes)
    processed_img = Image.open(BytesIO(processed_img_bytes)).convert("RGBA")

    # 2. Upscale
    upscaled_img = processed_img.resize(UPSCALE_SIZE, Image.Resampling.LANCZOS)

    # 3. Predict Categories (Type and Color)
    x = preprocess(original_img)
    probs = model.predict(x, verbose=0)[0]
    
    # Gasim cel mai bun Type (cele care incep cu type_)
    type_indices = [i for i, label in enumerate(CLASSES) if label.startswith("type_")]
    best_type_idx = type_indices[np.argmax(probs[type_indices])]
    best_type = CLASSES[best_type_idx].replace("type_", "")

    # Gasim cel mai bun Color (cele care incep cu color_)
    color_indices = [i for i, label in enumerate(CLASSES) if label.startswith("color_")]
    best_color_idx = color_indices[np.argmax(probs[color_indices])]
    best_color = CLASSES[best_color_idx].replace("color_", "")
    
    # 4. Convert back to base64
    buffered = BytesIO()
    upscaled_img.save(buffered, format="PNG")
    img_str = base64.b64encode(buffered.getvalue()).decode()

    return {
        "type": best_type,
        "color": best_color,
        "processed_image_b64": img_str
    }

@app.post("/predict")
async def predict(file: UploadFile = File(...), threshold: float = 0.35, top_k: int = 12):
    img_bytes = await file.read()
    img = Image.open(BytesIO(img_bytes)).convert("RGB")
    x = preprocess(img)
    probs = model.predict(x, verbose=0)[0]
    idx = np.argsort(probs)[::-1][:top_k]
    return {
        "top": [
            {"label": CLASSES[i], "score": float(probs[i]), "passed": bool(probs[i] >= threshold)}
            for i in idx
        ]
    }