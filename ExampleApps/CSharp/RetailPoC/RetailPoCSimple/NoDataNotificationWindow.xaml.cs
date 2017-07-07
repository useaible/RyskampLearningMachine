using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RetailPoCSimple
{
    /// <summary>
    /// Interaction logic for NoDataNotificationWindow.xaml
    /// </summary>
    public partial class NoDataNotificationWindow
    {
        public NoDataNotificationWindow()
        {
            InitializeComponent();
        }

        private Timer timer = new Timer(1000);
        private void noDataWindow_Loaded(object sender, RoutedEventArgs e)
        {
            timer.Start();
            timer.Elapsed += Timer_Elapsed;
        }

        private int counter = 0;
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            counter++;
            if(counter > 0)
            {
                timer.Stop();
                Dispatcher.Invoke(()=> {
                    this.Close();
                });
            }
        }
    }
}
