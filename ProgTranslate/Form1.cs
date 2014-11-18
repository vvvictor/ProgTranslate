using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Web;

namespace ProgTranslate
{
    public partial class Form1 : Form
    {
        string infile, outfile, instr, outstr, intext, outtext, dlmt;
        int dlmtpos, dlmt1, dlmt2;
        string trstr;

        string[] filestr;
        int strcounter;
        bool incomment = false;
        Int32[] cods = { 936, 0, 0, 950 };

        public Form1()
        {
            InitializeComponent();
            tolang.SelectedIndex = 1;
            fromlang.SelectedIndex = 0;
        }

        private void newtrans_Click(object sender, EventArgs e)
        {
            if (OpenFile.ShowDialog() == DialogResult.OK) infile = OpenFile.FileName;
            else return;
            try {
                if (cods[fromlang.SelectedIndex] != 0) filestr = File.ReadAllLines(infile, System.Text.Encoding.GetEncoding(cods[fromlang.SelectedIndex]));
                else filestr = File.ReadAllLines(infile);
                richTextBox1.Clear();
                richTextBox2.Clear();
                strcounter = 0;
                incomment = false;
                AsyncMethodCaller caller = new AsyncMethodCaller(StartTranslation);
                caller.Invoke();
                //IAsyncResult result = caller.BeginInvoke(null, null);
                //caller.EndInvoke(result);
            } catch (ArgumentNullException ex) {
                MessageBox.Show("Параметр path имеет значение null.");
            } catch(ArgumentException ex) {
                MessageBox.Show("path представляет собой строку нулевой длины, содержащую только пробелы или один или несколько недопустимых символов, как указано InvalidPathChars.");
            } catch(PathTooLongException ex) {
                MessageBox.Show("Длина указанного пути, имени файла или обоих параметров превышает установленное в системе максимальное значение. Например, для платформ на основе Windows длина пути не должна превышать 248 символов, а имена файлов не должны содержать более 260 символов.");
            } catch(DirectoryNotFoundException ex) {
                MessageBox.Show("Указанный путь недопустим (например, он соответствует неподключенному диску).");
            } catch(FileNotFoundException ex) {
                MessageBox.Show("Файл, заданный в path, не найден.");
            } catch(UnauthorizedAccessException ex) {
                MessageBox.Show("path указывает файл, разрешенный только для чтения. У вызывающего объекта отсутствует необходимое разрешение.");
            } catch(IOException ex) {
                MessageBox.Show("При открытии файла возникла ошибка ввода-вывода.");
            } catch(NotSupportedException ex) {
                MessageBox.Show("path имеет недопустимый формат.");
            } catch(System.Security.SecurityException ex) {
                MessageBox.Show("У вызывающего объекта отсутствует необходимое разрешение.");
            }
        }

