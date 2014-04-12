using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NS23andMeToYSNPs
{
    public partial class Class23andMeToYSNPs : Form
    {

        string filename = null;
        DataTable data_table = null;
        Dictionary<string,string[]> map=null;
        bool snps_found = false;

        public Class23andMeToYSNPs()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog(this)==DialogResult.OK)
            {
                filename = openFileDialog1.FileName;
                saveSNPsToolStripMenuItem.Enabled = false;
                saveToolStripMenuItem.Enabled = false;
                snps_found = false;
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                data_table = new DataTable();
                data_table.Columns.Add("Y-SNP");
                data_table.Columns.Add("Derived");
                data_table.Columns.Add("RSID");
                data_table.Columns.Add("Chromosome");
                data_table.Columns.Add("Position");
                data_table.Columns.Add("Genotype");

                string[] lines = File.ReadAllLines(filename);
                string[] data = null;
                string[] snp = null;
                foreach (string line in lines)
                {
                    if (line.StartsWith("#"))
                        continue;
                    data = line.Split(new char[] { '\t' });
                    if (data[3] == "--") //no-call ignore...
                        continue;
                    if (data[1] != "Y")
                        continue;
                    if (map.ContainsKey(data[2]))
                    {
                        snp = getYSNP(data[2], data[3]);
                        data_table.Rows.Add(new object[] { snp[0].Replace(";", "/"), snp[1], data[0], data[1], data[2], data[3] });
                        snps_found = true;
                    }
                    else
                    {
                        data_table.Rows.Add(new object[] { "-", "-", data[0], data[1], data[2], data[3] });
                    }
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show("Error while opening file "+filename+"\r\nDetails:\r\n"+e1.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                snps_found = true; //to avoid another popup
            }
            
        }

        private string[] getYSNP(string pos,string gt)
        {
            string[] data = map[pos];

            if (data[1].EndsWith("->"+gt))
                data[1] = "Yes";
            else
                data[1] = "No";

            return data;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dataGridView1.DataSource = data_table;
            saveSNPsToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            if(!snps_found)
            {
                MessageBox.Show("There are no Y-SNPs identified. Please make sure the input file is of build 37. If not, please convert to Build 37 before opening here.","Warning!",MessageBoxButtons.OK,    MessageBoxIcon.Warning);
            }
            snps_found = false;
        }

        static byte[] decompress(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        private void backgroundWorkerInit_DoWork(object sender, DoWorkEventArgs e)
        {
            string csv = Encoding.UTF8.GetString(decompress(NS23andMeToYSNPs.Properties.Resources.ysnp_hg19));
            map = new Dictionary<string, string[]>();
            StringReader reader = new StringReader(csv);
            string l = null;
            string[] d = null;
            l = reader.ReadLine(); // header
            while((l=reader.ReadLine())!=null)
            {
                d = l.Split(new char[] { ',' });
                if(!map.ContainsKey(d[1]))
                    map.Add(d[1], new string[]{d[0],d[2]});
            }
        }

        private void YSNPNovelVariants_Load(object sender, EventArgs e)
        {
            statusLbl.Text = "Loading Y-SNPs ...";
            backgroundWorkerInit.RunWorkerAsync();
        }

        private void backgroundWorkerInit_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statusLbl.Text = "Y-SNPs Loaded.";
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "CSV Files|*.csv";
            if(saveFileDialog1.ShowDialog(this)==DialogResult.OK)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Y-SNP,Derived,RSID,Chromosome,Position,Genotype\r\n");

                foreach(DataGridViewRow row in dataGridView1.Rows)
                {
                    sb.Append(row.Cells[0].Value + "," + row.Cells[1].Value + "," + row.Cells[2].Value + "," + row.Cells[3].Value + "," + row.Cells[4].Value + "," + row.Cells[5].Value + "\r\n");
                }
                File.WriteAllText(saveFileDialog1.FileName,sb.ToString());
            }
            statusLbl.Text = "File Saved.";
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Developer : Felix Chandrakumar <i@fc.id.au>\r\nBuild Date : 12-Apr-2014\r\nWebsite : www.y-str.org.\r\nApplication supports only Build 37.\r\n\r\nIcon : http://www.iconarchive.com/artist/hydrattz.html \r\nData from ISOGG, Dr Jim Wilson and ScotlandsDNA.\r\nRef: http://www.yourgeneticgenealogist.com/2014/03/dr-jim-wilson-and-scotlandsdna-release.html", "About 23andMe to Y-SNPs", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void saveSNPsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Text Files|*.txt";
            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                StringBuilder sb = new StringBuilder();
                string output = null;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Cells[1].Value == null)
                        continue;
                    if (row.Cells[1].Value.ToString() == "Yes")
                        sb.Append(row.Cells[0].Value.ToString().Replace("/","+ ")+"+ ");
                    else if (row.Cells[1].Value.ToString() == "No")
                        sb.Append(row.Cells[0].Value.ToString().Replace("/", "- ") + "- ");
                }
                output = sb.ToString().Trim().Replace(" ",", ");
                File.WriteAllText(saveFileDialog1.FileName, output);
            }
            statusLbl.Text = "Y-SNPs Saved.";
        }
    }
}
