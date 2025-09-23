// Path: Services/ToastService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using NetworkService.Views; // ← obavezno: namespace gde je ToastWindow.xaml/.cs

namespace NetworkService.Services
{
    public enum ToastType { Success, Info, Warning, Error }

    public static class ToastService
    {
        // Aktivni toasti (da ih pravilno rasporedimo)
        private static readonly List<ToastWindow> _active = new List<ToastWindow>();

        // Razmaci i pozicioniranje unutar MainWindow-a
        private const int VerticalGap = 8;   // razmak između toastova
        private const int RightOffset = 20;  // odmak od desne ivice MainWindow-a
        private const int TopOffset = 72;    // odmak od gornje ivice (ispod Undo/History)

        // Podrazumevano trajanje: 2.5s
        public static void Success(string message, int ms = 2500) => Show(message, ToastType.Success, ms);
        public static void Info   (string message, int ms = 2500) => Show(message, ToastType.Info,    ms);
        public static void Warning(string message, int ms = 2500) => Show(message, ToastType.Warning, ms);
        public static void Error  (string message, int ms = 2500) => Show(message, ToastType.Error,   ms);

        public static void Show(string message, ToastType type = ToastType.Info, int ms = 2500)
        {
            void ShowImpl()
            {
                var toast = new ToastWindow(message, type, ms);

                // Kad se zatvori, izbaci iz liste i prerasporedi
                toast.Closed += (_, __) =>
                {
                    _active.Remove(toast);
                    Reflow();
                };

                // Dodaj, prerasporedi i prikaži
                _active.Add(toast);
                Reflow();
                toast.Show();
            }

            var disp = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            if (disp.CheckAccess()) ShowImpl();
            else disp.Invoke(ShowImpl);
        }

        /// <summary>
        /// Raspored toasta: unutar MainWindow-a, desno gore, pa naniže.
        /// </summary>
        private static void Reflow()
        {
            var main = Application.Current?.MainWindow;

            // Ako nemamo MainWindow (teorijski), fallback je ekran (desno gore).
            if (main == null || !main.IsLoaded)
            {
                var wa = SystemParameters.WorkArea;
                double right = wa.Right - RightOffset;
                double currentTop = wa.Top + TopOffset;

                foreach (var t in _active.OrderByDescending(w => w.CreatedAtTicks))
                {
                    t.Left = right - t.Width;
                    t.Top = currentTop;
                    currentTop += t.Height + VerticalGap;
                }
                return;
            }

            // Unutar glavnog prozora:
            double rightInside = main.Left + main.Width - RightOffset; // desna ivica MainWindow-a (sa odmakom)
            double currentTopInside = main.Top + TopOffset;            // početni Y (ispod Undo/History)

            // Najnoviji ide gore (da se odmah vidi), stariji ispod njega
            foreach (var t in _active.OrderByDescending(w => w.CreatedAtTicks))
            {
                t.Left = rightInside - t.Width;     // poravnaj uz desnu ivicu prozora
                t.Top = currentTopInside;           // slaži nadole
                currentTopInside += t.Height + VerticalGap;
            }
        }
    }
}
