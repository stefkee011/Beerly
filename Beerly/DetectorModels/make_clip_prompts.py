import json
import numpy as np
import onnxruntime as ort
from transformers import CLIPTokenizer


PROMPTS = {
    "beer_bottle": [
        "a photo of a beer bottle",
        "a brown glass bottle of beer with a label",
        "a green glass bottle of lager",
        "a close-up of a beer bottle label",
    ],
    "beer_can": [
        "a photo of a beer can",
        "an aluminum can of beer",
        "a can of lager beer",
        "a close-up of a beer can",
    ],
    "beer_glass_only": [
        "a photo of a glass of beer",
        "a pint of beer with foam",
        "a beer in a glass on a bar counter",
        "a glass of golden lager",
    ],
    "multiple_beers": [
        "a photo of several beer bottles",
        "a six-pack of beer cans",
        "a crate of beer bottles",
        "many bottles of beer on a shelf",
    ],
    "other_drink": [
        "a photo of a bottle of wine",
        "a glass of red wine",
        "a can of soda or cola",
        "a bottle of juice or water",
    ],
    "not_a_drink": [
        "a photo of an animal",
        "a photo of a person's face",
        "a landscape, building or car",
        "a screenshot, document or everyday object",
    ],
}

tokenizer = CLIPTokenizer.from_pretrained("openai/clip-vit-base-patch32")
session = ort.InferenceSession("clip-textual.onnx")

first_input = session.get_inputs()[0]
dtype = np.int32 if "int32" in first_input.type else np.int64

items = [(label, text) for label, texts in PROMPTS.items() for text in texts]
tokens = tokenizer(
    [text for _, text in items],
    padding="max_length",
    max_length=77,
    truncation=True,
    return_tensors="np",
)["input_ids"].astype(dtype)

embeddings = session.run(None, {first_input.name: tokens})[0]
assert embeddings.ndim == 2 and embeddings.shape[1] == 512, embeddings.shape
embeddings = embeddings / np.linalg.norm(embeddings, axis=1, keepdims=True)

with open("clip-prompts.json", "w") as f:
    json.dump(
        [
            {"class": label, "text": text, "embedding": embedding.tolist()}
            for (label, text), embedding in zip(items, embeddings)
        ],
        f,
    )

print("Wrote clip-prompts.json with", len(items), "prompts")