# Syntec.TestApp.WPF
# Тестовое задание для Модульные строительные системы

CNC Syntec Monitoring System
Мониторинг станков с ЧПУ Syntec через интерфейс WPF с реактивным программированием.

## 📌 Особенности

- Реальное время: Мониторинг координат, статуса и параметров ЧПУ
- Реактивный UI: Автоматическое обновление данных (ReactiveUI)
- Современный интерфейс: Стилизация MahApps.Metro
- Безопасность: Корректное управление подключением
- Модульность: Разделение на компоненты (MVVM)

## 🛠 Технологии

- .NET Framework 4.8
- WPF + MVVM
- [ReactiveUI](https://www.reactiveui.net/) 20.2.45
- [MahApps.Metro](https://mahapps.com/) 2.4.10
- [Syntec Remote API](https://www.syntecclub.com/) (официальная библиотека)

## ⚙️ Установка

1. **Предварительные требования**:
   - Windows 10+
   - .NET Framework 4.8
   - Syntec CNC Controller (эмулятор или физическое устройство)
   - OCAPI.dll, OCKrnl.dll, OCKrnlDrv.dll, OCUser.dll в папке с исполняемым файлом

2. **Сборка**:
   ```powershell
   git clone https://github.com/JMatveichik/Syntec.TestApp.git
  
