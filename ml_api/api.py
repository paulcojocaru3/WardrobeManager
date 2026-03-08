import json
import numpy as np
from io import BytesIO
from PIL import Image
import tensorflow as tf
from fastapi import FastAPI, File, UploadFile

app = FastAPI(title="ML API")

MODEL_PATH = "fashion_my_model.h5"
CLASSES_PATH = "classes.json"
IMG_SIZE = (224, 224)

model = tf.keras.models.load_model(MODEL_PATH)
with open(CLASSES_PATH, "r") as f:
    CLASSES = json.load(f)

def preprocess(img_bytes: bytes) -> np.ndarray:
    img = Image.open(BytesIO(img_bytes)).convert("RGB")
    img = img.resize(IMG_SIZE)
    x = np.array(img, dtype=np.float32)[None, ...]
    x = tf.keras.applications.resnet50.preprocess_input(x)
    return x

@app.post("/predict")
async def predict(file: UploadFile = File(...), threshold: float = 0.35, top_k: int = 12):
    img_bytes = await file.read()
    x = preprocess(img_bytes)

    probs = model.predict(x, verbose=0)[0]
    idx = np.argsort(probs)[::-1][:top_k]

    return {
        "top": [
            {"label": CLASSES[i], "score": float(probs[i]), "passed": bool(probs[i] >= threshold)}
            for i in idx
        ]
    }