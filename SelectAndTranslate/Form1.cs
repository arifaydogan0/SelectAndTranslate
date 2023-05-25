using System;
using System.Windows.Forms;
using System.Net;
using System.Web;
using System.Threading.Tasks;
using Gma.System.MouseKeyHook;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Linq;
using System.Windows.Input;

namespace SelectAndTranslate
{
    public partial class Form1 : Form
    {
        string _sourceText = "";
        public string SourceText
        {
            get => _sourceText;
            set
            {
                IsActive = true;
                if (_sourceText == value)
                    return;
                _sourceText = value;

                richTextBox1.Clear();
                richTextBox1.AppendText(Translate(_sourceText, "auto", TargetLang).Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\u003c", "\u003c").Replace("\\u003d", "\u003d").Replace("\\u003e", "\u003e"));
                labelTranslate.Text = lblTranslate;
            }
        }
        public string TargetLang { get; set; } = "tr";
        bool _isActive = false;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                if (value)
                {
                    var screens = Screen.AllScreens.OrderBy(s => s.Bounds.X).ToList();
                    var cursorPos = GetCursorPosition();
                    if (screens.Count == 1 || cursorPos.X > screens[0].Bounds.X && cursorPos.X < screens[0].Bounds.X + screens[0].Bounds.Width)
                        this.Location = new Point(screens[0].Bounds.X + screens[0].Bounds.Width - this.Width - 10, screens[0].Bounds.Height - this.Height - 10);
                    else
                        this.Location = new Point(screens[1].Bounds.X + screens[1].Bounds.Width - this.Width - 10, screens[1].Bounds.Height - this.Height - 10);
                    LastActivate = DateTime.Now;
                    this.Show();
                }
                else
                {
                    this.Hide();

                }
            }
        }
        public DateTime LastActivate { get; set; } = DateTime.Now;
        public static string lblTranslate = "";


        public Form1()
        {
            InitializeComponent();

            globalMouseHook = Hook.GlobalEvents(); // Not: uygulama kancası için bunun yerine Hook.AppEvents() öğesini kullanın.

            globalMouseHook.MouseDragFinished += MouseDragFinished;   // DragFinished olayını bir işlevle bağlayın. Çift tıklama ile aynı, bu yüzden buraya yazmadım.
            //globalMouseHook.MouseDoubleClick += MouseDoubleClicked;   // MouseDoubleClick olayını MouseDoubleClicked adlı bir işlevle bağlayın. 
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            IsActive = true;

            if (Test())
                this.Text = "Bağlantı var";
            else
                this.Text = "Bağlantı yok";
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - LastActivate).TotalSeconds > richTextBox1.Text.Length / 6 + 7)
            {
                this.Hide();
            }
        }
        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
        private async void buttonCopy_Click(object sender, EventArgs e)
        {
            labelCopied.Visible = true;
            System.Windows.Clipboard.SetText(richTextBox1.Text);
            await Task.Delay(333);
            labelCopied.Visible = false;
        }
        private void kapatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }



        /// <summary>
        /// İnternet bağlantısı var mı diye test ediliyor.
        /// </summary>
        /// <returns></returns>
        public bool Test()
        {
            try
            {
                WebRequest request = WebRequest.Create("http://google.com");
                WebResponse response = request.GetResponse();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Bu metot Google Translate API kullanılır. Parametre olarak bir kaynak metin, kaynak dil(veya auto) ve hedef dil verilerek translate edilir ve sonucu string olarak döndürür.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static String Translate(String input, string from, string to)
        {
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={from}&tl={to}&dt=t&q={HttpUtility.UrlEncode(input.Replace("\"", "\'aa"))}";
            var result = new WebClient { Encoding = System.Text.Encoding.UTF8 }.DownloadString(url);
            try
            {
                //result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);

                List<int> quoteIndex = new System.Collections.Generic.List<int>();
                for (int i = 0; i < result.Length; i++)
                    if (result[i] == '\"')
                        quoteIndex.Add(i);
                List<string> contents = new List<string>();
                for (int i = 0; i < quoteIndex.Count - 1; i = i + 2)
                {
                    string content = result.Substring(quoteIndex[i] + 1, quoteIndex[i + 1] - quoteIndex[i] - 1);
                    contents.Add(content.Replace("\'aa", "\""));
                }
                string filteredResult = "";
                for (int i = 0; i < contents.Count; i++)
                {
                    if (input.Contains(contents[i].Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\u003c", "\u003c").Replace("\\u003d", "\u003d").Replace("\\u003e", "\u003e")) && !contents[i - 1].Contains("en_tr") && contents[i].Length > 7)
                        filteredResult += contents[i - 1];
                }
                lblTranslate = $"({contents[contents.Count - 1].ToUpper()} -> {to.ToUpper()})";
                return filteredResult;
            }
            catch(Exception ex)
            {
                return $"error\n{ex.Message}";
            }
        }
        // GUI gecikmelerini önlemek için işlevi zaman uyumsuz hale getiriyorum.
        private async void MouseDragFinished(object sender, System.Windows.Forms.MouseEventArgs e) => await GetSelectedText();
        //private async void MouseDoubleClicked(object sender, System.Windows.Forms.MouseEventArgs e) => await GetSelectedText();
        private IKeyboardMouseEvents globalMouseHook;
        /// <summary>
        /// Bu metot windows işletim sisteminde herhangi bir uygulamada seçilen metni yakalayıp döndürür.
        /// </summary>
        /// <returns></returns>
        async Task GetSelectedText()
        {
            var tmpClipboard = System.Windows.Clipboard.GetText();  // Daha sonra geri yüklemek için panonun mevcut içeriğini kaydedin.
            try
            {
                System.Windows.Clipboard.Clear();
                await Task.Delay(50);   // Küçük bir gecikmenin daha güvenli olacağını düşünüyorum.Kaldırabilirsiniz ama dikkatli olun.

                System.Windows.Forms.SendKeys.SendWait("^c");   // Send Ctrl+C, which is "copy"
                await Task.Delay(50);

                if (System.Windows.Clipboard.ContainsText())
                    SourceText = System.Windows.Clipboard.GetText();
            }
            catch
            {

            }
            finally
            {

                System.Windows.Clipboard.SetText(tmpClipboard);  // Restore the Clipboard.
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
            public static implicit operator Point(POINT point)
            {
                //return new Point(point.X - 175, point.Y - 100);               
                return new Point(point.X, point.Y);
                //Normal şartlarda bir üst satırdaki gibi olması gerekiyor.
                //Verdiğim bu ( - ) değerlerin açıklması aşağıdadır.                
            }
        }
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);
        public static Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
        }

    }
}
