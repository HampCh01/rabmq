FROM python:slim

WORKDIR /app
COPY spider.py /app

ENTRYPOINT ["python", "spider.py"]