        delegate void SetTextCallback(string text);
        private void SetText1(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.richTextBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText1);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.richTextBox1.AppendText(text);
            }
        }
        private void SetText2(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.richTextBox2.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText2);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.richTextBox2.AppendText(text);
            }
        }

        public void StartTranslation()
        {
            
            if(strcounter>=filestr.Length) return;
//            if (strcounter > 20) return;
            instr = filestr[strcounter] +"\r\n";
            trstr = "";
            SetText1(instr);
            if (incomment)
            {
                outstr = "";
                trstr = instr;
                if (instr.IndexOf("*/") > -1) incomment = false;
            }
            else
            {
                dlmt1 = instr.IndexOf("//");
                dlmt2 = instr.IndexOf("/*");
                if (dlmt1 == dlmt2) outstr = instr;
                else
                {
                    if (dlmt1 >= 0 && (dlmt1 < dlmt2 || dlmt2 < 0))
                    {
                        dlmtpos = dlmt1; dlmt = "//";
                    }
                    else
                    {
                        dlmtpos = dlmt1; dlmt = "/*";
                        if (instr.IndexOf("*/") == -1) incomment = true;
                    }
                    outstr = instr.Substring(0, dlmtpos + 2);
                    trstr = instr.Substring(dlmtpos + 2);
                }
            }

            if (trstr.Length == 0) {
                SetText2(outstr);
                strcounter++;
                AsyncMethodCaller caller = new AsyncMethodCaller(StartTranslation);
                caller.Invoke();
                return;
            }

            // STEP 1: Create the request for the OAuth service that will
            // get us our access tokens.
            String strTranslatorAccessURI = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
            System.Net.WebRequest req = System.Net.WebRequest.Create(strTranslatorAccessURI);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            // Important to note -- the call back here isn't that the request was processed by the server
            // but just that the request is ready for you to do stuff to it (like writing the details)
            // and then post it to the server.
            IAsyncResult writeRequestStreamCallback = (IAsyncResult)req.BeginGetRequestStream(new AsyncCallback(RequestStreamReady), req);
        }

        private void RequestStreamReady(IAsyncResult ar)
        {
            // STEP 2: The request stream is ready. Write the request into the POST stream
            string clientID = "uour clientID";
            string clientSecret = "your clientSecret";
            String strRequestDetails = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", HttpUtility.UrlEncode(clientID), HttpUtility.UrlEncode(clientSecret));
           
            // note, this isn't a new request -- the original was passed to beginrequeststream, so we're pulling
            // a reference to it back out. It's the same request
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)ar.AsyncState;
           
            // now that we have the working request, write the request details into it
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(strRequestDetails);
            System.IO.Stream postStream = request.EndGetRequestStream(ar);
            postStream.Write(bytes, 0, bytes.Length);
            postStream.Close();

            // now that the request is good to go, let's post it to the server
            // and get the response. When done, the async callback will call the
            // GetResponseCallback function
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
        }

        delegate string GetTextCallback();
        private string FromText()
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.fromlang.InvokeRequired)
            {
                GetTextCallback d = new GetTextCallback(FromText);
                return (string)this.Invoke(d);
            }
            else
            {
                return fromlang.GetItemText(fromlang.SelectedItem);
            }
        }
        private string ToText()
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.tolang.InvokeRequired)
            {
                GetTextCallback d = new GetTextCallback(ToText);
                return (string)this.Invoke(d);
            }
            else
            {
                return tolang.GetItemText(tolang.SelectedItem);
            }
        }
        private void GetResponseCallback(IAsyncResult ar)
        {
            // STEP 3: Process the response callback to get the token
            // we'll then use that token to call the translator service

            // Pull the request out of the IAsynch result
            HttpWebRequest request = (HttpWebRequest)ar.AsyncState;
 
            // The request now has the response details in it (because we've called back having gotten the response
            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar);

            // Using JSON we'll pull the response details out, and load it into an AdmAccess token class (defined earlier in this module)
            // IMPORTANT (and vague) -- despite the name, in Windows Phone 7, this is in the System.ServiceModel.Web library,
            // and not the System.Runtime.Serialization one -- so you will need to have a reference to System.ServiceModel.Web
            System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(AdmAccessToken));
            AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(response.GetResponseStream());

            // We create the URI to the Translate service in the usual way. It's hardcoded here for EN->ES
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + System.Web.HttpUtility.UrlEncode(trstr) +
                "&from=" + FromText() +
                "&to=" + ToText();
            System.Net.WebRequest translationWebRequest = System.Net.HttpWebRequest.Create(uri);

            // The authorization header needs to be "Bearer" + " " + the access token
            string headerValue = "Bearer " + token.access_token;
            translationWebRequest.Headers["Authorization"] = headerValue;

            // And now we call the service. When the translation is complete, we'll get the callback
            IAsyncResult writeRequestStreamCallback = (IAsyncResult)translationWebRequest.BeginGetResponse(new AsyncCallback(translationReady), translationWebRequest);
        }

        private void translationReady(IAsyncResult ar)
        {
            // STEP 4: Process the translation

            // Get the request details
            HttpWebRequest request = (HttpWebRequest)ar.AsyncState;
           
            // Get the response details
            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar);
           
            // Read the contents of the response into a string
            System.IO.Stream streamResponse = response.GetResponseStream();
            System.IO.StreamReader streamRead = new System.IO.StreamReader(streamResponse);
            string responseString = streamRead.ReadToEnd();

            // Translator returns XML, so load it into an XDocument
            // Note -- you need to add a reference to the System.Linq.XML namespace
            System.Xml.Linq.XDocument xTranslation = System.Xml.Linq.XDocument.Parse(responseString);
            if (xTranslation.Root.FirstNode != null) outstr += xTranslation.Root.FirstNode.ToString();
            else outstr += trstr;
            //if (outstr == null) outstr = "\r\n";
            SetText2(outstr);
            strcounter++;
            AsyncMethodCaller caller = new AsyncMethodCaller(StartTranslation);
            caller.Invoke();
            // We're not on the UI thread, so use the dispatcher to update the UI
            //Deployment.Current.Dispatcher.BeginInvoke( () => lblTranslated.Text = strTest);

        }

        private void onSplitterMoved(object sender, SplitterEventArgs e)
        {
            richTextBox1.Height = splitContainer1.Panel1.ClientSize.Height;
            richTextBox1.Width = splitContainer1.Panel1.ClientSize.Width;
            richTextBox2.Height = splitContainer1.Panel2.ClientSize.Height;
            richTextBox2.Width = splitContainer1.Panel2.ClientSize.Width;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            splitContainer1.Width = Form1.ActiveForm.ClientSize.Width - 12;
            splitContainer1.Height = Form1.ActiveForm.ClientSize.Height - 39;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(richTextBox2.Lines.Length==0) return;
            if (SaveFile.ShowDialog() == DialogResult.OK) File.WriteAllLines(SaveFile.FileName, richTextBox2.Lines);
        }

  


    }

    public delegate void AsyncMethodCaller();

    public class AdmAccessToken
    {
        public string access_token { get; set; } //The new access token that was requested. 
        public string token_type { get; set; } //The token type. The only supported value is bearer.
        public string expires_in { get; set; } //The remaining lifetime of the token in seconds. A typical value is 3600 (one hour).
        public string scope { get; set; } //Impersonation permissions granted to the native client application. The default permission is user_impersonation. The owner of the target resource can register alternate values in Azure AD.
        public string expires_on { get; set; } //The date and time on which the token expires. The date is represented as the number of seconds from 1970-01-01T0:0:0Z UTC until the expiration time.
        public string refresh_token { get; set; } //A new OAuth 2.0 refresh_token that can be used to request new access tokens when the one in this response expires.
        public string resource { get; set; } //Identifies the secured resource that the access token can be used to access.
    } 
}
