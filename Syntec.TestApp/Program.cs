using System;
using System.Threading;
using SyntecRemote; // Подключение библиотеки Syntec

namespace Syntec.TestApp
{

    class Program
    {
        private static SyntecRemoteObj _remoteCnc;
        private static bool _isRunning = true;

        static void Main(string[] args)
        {
            Console.WriteLine("Программа для удаленного управления ЧПУ Syntec");
            Console.WriteLine("---------------------------------------------");

            // Настройки подключения (IP и таймаут)
            string cncIp = "127.0.0.1"; // Заменить на реальный IP ЧПУ
            uint timeout = 5000;        // Таймаут в миллисекундах

            try
            {
                // Подключение к ЧПУ
                _remoteCnc = new SyntecRemoteObj(cncIp, timeout);
                bool isConnected = _remoteCnc.Connect();

                if (!isConnected)
                {
                    Console.WriteLine("Ошибка подключения к ЧПУ!");
                    return;
                }

                Console.WriteLine("Подключение к ЧПУ успешно!");

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
                _remoteCnc?.DisConnect();
            }
        }

        // Вывод основной информации о ЧПУ
        private static void PrintCncInfo()
        {
            Console.WriteLine("\n=== Информация о ЧПУ ===");
            Console.WriteLine($"Модель: {_remoteCnc.CncModel}");
            Console.WriteLine($"Серийный номер: {_remoteCnc.SeriesNo}");
            Console.WriteLine($"Версия ПО: {_remoteCnc.CNCVer}");
            Console.WriteLine($"Тип машины: {_remoteCnc.MachineType}");
            Console.WriteLine($"Текущий статус: {_remoteCnc.Status}");
            Console.WriteLine($"Текущий режим: {_remoteCnc.Mode}");
            Console.WriteLine($"Текущая программа: {_remoteCnc.CurProg}");
            Console.WriteLine($"Текущая строка: {_remoteCnc.CurrentLineNo}");
        }

        // Периодический опрос координат
        private static void PollCoordinates()
        {
            while (_isRunning)
            {
                try
                {
                    // Получение текущих координат
                    float[] machineCoords = _remoteCnc.MachineCoordinate;
                    float[] absoluteCoords = _remoteCnc.AbsoluteCoordinate;
                    float[] relativeCoords = _remoteCnc.RelativeCoordinate;

                    Console.WriteLine("\n=== Текущие координаты ===");
                    Console.WriteLine("Машинные координаты: " + string.Join(", ", machineCoords));
                    Console.WriteLine("Абсолютные координаты: " + string.Join(", ", absoluteCoords));
                    Console.WriteLine("Относительные координаты: " + string.Join(", ", relativeCoords));

                    // Пауза между опросами (0.5 секунды)
                    Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при опросе координат: {ex.Message}");
                    Thread.Sleep(5000); // Пауза перед повторной попыткой
                }
            }
        }
    }
}
