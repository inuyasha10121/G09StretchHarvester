using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace G09StretchHarvester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            if (textBoxPath.Text == String.Empty)
                return;
            var workdir = textBoxPath.Text;
            if (!Directory.Exists(workdir))
                return;

            saveFileDialog1.Filter = ".csv File | *.csv";
            var result = saveFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
                return;
            var savefile = saveFileDialog1.FileName;


            var outfiles = Directory.GetFiles(workdir, "*.out");
            var goodfiles = new List<string>();
            Console.WriteLine("Going through and checking files for errors...");
            foreach(var file in outfiles)
            {
                bool passed = true;
                var data = File.ReadAllLines(file);
                foreach(var line in data)
                {
                    if (line.Contains("Error termination"))
                        passed = false;
                }
                if (passed)
                    goodfiles.Add(file);
            }
            Console.WriteLine("Getting data...");
            var lengths = new decimal[goodfiles.Count];
            var energies = new decimal[goodfiles.Count];
            //Go through the first file to get the parameters
            lengths[0] = decimal.Parse(goodfiles[0].Substring(goodfiles[0].LastIndexOf("-") + 1).Replace("_", ".").Replace(".out",""));
            var tempMOs = new List<decimal>();
            var tempNMR = new List<decimal>();
            var tempdata = File.ReadAllLines(goodfiles[0]);
            bool link1 = false;
            for(int i = 0; i < tempdata.Length; ++i)
            {
                if (link1)
                {
                    if(tempdata[i].Contains("SCF Done:"))
                    {
                        energies[0] = decimal.Parse(tempdata[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries)[4]);
                    }
                    if(tempdata[i].Contains("Anisotropy ="))
                    {
                        tempNMR.Add(decimal.Parse(tempdata[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries)[4]));
                    }
                    if(tempdata[i].Contains("Alpha  occ. eigenvalues"))
                    {
                        var splitvals = tempdata[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        for(int j = 4; j < splitvals.Length; ++j)
                        {
                            tempMOs.Add(decimal.Parse(splitvals[j]));
                        }
                    }
                }
                if (tempdata[i].Contains("%oldchk="))
                {
                    link1 = true;
                }
            }

            var MOTable = new decimal[goodfiles.Count][];
            var NMRTable = new decimal[goodfiles.Count][];
            MOTable[0] = tempMOs.ToArray();
            NMRTable[0] = tempNMR.ToArray();

            //Go through the rest of the files and harvest the data
            for (int j = 1; j < goodfiles.Count; ++j)
            {
                lengths[j] = decimal.Parse(goodfiles[j].Substring(goodfiles[j].LastIndexOf("-") + 1).Replace("_", ".").Replace(".out", ""));
                tempMOs = new List<decimal>();
                tempNMR = new List<decimal>();
                tempdata = File.ReadAllLines(goodfiles[j]);
                link1 = false;
                for (int i = 0; i < tempdata.Length; ++i)
                {
                    if (link1)
                    {
                        if (tempdata[i].Contains("SCF Done:"))
                        {
                            energies[j] = decimal.Parse(tempdata[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries)[4]);
                        }
                        if (tempdata[i].Contains("Anisotropy ="))
                        {
                            tempNMR.Add(decimal.Parse(tempdata[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries)[4]));
                        }
                        if (tempdata[i].Contains("Alpha  occ. eigenvalues"))
                        {
                            var splitvals = tempdata[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                            for (int k = 4; k < splitvals.Length; ++k)
                            {
                                tempMOs.Add(decimal.Parse(splitvals[k]));
                            }
                        }
                    }
                    if (tempdata[i].Contains("%oldchk="))
                    {
                        link1 = true;
                    }
                }
                MOTable[j] = tempMOs.ToArray();
                NMRTable[j] = tempNMR.ToArray();
            }
            Console.WriteLine("Done harvesting data.  Writing data to file...");
            var writedata = "Length:,Energy:,";
            for(int i = 0; i < tempNMR.Count; ++i)
            {
                writedata += "NMR-" + (i + 1) + ":,";
            }
            for(int i = 0; i < tempMOs.Count; ++i)
            {
                writedata += "MO-" + (i + 1) + ":,";
            }
            writedata += "\n";

            for(int i = 0; i < goodfiles.Count; ++i)
            {
                writedata += lengths[i] + "," + energies[i] + ",";
                for(int j = 0; j < NMRTable[i].Length; ++j)
                {
                    writedata += NMRTable[i][j] + ",";
                }
                for(int j = 0; j < MOTable[i].Length; ++j)
                {
                    writedata += MOTable[i][j] + ",";
                }

                writedata += "\n";
            }

            File.WriteAllText(savefile, writedata);
            Console.WriteLine("Done fully!");
        }
    }
}
