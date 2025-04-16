using ReactiveUI;
using System;
using System.ComponentModel;

namespace Syntec.TestApp.WPF.ViewModels
{
    public class Parameter : ReactiveObject
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        private Type _type;
        public Type Type
        {
            get => _type;
            set => this.RaiseAndSetIfChanged(ref _type, value);
        }

        private object _value;
        public object Value
        {
            get => _value;
            set
            {
                // Проверка типа при установке значения
                if (value != null && _type != null && !_type.IsInstanceOfType(value))
                {
                    throw new ArgumentException($"Значение типа {value.GetType()} не совместимо с ожидаемым типом {_type}");
                }
                this.RaiseAndSetIfChanged(ref _value, value);
            }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        // Конструкторы
        public Parameter() { }

        public Parameter(string name, Type type, object value = null, string description = "")
        {
            Name = name;
            Type = type;
            Value = value;
            Description = description;
        }

        // Метод для безопасной установки значения с преобразованием типа
        public bool TrySetValue(object value)
        {
            try
            {
                if (value != null && Type != null)
                {
                    Value = Convert.ChangeType(value, Type);
                    return true;
                }
                Value = value;
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Переопределение ToString для удобства отладки
        public override string ToString()
        {
            return $"{Name} ({Type?.Name}): {Value}";
        }
    }
}