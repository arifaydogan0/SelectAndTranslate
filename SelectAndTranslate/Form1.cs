using System;
using System.Windows.Forms;
using System.Net;
using System.Web;
using System.Threading.Tasks;
using Gma.System.MouseKeyHook;
using System.Collections.Generic;
using System.Drawing;

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
            }
        }
        public string TargetLang { get; set; } = "tr";
        bool _isActive = false;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                var screens = Screen.AllScreens;
                this.Location = screens.Length == 1 ? new Point(screens[0].WorkingArea.Width - this.Width, screens[0].WorkingArea.Height - this.Height) : new Point(screens[1].WorkingArea.Width - this.Width, screens[1].WorkingArea.Height - this.Height);
            }
        }
        public DateTime LastActivate { get; set; } = DateTime.Now;


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


        }
        private void buttonClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private async void buttonCopy_Click(object sender, EventArgs e)
        {
            labelCopied.Visible = true;
            System.Windows.Clipboard.SetText(richTextBox1.Text);
            await Task.Delay(333);
            labelCopied.Visible = false;
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
                return filteredResult;
            }
            catch
            {
                return "error";
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
            System.Windows.IDataObject tmpClipboard = System.Windows.Clipboard.GetDataObject();  // Daha sonra geri yüklemek için panonun mevcut içeriğini kaydedin.
            System.Windows.Clipboard.Clear();
            await Task.Delay(50);   // Küçük bir gecikmenin daha güvenli olacağını düşünüyorum.Kaldırabilirsiniz ama dikkatli olun.

            System.Windows.Forms.SendKeys.SendWait("^c");   // Send Ctrl+C, which is "copy"
            await Task.Delay(50);

            if (System.Windows.Clipboard.ContainsText())
                SourceText = System.Windows.Clipboard.GetText();


            System.Windows.Clipboard.SetDataObject(tmpClipboard);  // Restore the Clipboard.
        }

    }
}
