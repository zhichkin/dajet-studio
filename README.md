# DaJet Studio <a href="https://hub.docker.com/r/zhichkin/dajet-studio"><img width="32" height="32" alt="docker-logo" src="https://github.com/user-attachments/assets/e41122f3-8aae-4ea0-9bb3-289b874b5c4c" /></a>

Пользовательский интерфейс для [DaJet HTTP Server](https://github.com/zhichkin/dajet-http-server).

Приложение может быть установлено локально как PWA (Progressive Web App) и в дальнейшем работать автономно.

### Установка и запуск на Windows или Linux

1. Установить [Microsoft .NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
2. Скачать дистрибутив [DaJet Studio](https://github.com/zhichkin/dajet-studio/releases/latest)
3. Создать рабочий каталог и распаковать в него дистрибутив, например: ```C:\dajet-studio```
4. Перейти в каталог установки и запустить исполняемый файл ```dajet-studio.exe```
5. Открыть браузер и перейти по адресу ```http://localhost:5555```

### Установка и запуск в Docker

1. Получить образ из Docker Hub

```
docker pull zhichkin/dajet-studio
```

2. Запустить контейнер в Docker

```
docker run --name dajet-studio --user=root -it -p 5555:5555 zhichkin/dajet-studio
```

3. Открыть браузер и перейти по адресу ```http://localhost:5555```
