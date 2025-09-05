using FastColoredTextBoxNS;
using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace CodeEditorApp
{
    public partial class Form1 : Form
    {
        private string dbPath = Path.Combine(Application.StartupPath, "dbcoding.mdb");
        private string connectionString;
        private int fontSize = 12;
        private bool isDragging = false;
        private Point dragStartPoint = Point.Empty;
        private ContextMenuStrip contextMenu;
        private int selectedId = -1;
        // এখানে ক্লাসের ফিল্ড ডিক্লেয়ার করতে হবে
        private bool isNewImageLoaded = false;
        public Form1()
        {
            InitializeComponent();

            connectionString = $@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={dbPath}";
            this.Load += Form1_Load;

            SetupContextMenu();

            fastColoredTextBox1.Language = Language.Custom; // Custom language
            fastColoredTextBox1.Font = new Font("Consolas", 20);
            fastColoredTextBox1.Text = "public class HelloWorld {\n    static void Main() {\n        Console.WriteLine(\"Welcome on Codedetails Software\");\n    }\n}";

            // চেটজিপিটি/VS Code-এর মতো কালার থিম
            fastColoredTextBox1.BackColor = Color.FromArgb(30, 30, 30);
            fastColoredTextBox1.ForeColor = Color.FromArgb(220, 220, 220);
            fastColoredTextBox1.LineNumberColor = Color.Gray;

            // Custom Syntax Highlighting Rules
            fastColoredTextBox1.TextChanged += (sender, e) =>
            {
                fastColoredTextBox1.ClearStylesBuffer();
                fastColoredTextBox1.Range.ClearStyle(StyleIndex.All);

                var keywordStyle = new TextStyle(new SolidBrush(Color.DeepSkyBlue), null, FontStyle.Regular);
                var commentStyle = new TextStyle(new SolidBrush(Color.Green), null, FontStyle.Italic);
                var stringStyle = new TextStyle(new SolidBrush(Color.Orange), null, FontStyle.Regular);
                var typeStyle = new TextStyle(new SolidBrush(Color.Teal), null, FontStyle.Bold);

                // Highlight keywords
                fastColoredTextBox1.Range.SetStyle(keywordStyle, @"\b(public|private|static|void|class|using|namespace|return|new)\b");

                // Highlight types
                fastColoredTextBox1.Range.SetStyle(typeStyle, @"\b(int|string|bool|char|float|double|var|object)\b");

                // Highlight strings
                fastColoredTextBox1.Range.SetStyle(stringStyle, "\".*?\"");

                // Highlight comments
                fastColoredTextBox1.Range.SetStyle(commentStyle, @"//.*$", RegexOptions.Multiline);
            };

            // Reset after 20 seconds
            Task.Delay(20000).ContinueWith(t =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    fastColoredTextBox1.Clear();
                    fastColoredTextBox1.Font = new Font("Consolas", 12);
                });
            });

            // Events
            fastColoredTextBox1.TabStop = true;
            fastColoredTextBox1.MouseWheel += FastColoredTextBox1_MouseWheel;
            fastColoredTextBox1.MouseUp += FastColoredTextBox1_MouseUp;

            pictureBox1.MouseWheel += PictureBox1_MouseWheel;
            pictureBox1.Click += pictureBox1_Click;
            pictureBox1.TabStop = true;
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseUp += PictureBox1_MouseUp;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData();
            dataGridView1.CellClick += dataGridView1_CellClick;
        }

        private void SetupContextMenu()
        {
            contextMenu = new ContextMenuStrip();

            ToolStripMenuItem copyItem = new ToolStripMenuItem("Copy");
            copyItem.Click += (s, e) => fastColoredTextBox1.Copy();
            contextMenu.Items.Add(copyItem);

            ToolStripMenuItem pasteItem = new ToolStripMenuItem("Paste");
            pasteItem.Click += (s, e) => fastColoredTextBox1.Paste();
            contextMenu.Items.Add(pasteItem);

            ToolStripMenuItem cutItem = new ToolStripMenuItem("Cut");
            cutItem.Click += (s, e) => fastColoredTextBox1.Cut();
            contextMenu.Items.Add(cutItem);

            ToolStripMenuItem selectAllItem = new ToolStripMenuItem("Select All");
            selectAllItem.Click += (s, e) => fastColoredTextBox1.SelectAll();
            contextMenu.Items.Add(selectAllItem);
        }

        private void FastColoredTextBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            int minFontSize = 8;
            int maxFontSize = 32;
            int step = 1;

            if (e.Delta > 0 && fontSize < maxFontSize)
                fontSize += step;
            else if (e.Delta < 0 && fontSize > minFontSize)
                fontSize -= step;

            fastColoredTextBox1.Font = new Font(fastColoredTextBox1.Font.FontFamily, fontSize);
        }

        private void FastColoredTextBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenu.Show(fastColoredTextBox1, e.Location);
        }

        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null) return;

            int step = 20;
            int minSize = 50;
            int maxSize = 1000;

            if (e.Delta > 0)
            {
                pictureBox1.Width = Math.Min(pictureBox1.Width + step, maxSize);
                pictureBox1.Height = Math.Min(pictureBox1.Height + step, maxSize);
            }
            else
            {
                pictureBox1.Width = Math.Max(pictureBox1.Width - step, minSize);
                pictureBox1.Height = Math.Max(pictureBox1.Height - step, minSize);
            }
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPoint = e.Location;
            }
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = pictureBox1.Location;
                newLocation.X += e.X - dragStartPoint.X;
                newLocation.Y += e.Y - dragStartPoint.Y;
                pictureBox1.Location = newLocation;
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                isDragging = false;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        pictureBox1.Image = Image.FromFile(ofd.FileName);
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    }
                }
            }
            else
            {
                int growAmount = 100;
                int maxWidth = 1000;
                int maxHeight = 1000;

                if (pictureBox1.Width + growAmount <= maxWidth &&
                    pictureBox1.Height + growAmount <= maxHeight)
                {
                    pictureBox1.Width += growAmount;
                    pictureBox1.Height += growAmount;
                }
                else
                {
                    MessageBox.Show("Maximum size reached.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

                txtCodeName.Text = row.Cells["codenam"].Value?.ToString();
                fastColoredTextBox1.Text = row.Cells["txtcoding"].Value?.ToString();

                selectedId = Convert.ToInt32(row.Cells["ID"].Value);

                LoadImageById(selectedId);

                isNewImageLoaded = false;  // আগের ছবি লোড হয়েছে, তাই ফ্ল্যাগ false
            }
        }


        private void LoadImageById(int id)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT imgpage FROM dbcoding WHERE ID = ?";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?", id);
                        object result = cmd.ExecuteScalar();

                        if (result != DBNull.Value && result != null)
                        {
                            byte[] imgData = (byte[])result;
                            using (MemoryStream ms = new MemoryStream(imgData))
                            {
                                pictureBox1.Image = Image.FromStream(ms);
                                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                            }
                        }
                        else
                        {
                            pictureBox1.Image = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading image: " + ex.Message);
            }
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodeName.Text) || string.IsNullOrWhiteSpace(fastColoredTextBox1.Text))
            {
                MessageBox.Show("Please enter code name and code text.");
                return;
            }

            byte[] imgData = null;
            if (pictureBox1.Image != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    pictureBox1.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    imgData = ms.ToArray();
                }
            }

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO dbcoding (codenam, txtcoding, imgpage) VALUES (?, ?, ?)";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?", txtCodeName.Text);
                        cmd.Parameters.AddWithValue("?", fastColoredTextBox1.Text);
                        cmd.Parameters.AddWithValue("?", imgData ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Data inserted successfully.");
                ClearForm();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedId == -1)
            {
                MessageBox.Show("Please select a row to update.");
                return;
            }

            byte[] imgData = null;

            if (isNewImageLoaded && pictureBox1.Image != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    pictureBox1.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    imgData = ms.ToArray();
                }
            }

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();

                    OleDbCommand cmd = new OleDbCommand();
                    cmd.Connection = conn;

                    if (isNewImageLoaded)
                    {
                        // ছবি সহ আপডেট
                        cmd.CommandText = "UPDATE dbcoding SET codenam = ?, txtcoding = ?, imgpage = ? WHERE ID = ?";
                        cmd.Parameters.AddWithValue("?", txtCodeName.Text);
                        cmd.Parameters.AddWithValue("?", fastColoredTextBox1.Text);
                        cmd.Parameters.AddWithValue("?", imgData);
                        cmd.Parameters.AddWithValue("?", selectedId);
                    }
                    else
                    {
                        // ছবি অপরিবর্তিত রেখে শুধুমাত্র টেক্সট আপডেট
                        cmd.CommandText = "UPDATE dbcoding SET codenam = ?, txtcoding = ? WHERE ID = ?";
                        cmd.Parameters.AddWithValue("?", txtCodeName.Text);
                        cmd.Parameters.AddWithValue("?", fastColoredTextBox1.Text);
                        cmd.Parameters.AddWithValue("?", selectedId);
                    }

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Data updated successfully.");
                    }
                    else
                    {
                        MessageBox.Show("No data was updated.");
                    }
                }

                ClearForm();
                LoadData();

                isNewImageLoaded = false;  // আপডেটের পর ফ্ল্যাগ রিসেট করো
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed: " + ex.Message);
            }
        }



        private void LoadData()
        {
            try
            {
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("Database file not found:\n" + dbPath);
                    return;
                }

                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    string query = "SELECT ID, codenam, txtcoding FROM dbcoding";
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        // Add Slno column for serial number
                        if (!dt.Columns.Contains("Slno"))
                        {
                            dt.Columns.Add("Slno", typeof(int));
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                dt.Rows[i]["Slno"] = i + 1;
                            }
                        }

                        dataGridView1.DataSource = null;
                        dataGridView1.Columns.Clear();
                        dataGridView1.DataSource = dt;

                        if (dataGridView1.Columns.Contains("ID"))
                            dataGridView1.Columns["ID"].Visible = false;

                        if (dataGridView1.Columns.Contains("Slno"))
                        {
                            var slnoCol = dataGridView1.Columns["Slno"];
                            slnoCol.DisplayIndex = 0;
                            slnoCol.HeaderText = "Sl No";
                            slnoCol.ReadOnly = true;
                            slnoCol.Width = 50;
                        }

                        if (dataGridView1.Columns.Contains("codenam"))
                        {
                            var nameCol = dataGridView1.Columns["codenam"];
                            nameCol.HeaderText = "Code Name";
                            nameCol.Width = 400;
                            nameCol.ReadOnly = true;
                        }

                        if (dataGridView1.Columns.Contains("txtcoding"))
                            dataGridView1.Columns["txtcoding"].ReadOnly = true;

                        dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                        dataGridView1.MultiSelect = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message);
            }
        }
        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // আগের ইমেজ Dispose করে নতুন ইমেজ লোড করো
                        if (pictureBox1.Image != null)
                        {
                            pictureBox1.Image.Dispose();
                        }

                        pictureBox1.Image = Image.FromFile(ofd.FileName);
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

                        isNewImageLoaded = true; // ফ্ল্যাগ সেট করো ছবি পরিবর্তিত হয়েছে
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading image: " + ex.Message);
                    }
                }
            }
        }


        private void ClearForm()
        {
            txtCodeName.Clear();
            fastColoredTextBox1.Clear();
            pictureBox1.Image = null;
            selectedId = -1;
        }
    }
}
