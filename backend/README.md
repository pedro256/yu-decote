# Rodar Docker
* Construir a imagem Docker
```yml
docker build -t yudecote-backend .
```
* Subir Imagem Docker
```
docker run -d -p 8000:8080 --name yudecote-backend-container yudecote-backend
```