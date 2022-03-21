using MediaToolkit;
using MediaToolkit.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoLibrary;

namespace YoutubeDownlaoder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            textBox2.Text = "999";

            if (!Directory.Exists(ExecutablePath))
            {
                Directory.CreateDirectory(ExecutablePath);
            }
            else
            {
                foreach (var file in Directory.GetFiles(ExecutablePath))
                {
                    try
                    {
                        File.Delete(file);
                    }catch { }
                }
            }
        }

        public string ApiKey => "YOUR API KEY";
        public static readonly Lazy<HttpClient> LazyHttpClient = new Lazy<HttpClient>();
        public static readonly HttpClient httpCLient = LazyHttpClient.Value;

        private async void Button2_Click(object sender, EventArgs e)
        {
            var folderBrowser = new FolderBrowserDialog();

            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                button2.Enabled = false;

                var yt = YouTube.Default;

                try
                {
                    Sayac = 0;
                    timer1.Start();
                    foreach (var item in items)
                    {
                        await Download(item, folderBrowser.SelectedPath, yt, Sayac);

                        lblDownloaded.Text = (Sayac + 1).ToString() + ". video downloaded";

                        if (Cancel)
                        {
                            break;
                        }

                        Sayac++;
                    }

                    timer1.Stop();
                    await Task.Delay(1000);

                    Reset();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No folder selected", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Reset()
        {
            if (!Cancel)
            {
                lblDownloading.Text = "Completed!";
                lblDownloaded.Text = "";
            }
            else
            {
                lblDownloading.Text = "It is cancelled";
                lblDownloaded.Text = "";
            }

            items = null;
            checkedListBox1.Items.Clear();
            lblIptal.Text = "";
            textBox1.Clear();
            textBox2.Text = "999";
            Cancel = false;
        }

        int Sayac { get; set; } = 0;
        bool Cancel { get; set; } = false;

        string ExecutablePath => Path.GetDirectoryName(Application.ExecutablePath) + "\\Trash";

        private async Task Download(Item item, string path, YouTube yt, int sayac)
        {
            try
            {
                var video = await yt.GetVideoAsync("https://www.youtube.com/watch?v=" + item.Snippet.ResourceId.VideoId);
                File.WriteAllBytes(ExecutablePath + "\\" + video.FullName, await video.GetBytesAsync());

                var inputFile = new MediaFile { Filename = ExecutablePath + "\\" + video.FullName };
                var outputFile = new MediaFile { Filename = (path + "\\" + video.FullName + ".mp3").Replace(".mp4", "") };

                var enging = new Engine();

                enging.GetMetadata(inputFile);
                enging.Convert(inputFile, outputFile);

                File.Delete(ExecutablePath + "\\" + video.FullName);
                checkedListBox1.SetItemChecked(sayac, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || !textBox1.Text.Contains("https://www.youtube.com/playlist?list="))
            {
                MessageBox.Show("Please enter a correct youtube playlist link!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                checkedListBox1.Items.Clear();
                string id = textBox1.Text.Substring(38);
                var maxResult = textBox2.Text ?? "999";

                string jsonUrl = $"https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&maxResults={maxResult}&playlistId={id}&key={ApiKey}";

                var responseMessage = await httpCLient.GetAsync(jsonUrl);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    MessageBox.Show(responseMessage.ReasonPhrase, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var content = await responseMessage.Content.ReadAsStringAsync();
                Root playlistInfo = JsonConvert.DeserializeObject<Root>(content);

                items = playlistInfo.Items;

                foreach (var item in items)
                {
                    checkedListBox1.Items.Add(item.Snippet.Title);
                }

                txtTotal.Text = items.Count.ToString();
                button2.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        List<Item> items;

        private void Button3_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.Items.Count != 0 && lblDownloading.Text != "")
            {
                var result = MessageBox.Show("Are you sure you want to cancel?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Cancel = true;
                    lblIptal.Text = "When the currently downloading\nsong is downloaded, theoptation will take place.";
                }
            }            
            else
            {
                if (checkedListBox1.Items.Count == 0) return;

                var result = MessageBox.Show("Are you sure you want to cancel?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    checkedListBox1.Items.Clear();
                    Cancel = true;
                }
            }
        }

        private async void Timer1_Tick(object sender, EventArgs e)
        {
            lblDownloading.Text = (Sayac + 1).ToString() + ". video is downloading.";
            await Task.Delay(600);
            lblDownloading.Text = (Sayac + 1).ToString() + ". video is downloading..";
            await Task.Delay(600);
            lblDownloading.Text = (Sayac + 1).ToString() + ". video is downloading...";
            await Task.Delay(600);
        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            button2.Enabled = false;
        }
    }
}
