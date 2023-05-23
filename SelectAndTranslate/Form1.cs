using System;
using System.Windows.Forms;
using System.Net;
using System.Web;
using System.Threading.Tasks;
using Gma.System.MouseKeyHook;
using System.Collections.Generic;

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
                if (_sourceText == value)
                    return;
                _sourceText = value;

                richTextBox1.Clear();
                richTextBox1.AppendText(Translate(_sourceText, "auto", TargetLang).Replace("\\n","\n").Replace("\\r","\r"));

            }
        }
        public string TargetLang { get; set; } = "tr";


        public Form1()
        {
            InitializeComponent();

            globalMouseHook = Hook.GlobalEvents(); // Not: uygulama kancası için bunun yerine Hook.AppEvents() öğesini kullanın.

            globalMouseHook.MouseDragFinished += MouseDragFinished;   // DragFinished olayını bir işlevle bağlayın. Çift tıklama ile aynı, bu yüzden buraya yazmadım.
            //globalMouseHook.MouseDoubleClick += MouseDoubleClicked;   // MouseDoubleClick olayını MouseDoubleClicked adlı bir işlevle bağlayın. 
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //TODO: buraya pencerenin boyutunu ve konumunu ayarlayan kodları yaz.
            if (Test())
                this.Text = "Bağlantı var";
            else
                this.Text = "Bağlantı yok";
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            //her cycle da seçili metin var mı diye baksın, varsa tüm metni çevirip textboxa yazsın

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
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={from}&tl={to}&dt=t&q={HttpUtility.UrlEncode(input)}";
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
                    contents.Add(content);
                }
                string filteredResult = "";
                for (int i = 0; i < contents.Count; i++)
                {
                    if (input.Contains(contents[i].Replace("\\n","").Replace("\\r","")) && contents[i].Length >= 7)
                        filteredResult += contents[i-1];
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
            {
                SourceText = System.Windows.Clipboard.GetText();
            }

            System.Windows.Clipboard.SetDataObject(tmpClipboard);  // Restore the Clipboard.
        }
    }
}
