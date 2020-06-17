using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NLog.WPF.Samples
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            RichTextBoxTarget.ReInitializeAllTextboxes(this);

            //启用超链接时，要只读且documentenable为true
            //IsReadOnly="True" IsDocumentEnabled="True"

            RichTextBoxTarget.GetTargetByControl(richTextBox1).LinkClicked += Form1_LinkClicked;
            //RichTextBoxTarget.GetTargetByControl(richTextBox2).LinkClicked += Form1_LinkClicked;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
            Logger.Error("1111111111111");

            Random rnd = new Random();
            LogEventInfo theEvent = new LogEventInfo(LogLevel.Debug, "", "***: a line with some length\n a new line");
            if (rnd.NextDouble() > 0.1)
            {
                theEvent.Properties["ShowLink"] = "link via property";
            }
            if (rnd.NextDouble() > 0.5)
            {
                theEvent.Properties["ShowLink2"] = "Another link";
            }
            Logger.Log(theEvent);
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
          //  BeginInvokeLambda(this,
          //    () => { MessageBox.Show("Clicked link '" + linkText + "' for event\n" + logEvent, sender.Name); }
          //);
        }

        private void Form1_LinkClicked(RichTextBoxTarget sender, string linkText, LogEventInfo logEvent)
        {
            MessageBox.Show("Clicked link '" + linkText + "' for event\n" + logEvent, sender.Name);
            //COM HRESULT E_FAIL happens when not used BeginInvoke and links are clicked while spinning
            //BeginInvokeLambda(this,
            //    () => { MessageBox.Show("Clicked link '" + linkText + "' for event\n" + logEvent, sender.Name); }
            //);
        }

        private static IAsyncResult BeginInvokeLambda(Control control, Action action)
        {
            //if (!control.IsDisposed)
            //{
            //    return control.BeginInvoke(action, null);
            //}
            return null;
        }

    }
}
