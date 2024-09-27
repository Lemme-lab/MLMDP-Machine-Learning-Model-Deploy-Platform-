import os
import tensorflow as tf
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import numpy as np

app = FastAPI()

# Fetch the model name and directory from environment variables
model_name = os.getenv("MODEL_NAME")
model_dir = os.getenv("MODEL_DIR", "/app/models")  # Default path if not set
model = None

@app.on_event("startup")
async def load_model():
    global model
    model_path = os.path.join(model_dir, "model.h5")  # Use model.h5 for consistency

    if os.path.exists(model_path):
        model = tf.keras.models.load_model(model_path)
        print(f"Model {model_name} loaded successfully from {model_dir}.")
    else:
        print(f"Model {model_name} not found in {model_dir}.")
        raise HTTPException(status_code=404, detail="Model file not found.")

class InputData(BaseModel):  
    features: list

@app.post("/predict/")
async def predict(data: InputData):
    global model

    if model is None:
        raise HTTPException(status_code=400, detail="Model not loaded.")
    
    try:
        input_array = np.array([data.features])
        prediction = model.predict(input_array)
        return {"prediction": prediction.tolist()}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
