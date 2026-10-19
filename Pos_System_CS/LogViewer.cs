using System;
using System.IO;
using System.Windows.Forms;

namespace Pos_System_CS
{
    public partial class LogViewer : Form
    {
        private TextBox _textBox;
        public LogViewer()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Log Viewer";
            this.Width = 800;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterParent;

            _textBox = new TextBox();
            _textBox.Multiline = true;
            _textBox.ReadOnly = true;
            _textBox.ScrollBars = ScrollBars.Both;
            _textBox.Dock = DockStyle.Fill;
            _textBox.Font = new System.Drawing.Font("Consolas", 10);

            var btnRefresh = new Button();
            btnRefresh.Text = "Refresh";
            btnRefresh.Dock = DockStyle.Top;
            btnRefresh.Height = 30;
            btnRefresh.Click += (s, e) => LoadLog();

            var panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 30;
            panel.Controls.Add(btnRefresh);

            this.Controls.Add(_textBox);
            this.Controls.Add(panel);

            this.Load += LogViewer_Load;
        }

        private void LogViewer_Load(object sender, EventArgs e)
        {
            LoadLog();
        }

        private void LoadLog()
        {
            try
            {
                var path = Logger.GetLogFilePath();
                if (!File.Exists(path))
                {
                    _textBox.Text = "Log file not found: " + path;
                    return;
                }

                // Read file safely
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    _textBox.Text = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                _textBox.Text = "Failed to load log: " + ex.Message;
            }
        }
    }
}
