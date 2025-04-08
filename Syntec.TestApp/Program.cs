using System;
using System.Diagnostics;
using System.Threading;
using Syntec.OpenCNC;
using Syntec.Remote;
using SyntecRemote; // Подключение библиотеки Syntec

namespace Syntec.TestApp
{

    class Program
    {
        private static SyntecRemoteCNC _remoteCnc;
        private static bool _isRunning = true;

        static void Main(string[] args)
        {
            Console.WriteLine("Тестовое подключение к ЧПУ Syntec");
            Console.WriteLine("---------------------------------------------");

            // Настройки подключения (IP и таймаут)
            string host  = "127.0.0.1"; // Заменить на реальный IP сервера
            int timeout = 5000;         // Таймаут в миллисекундах

            try
            {
                // Подключение к ЧПУ
                _remoteCnc = new SyntecRemoteCNC(host, timeout);
                bool isConnected = _remoteCnc.isConnected();
               
                if (!isConnected)
                {
                    Console.WriteLine("Ошибка подключения к ЧПУ!");
                    return;
                }

                Console.WriteLine("Удаленное подключение к ЧПУ успешно!");

                // Вывод основной информации
                PrintCncInfo();

                // Запуск периодического опроса координат
                Console.WriteLine("\nЗапуск опроса координат... (нажмите любую клавишу для остановки)");
                Thread pollingThread = new Thread(PollCoordinates);
                pollingThread.Start();

                // Ожидание нажатия клавиши для остановки
                Console.ReadKey();
                _isRunning = false;
                pollingThread.Join();

                Console.WriteLine("Программа завершена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                _remoteCnc?.Dispose();
            }
        }

        // Вывод основной информации о ЧПУ
        private static void PrintCncInfo()
        {
            Console.WriteLine("\n[DEBUG] Начало получения информации о ЧПУ...");

            short Axes = 0;
            string CncType = "UNKNOWN";
            short MaxAxes = 0;
            string Series = "UNKNOWN";
            string Nc_Ver = "UNKNOWN";
            string[] AxisName = null;

            Console.WriteLine("[DEBUG] Запрос общей информации о ЧПУ...");
            short res = _remoteCnc.READ_information(out Axes, out CncType, out MaxAxes, out Series, out Nc_Ver, out AxisName);

            Console.WriteLine($"[DEBUG] Код ответа READ_information: {res}");

            if (res != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
            {
                Console.WriteLine($"[ERROR] Ошибка получения информации о ЧПУ. Код: {res}");
                throw new Exception($"Ошибка получения информации о ЧПУ. Код ошибки: {res}");
            }

            Console.WriteLine("\n=== Общая информация о ЧПУ ===");
            Console.WriteLine($"• Число активных осей: {Axes}");
            Console.WriteLine($"• Тип ЧПУ:             {CncType}");
            Console.WriteLine($"• Макс. число осей:    {MaxAxes}");
            Console.WriteLine($"• Серия:               {Series}");
            Console.WriteLine($"• Версия ПО:           {Nc_Ver}");

            if (AxisName != null)
            {
                Console.WriteLine($"\n[DEBUG] Список имен осей ({AxisName.Length}):");
                for (int i = 0; i < AxisName.Length; i++)
                {
                    Console.WriteLine($"  Ось {i + 1}: {AxisName[i]}");
                }
            }
            else
            {
                Console.WriteLine("[WARNING] Информация об именах осей недоступна");
            }

            Console.WriteLine("\n[DEBUG] Запрос статуса ЧПУ...");
            string MainProg;
            string CurProg;
            int CurSeq;
            string Mode;
            string Status;
            string Alarm;
            string EMG;

            res = _remoteCnc.READ_status(out MainProg, out CurProg, out CurSeq, out Mode, out Status, out Alarm, out EMG);
            Console.WriteLine($"[DEBUG] Код ответа READ_status: {res}");

            if (res != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
            {
                Console.WriteLine($"[ERROR] Ошибка получения статуса. Код: {res}");
                throw new Exception($"Ошибка получения статуса ЧПУ. Код ошибки: {res}");
            }

            Console.WriteLine("\n=== Текущий статус ЧПУ ===");
            Console.WriteLine($"• Главная программа:  {MainProg ?? "N/A"}");
            Console.WriteLine($"• Текущая программа:  {CurProg ?? "N/A"}");
            Console.WriteLine($"• Номер кадра:       {CurSeq}");
            Console.WriteLine($"• Режим работы:      {Mode ?? "N/A"}");
            Console.WriteLine($"• Состояние:         {Status ?? "N/A"}");
            Console.WriteLine($"• Аварии:            {Alarm ?? "N/A"}");
            Console.WriteLine($"• Аварийная остановка: {EMG ?? "N/A"}");

            Console.WriteLine("[DEBUG] Информация о ЧПУ успешно получена");
        }

        // Периодический опрос координат
        private static void PollCoordinates()
        {
            Console.WriteLine("[DEBUG] Запуск опроса координат...");
            Console.WriteLine($"[DEBUG] Режим работы: {(_isRunning ? "Активен" : "Остановлен")}");

            string[] AxisName = null;
            short DecPoint = 0;
            string[] Unit = null;
            float[] Mach = null;
            float[] Abs = null;
            float[] Rel = null;
            float[] Dist = null;

            int pollCount = 0;
            var stopwatch = Stopwatch.StartNew();

            while (_isRunning)
            {
                pollCount++;
                Console.WriteLine($"\n[DEBUG] Опрос #{pollCount} (Время работы: {stopwatch.Elapsed:mm\\:ss})");

                try
                {
                    Console.WriteLine("[DEBUG] Запрос координат...");
                    short res = _remoteCnc.READ_position(out AxisName, out DecPoint, out Unit, out Mach, out Abs, out Rel, out Dist);

                    Console.WriteLine($"[DEBUG] Код ответа: {res} ({(SyntecRemoteCNC.ErrorCode)res})");

                    if (res != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
                    {
                        Console.WriteLine($"[ERROR] Ошибка получения координат. Код: {res}");
                        throw new Exception($"Ошибка получения координат. Код ошибки: {res}");
                    }

                    Console.WriteLine("\n=== Текущие координаты ===");
                    Console.WriteLine($"Точность отображения: {DecPoint} знаков");

                    if (AxisName != null && AxisName.Length > 0)
                    {
                        Console.WriteLine("\n| Ось      | Машинные | Абсолютные | Относительные | Дистанция | Ед.изм |");
                        Console.WriteLine("|----------|----------|------------|---------------|-----------|--------|");

                        for (int i = 0; i < AxisName.Length; i++)
                        {
                            Console.WriteLine($"| {AxisName[i],-8} | " +
                                            $"{Mach?[i].ToString($"F{DecPoint}"),-8} | " +
                                            $"{Abs?[i].ToString($"F{DecPoint}"),-10} | " +
                                            $"{Rel?[i].ToString($"F{DecPoint}"),-13} | " +
                                            $"{Dist?[i].ToString($"F{DecPoint}"),-9} | " +
                                            $"{Unit?[i],-6} |");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[WARNING] Информация об осях недоступна");
                    }

                    // Пауза между опросами
                    Console.WriteLine($"[DEBUG] Ожидание 1с перед следующим опросом...");
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Ошибка при опросе координат: {ex.Message}");
                    Console.WriteLine($"[DEBUG] StackTrace: {ex.StackTrace}");
                    Console.WriteLine("[DEBUG] Повторная попытка через 5 секунд...");
                    Thread.Sleep(5000);
                }
            }

            Console.WriteLine("[DEBUG] Опрос координат остановлен");
            stopwatch.Stop();
        }
    }
}
