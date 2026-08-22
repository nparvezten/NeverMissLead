FROM python:3.11-slim

WORKDIR /app

# Install CPU PyTorch first to prevent downloading multi-gigabyte CUDA/GPU wheels
RUN pip install --no-cache-dir --extra-index-url https://download.pytorch.org/whl/cpu torch==2.6.0

COPY requirements.txt .
RUN pip install --no-cache-dir --extra-index-url https://download.pytorch.org/whl/cpu -r requirements.txt

COPY . .

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
