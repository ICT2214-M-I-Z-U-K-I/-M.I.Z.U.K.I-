using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Mizuki
{
    [RequiresPreviewFeatures]
    /*
     * OLD CODE USED FOR SYNCHRONOUS WINDOWS FORM SPAWNING
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainPage());
        }
    }
    */
    internal class Program
    {
        private readonly MainPage mainPage;
        public event EventHandler<EventArgs> ExitRequested;
        void mainPage_FormClosed(object sender, FormClosedEventArgs e)
        {
            OnExitRequested(EventArgs.Empty);
        }

        private Program()
        {
            mainPage = new MainPage();
            mainPage.FormClosed += mainPage_FormClosed;
        }

        public async Task StartAsync()
        {
            await mainPage.InitializeAsync();
            mainPage.Show();
        }

        protected virtual void OnExitRequested(EventArgs e)
        {
            if (ExitRequested != null)
            {
                ExitRequested(this, e);
            }
        }

        static void p_ExitRequested(object sender, EventArgs e)
        {
            Application.ExitThread();
        }

        private static async void HandleExceptions(Task task)
        {
            try
            {
                await Task.Yield();
                await task;
            }
            catch (Exception exception)
            {
                //Do Something Here I need to Warn User.....
                Application.Exit();
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

            Program p = new Program();
            p.ExitRequested += p_ExitRequested;
            Task programStart = p.StartAsync();
            HandleExceptions(programStart);
            Application.Run();
        }
    }
}